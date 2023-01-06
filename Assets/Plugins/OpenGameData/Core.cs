using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace OGD {

    /// <summary>
    /// Core features of OGD client.
    /// </summary>
    static public class Core {

        #region Return Status

        public enum ReturnStatus {
            Success,
            Error_DB,
            Error_Request,
            Error_Server,

            Error_Network,
            Error_Exception,
            Unknown
        }

        public struct Error {
            public readonly ReturnStatus Status;
            public readonly string Msg;

            public Error(ReturnStatus status, string msg) {
                Status = status;
                Msg = msg;
            }

            public Error(string msg) {
                Status = ReturnStatus.Unknown;
                Msg = msg;
            }

            public override string ToString() {
                return string.Format("{0}({1})", Status, Msg);
            }

            static public readonly Error Success = default(Error);
        }

        static internal ReturnStatus ParseStatus(string status) {
            switch(status) {
                case StatusCodeSuccess:
                    return ReturnStatus.Success;
                case StatusCodeDBError:
                    return ReturnStatus.Error_DB;
                case StatusCodeRequestError:
                    return ReturnStatus.Error_Request;
                case StatusCodeServerError:
                    return ReturnStatus.Error_Server;
                default:
                    Debug.LogWarningFormat("[OGD.Core] Unknown status code '{0}'", status);
                    return ReturnStatus.Unknown;
            }
        }

        private const string StatusCodeSuccess = "SUCCESS";
        private const string StatusCodeDBError = "ERR_DB";
        private const string StatusCodeRequestError = "ERR_REQ";
        private const string StatusCodeServerError = "ERR_SRV";

        #endregion // Return Status

        internal struct DefaultResponse {
            public string[] val;
            public string msg;
            public string status;
        }

        public delegate void DefaultErrorHandlerDelegate(Error err);
        public delegate void RequestNotificationDelegate(UnityWebRequest request, int retryStatus);
        public delegate void RequestNotificationErrorDelegate(UnityWebRequest request, Error err);
        
        internal delegate Error ResponseProcessorDelegate<TResponse>(TResponse response, object userData);
        internal delegate void RequestErrorHandlerDelegate(Error err, object userData);

        static private string s_ServerAddress = string.Empty;
        static private string s_GameId = string.Empty;

        static private readonly StringBuilder[] s_BuilderPool = new StringBuilder[4];
        static private int s_BuilderPoolCount = 0;

        /// <summary>
        /// Configures the server address and game id.
        /// </summary>
        static public void Configure(string serverAddress, string gameId) {
            s_ServerAddress = serverAddress;
            s_GameId = gameId;
        }

        /// <summary>
        /// Returns the currently assigned game id.
        /// </summary>
        static public string GameId() {
            return s_GameId;
        }

        /// <summary>
        /// Event dispatched when a request is sent.
        /// </summary>
        static public event RequestNotificationDelegate OnRequestSent;

        /// <summary>
        /// Event dispatched when a request is processed.
        /// </summary>
        static public event RequestNotificationErrorDelegate OnRequestResponse;

        #region Constructing a Query

        internal struct Query {
            public string API;
            public StringBuilder ArgsBuilder;
            public int ArgCount;
        }

        static internal Query NewQuery(string api) {
            Query query;
            query.API = api;
            query.ArgsBuilder = null;
            query.ArgCount = 0;
            return query;
        }

        static internal Query NewQuery(string apiFormat, params string[] args) {
            Query query;
            query.API = string.Format(apiFormat, args);
            query.ArgsBuilder = null;
            query.ArgCount = 0;
            return query;
        }

        static internal void QueryArg(ref Query builder, string key, string data) {
            if (builder.ArgsBuilder == null) {
                builder.ArgsBuilder = AllocStringBuilder();
            }

            if (builder.ArgCount++ > 0) {
                builder.ArgsBuilder.Append('&');
            } else {
                builder.ArgsBuilder.Append('?');
            }

            builder.ArgsBuilder.Append(EscapeURI(key))
                .Append('=')
                .Append(EscapeURI(data));
        }

        static internal void QueryArg(ref Query builder, string key, int data) {
            if (builder.ArgsBuilder == null) {
                builder.ArgsBuilder = new StringBuilder();
            }

            if (builder.ArgCount++ > 0) {
                builder.ArgsBuilder.Append('&');
            } else {
                builder.ArgsBuilder.Append('?');
            }

            builder.ArgsBuilder.Append(EscapeURI(key))
                .Append('=')
                .Append(data);
        }

        static private string EscapeURI(string data) {
            return data == null ? "" : UnityWebRequest.EscapeURL(data);
        }

        #endregion // Constructing a Query

        #region Sending Request

        internal class Request<TResponse> : IDisposable {
            public UnityWebRequest WebRequest;
            public ResponseProcessorDelegate<TResponse> Handler;
            public RequestErrorHandlerDelegate ErrorHandler;
            public object UserData;
            public int RetryCount;

            public void Dispose() {
                var request = this;
                Core.CancelRequest(ref request);
            }
        }

        /// <summary>
        /// Submits a query as a GET request.
        /// </summary>
        static internal Request<T> Get<T>(Query query, ResponseProcessorDelegate<T> handler, RequestErrorHandlerDelegate errorHandler, object userData, int retryCount = 0) {
            return SendRequest<T>(query, UnityWebRequest.kHttpVerbGET, handler, errorHandler, userData, retryCount);
        }

        /// <summary>
        /// Submits a query as a PUT request.
        /// </summary>
        static internal Request<T> Put<T>(Query query, ResponseProcessorDelegate<T> handler, RequestErrorHandlerDelegate errorHandler, object userData, int retryCount = 0) {
            return SendRequest<T>(query, UnityWebRequest.kHttpVerbPUT, handler, errorHandler, userData, retryCount);
        }

        /// <summary>
        /// Submits a query as a POST request.
        /// </summary>
        static internal Request<T> Post<T>(Query query, ResponseProcessorDelegate<T> handler, RequestErrorHandlerDelegate errorHandler, object userData, int retryCount = 0) {
            return SendRequest<T>(query, UnityWebRequest.kHttpVerbPOST, handler, errorHandler, userData, retryCount);
        }

        static private Request<T> SendRequest<T>(Query query, string method, ResponseProcessorDelegate<T> handler, RequestErrorHandlerDelegate errorHandler, object userData, int retryCount) {
            StringBuilder pathBuilder = AllocStringBuilder();
            pathBuilder.Append(s_ServerAddress).Append(query.API);
            if (query.ArgsBuilder != null && query.ArgsBuilder.Length > 0) {
                pathBuilder.Append(query.ArgsBuilder.ToString());
            }

            Request<T> request = new Request<T>();
            UnityWebRequest uwr = new UnityWebRequest(pathBuilder.ToString(), method);
            uwr.downloadHandler = new DownloadHandlerBuffer();

            FreeStringBuilder(query.ArgsBuilder);
            FreeStringBuilder(pathBuilder);
            
            request.WebRequest = uwr;
            request.Handler = handler;
            request.ErrorHandler = errorHandler;
            request.UserData = userData;

            AsyncOperation sent = uwr.SendWebRequest();
            sent.completed += (a) => HandleUWRCompleted(uwr, request);

            if (OnRequestSent != null) {
                OnRequestSent(uwr, request.RetryCount);
            }

            return request;
        }

        /// <summary>
        /// Cancels a request.
        /// </summary>
        static internal void CancelRequest<TResponse>(ref Request<TResponse> request) {
            if (request != null) {
                if (request.WebRequest != null) {
                    request.WebRequest.Dispose();
                    request.WebRequest = null;
                }

                request.Handler = null;
                request.ErrorHandler = null;
                request.UserData = null;

                request = null;
            }
        }

        static private void HandleUWRCompleted<T>(UnityWebRequest uwr, Request<T> request) {
            if (request.WebRequest != uwr)
                return;

            object userData = request.UserData;
            ResponseProcessorDelegate<T> handler = request.Handler;
            RequestErrorHandlerDelegate errorHandler = request.ErrorHandler;

            request.WebRequest = null;
            request.UserData = null;
            request.Handler = null;
            request.ErrorHandler = null;

            using(uwr)
            {
                if (uwr.isHttpError || uwr.isNetworkError) {
                    InvokeErrorResponse(uwr, new Error(ReturnStatus.Error_Network, uwr.error), userData, errorHandler);
                } else {
                    try {
                        T response = JsonUtility.FromJson<T>(uwr.downloadHandler.text);
                        if (handler != null) {
                            Error e = handler(response, userData);
                            if (e.Status != ReturnStatus.Success) {
                                InvokeErrorResponse(uwr, e, userData, errorHandler);
                            } else {
                                InvokeSuccessResponse(uwr, e);
                            }
                        } else {
                            InvokeSuccessResponse(uwr, null);
                        }
                    }
                    catch(Exception e) {
                        InvokeErrorResponse(uwr, new Error(ReturnStatus.Error_Exception, e.ToString()), userData, errorHandler);
                    }
                }
            }
        }

        static private void InvokeErrorResponse(UnityWebRequest request, Error error, object userData, RequestErrorHandlerDelegate errorHandler) {
            if (errorHandler != null) {
                errorHandler(error, userData);
            }
            if (OnRequestResponse != null) {
                OnRequestResponse(request, error);
            }
        }

        static private void InvokeErrorResponse(UnityWebRequest request, Error error, DefaultErrorHandlerDelegate errorHandler) {
            if (errorHandler != null) {
                errorHandler(error);
            }
            if (OnRequestResponse != null) {
                OnRequestResponse(request, error);
            }
        }

        static private void InvokeSuccessResponse(UnityWebRequest request, string responseString) {
            if (OnRequestResponse != null) {
                OnRequestResponse(request, new Error(ReturnStatus.Success, responseString));
            }
        }

        static private void InvokeSuccessResponse(UnityWebRequest request, Error error) {
            if (OnRequestResponse != null) {
                OnRequestResponse(request, error);
            }
        }

        #endregion // Sending Request
    
        #region StringBuilder Pool

        static private StringBuilder AllocStringBuilder() {
            if (s_BuilderPoolCount > 0) {
                StringBuilder builder = s_BuilderPool[--s_BuilderPoolCount];
                s_BuilderPool[s_BuilderPoolCount] = null;
                return builder;
            } else {
                return new StringBuilder(128);
            }
        }

        static private void FreeStringBuilder(StringBuilder builder) {
            if (builder != null) {
                builder.Length = 0;
                if (s_BuilderPoolCount < s_BuilderPool.Length) {
                    s_BuilderPool[s_BuilderPoolCount++] = builder;
                }
            }
        }

        #endregion // StringBuilder Pool
    }
}