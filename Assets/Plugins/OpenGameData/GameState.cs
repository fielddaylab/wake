using System;

namespace OGD {
    static public class GameState {
        
        private struct RequestGameStateResponse {
            public string message;
            public string[] states;
        }

        private struct PostGameStateResponse {
            public string message;
        }

        static private Core.Request<RequestGameStateResponse> s_CurrentRequestGameState;
        static private Core.Request<PostGameStateResponse> s_CurrentPostGameState;

        /// <summary>
        /// Requests the latest state for the given player id.
        /// </summary>
        static public void RequestLatestState(string playerId, Action<string> onSuccess, Action<string> onError) {
            Core.CancelRequest(ref s_CurrentRequestGameState);

            Core.Query query = Core.NewQuery("/player/{0}/game/{1}/state", playerId, Core.GameId());
            s_CurrentRequestGameState = Core.Get<RequestGameStateResponse>(query, (response, data) => {
                s_CurrentRequestGameState = null;
                if (response.states != null && response.states.Length > 0) {
                    onSuccess?.Invoke(response.states[0]);
                } else {
                    onError?.Invoke(response.message);
                }
            }, (error, data) => {
                s_CurrentRequestGameState = null;
                onError?.Invoke(error);
            }, null);
        }

        /// <summary>
        /// Pushes the state for the given player id.
        /// </summary>
        static public void PushState(string playerId, string state, Action onSuccess, Action<string> onError) {
            Core.CancelRequest(ref s_CurrentPostGameState);

            Core.Query query = Core.NewQuery("/player/{0}/game/{1}/state", playerId, Core.GameId());
            Core.QueryArg(ref query, "state", state);

            s_CurrentPostGameState = Core.Get<PostGameStateResponse>(query, (response, data) => {
                s_CurrentPostGameState = null;
                if (response.message != null && response.message.StartsWith("SUCCESS")) {
                    onSuccess?.Invoke();
                } else {
                    onError?.Invoke(response.message);
                }
            }, (error, data) => {
                s_CurrentPostGameState = null;
                onError?.Invoke(error);
            }, null);
        }
    }
}