using System;

namespace OGD {
    static public class GameState {

        private struct RequestGameStateResponse {
            public string msg;
            public string status;
            public string[] val;
        }

        static private Core.Request<RequestGameStateResponse> s_CurrentRequestGameState;
        static private Core.Request<Core.DefaultResponse> s_CurrentPostGameState;

        /// <summary>
        /// Requests the latest state for the given player id.
        /// </summary>
        static public IDisposable RequestLatestState(string playerId, Action<string> onSuccess, Core.DefaultErrorHandlerDelegate onError, int retryCount) {
            Core.CancelRequest(ref s_CurrentRequestGameState);

            Core.Query query = Core.NewQuery("/player/{0}/game/{1}/state", playerId, Core.GameId());
            return s_CurrentRequestGameState = Core.Get<RequestGameStateResponse>(query, (response, data) => {
                s_CurrentRequestGameState = null;

                var status = Core.ParseStatus(response.status);
                if (status == Core.ReturnStatus.Success && response.val != null && response.val.Length > 0) {
                    onSuccess?.Invoke(response.val[0]);
                    return Core.Error.Success;
                } else {
                    Core.Error error = new Core.Error(status, response.msg);
                    onError?.Invoke(error);
                    return error;
                }
            }, (error, data) => {
                s_CurrentRequestGameState = null;

                onError?.Invoke(error);
            }, null, retryCount);
        }

        /// <summary>
        /// Pushes the state for the given player id.
        /// </summary>
        static public IDisposable PushState(string playerId, string state, Action onSuccess, Core.DefaultErrorHandlerDelegate onError, int retryCount) {
            Core.CancelRequest(ref s_CurrentPostGameState);

            Core.Query query = Core.NewQuery("/player/{0}/game/{1}/state", playerId, Core.GameId());
            Core.QueryArg(ref query, "state", state);

            return s_CurrentPostGameState = Core.Post<Core.DefaultResponse>(query, (response, data) => {
                s_CurrentPostGameState = null;

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
                s_CurrentPostGameState = null;

                onError?.Invoke(error);
            }, null, retryCount);
        }
    }
}