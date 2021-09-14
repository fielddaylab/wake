using System;

namespace OGD {
    static public class Player {

        private struct NewIdResponse {
            public string id;
            public string message;
        }

        private struct ClaimIdResponse {
            public string message;
        }

        static private Core.Request<NewIdResponse> s_CurrentNewIdRequest;
        static private Core.Request<ClaimIdResponse> s_CurrentClaimIdRequest;

        /// <summary>
        /// Generates a new id.
        /// </summary>
        static public void NewId(Action<string> onNewId, Action<string> onError) {
            Core.CancelRequest(ref s_CurrentNewIdRequest);

            var query = Core.NewQuery("/player/");
            s_CurrentNewIdRequest = Core.Get<NewIdResponse>(query, (response, data) => {
                s_CurrentNewIdRequest = null;

                if (!string.IsNullOrEmpty(response.id)) {
                    onNewId?.Invoke(response.id);
                } else {
                    onError?.Invoke(response.message);
                }
            }, (error, data) => {
                onError?.Invoke(error);
            }, null);
        }

        /// <summary>
        /// Claims the given id.
        /// </summary>
        static public void ClaimId(string id, string name, Action onSuccess, Action<string> onError) {
            Core.CancelRequest(ref s_CurrentClaimIdRequest);

            var query = Core.NewQuery("/player/");
            Core.QueryArg(ref query, "player_id", id);
            Core.QueryArg(ref query, "name", name);

            s_CurrentClaimIdRequest = Core.Put<ClaimIdResponse>(query, (response, data) => {
                s_CurrentClaimIdRequest = null;

                if (response.message != null && response.message.StartsWith("SUCCESS")) {
                    onSuccess?.Invoke();
                } else {
                    onError?.Invoke(response.message);
                }
            }, (error, data) => {
                onError?.Invoke(error);
            }, null);
        }
    }
}