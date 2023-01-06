using System;

namespace OGD {
    static public class Player {

        static private Core.Request<Core.DefaultResponse> s_CurrentNewIdRequest;
        static private Core.Request<Core.DefaultResponse> s_CurrentClaimIdRequest;

        /// <summary>
        /// Generates a new id.
        /// </summary>
        static public IDisposable NewId(Action<string> onNewId, Core.DefaultErrorHandlerDelegate onError) {
            Core.CancelRequest(ref s_CurrentNewIdRequest);

            var query = Core.NewQuery("/player/");
            return s_CurrentNewIdRequest = Core.Get<Core.DefaultResponse>(query, (response, data) => {
                s_CurrentNewIdRequest = null;

                var status = Core.ParseStatus(response.status);
                if (status == Core.ReturnStatus.Success) {
                    onNewId?.Invoke(response.val[0]);
                    return Core.Error.Success;;
                } else {
                    Core.Error error = new Core.Error(status, response.msg);
                    onError?.Invoke(error);
                    return error;
                }
            }, (error, data) => {
                s_CurrentNewIdRequest = null;
                onError?.Invoke(error);
            }, null);
        }

        /// <summary>
        /// Claims the given id.
        /// </summary>
        static public IDisposable ClaimId(string id, string name, Action onSuccess, Core.DefaultErrorHandlerDelegate onError) {
            Core.CancelRequest(ref s_CurrentClaimIdRequest);

            var query = Core.NewQuery("/player/");
            Core.QueryArg(ref query, "player_id", id);
            Core.QueryArg(ref query, "name", name);

            return s_CurrentClaimIdRequest = Core.Put<Core.DefaultResponse>(query, (response, data) => {
                s_CurrentClaimIdRequest = null;

                var status = Core.ParseStatus(response.status);
                if (status == Core.ReturnStatus.Success) {
                    onSuccess?.Invoke();
                    return Core.Error.Success;
                } else {
                    Core.Error error = new Core.Error(status, response.msg);
                    onError?.Invoke(error);
                    return error;
                }
            }, (error, data) => {
                s_CurrentClaimIdRequest = null;

                onError?.Invoke(error);
            }, null);
        }
    }
}