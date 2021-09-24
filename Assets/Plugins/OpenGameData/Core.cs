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

            Unknown
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

        public delegate void DefaultErrorHandlerDelegate(ReturnStatus status, string msg);
        
        internal delegate void ResponseProcessorDelegate<TResponse>(TResponse response, object userData);
        internal delegate void RequestErrorHandlerDelegate(string error, object userData);

        static private string s_ServerAddress = string.Empty;
        static private string s_GameId = string.Empty;

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
                builder.ArgsBuilder = new StringBuilder();
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

            public void Dispose() {
                var request = this;
                Core.CancelRequest(ref request);
            }
        }

        /// <summary>
        /// Submits a query as a GET request.
        /// </summary>
        static internal Request<T> Get<T>(Query query, ResponseProcessorDelegate<T> handler, RequestErrorHandlerDelegate errorHandler, object userData) {
            return SendRequest<T>(query, UnityWebRequest.kHttpVerbGET, handler, errorHandler, userData);
        }

        /// <summary>
        /// Submits a query as a PUT request.
        /// </summary>
        static internal Request<T> Put<T>(Query query, ResponseProcessorDelegate<T> handler, RequestErrorHandlerDelegate errorHandler, object userData) {
            return SendRequest<T>(query, UnityWebRequest.kHttpVerbPUT, handler, errorHandler, userData);
        }

        /// <summary>
        /// Submits a query as a POST request.
        /// </summary>
        static internal Request<T> Post<T>(Query query, ResponseProcessorDelegate<T> handler, RequestErrorHandlerDelegate errorHandler, object userData) {
            return SendRequest<T>(query, UnityWebRequest.kHttpVerbPOST, handler, errorHandler, userData);
        }

        static private Request<T> SendRequest<T>(Query query, string method, ResponseProcessorDelegate<T> handler, RequestErrorHandlerDelegate errorHandler, object userData) {
            string fullPath = s_ServerAddress + query.API;
            if (query.ArgsBuilder != null && query.ArgsBuilder.Length > 0) {
                fullPath += query.ArgsBuilder.ToString();
                query.ArgsBuilder.Length = 0;
            }

            Request<T> request = new Request<T>();
            UnityWebRequest uwr = new UnityWebRequest(fullPath, method);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            
            request.WebRequest = uwr;
            request.Handler = handler;
            request.ErrorHandler = errorHandler;
            request.UserData = userData;

            AsyncOperation sent = uwr.SendWebRequest();
            sent.completed += (a) => HandleUWRCompleted(uwr, request);

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
                    if (errorHandler != null)
                        errorHandler(uwr.error, userData);
                } else {
                    try {
                        T response = JsonUtility.FromJson<T>(uwr.downloadHandler.text);
                        if (handler != null)
                            handler(response, userData);
                    }
                    catch(Exception e) {
                        if (errorHandler != null)
                            errorHandler(e.ToString(), userData);
                    }
                }
            }
        }

        #endregion // Sending Request
    }
}