using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProtoAqua
{
    public class FastBootController : MonoBehaviour, ISceneLoadHandler
    {
        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            var queryParams = Services.Data.PeekQueryParams();
            string targetScene = null;

            if (queryParams != null)
            {
                string activity = queryParams.Get("activity");
                if (activity == "sim")
                {
                    targetScene = "SimScene";
                }
                else if (activity == "observation")
                {
                    targetScene = "SeaSceneTest";
                }
                else if (queryParams.Contains("scenarioData") || queryParams.Contains("scenarioId"))
                {
                    targetScene = "SimScene";
                }
            }

            if (!string.IsNullOrEmpty(targetScene))
            {
                Services.State.LoadScene("*" + targetScene, null);
            }
            else
            {
                int buildIdx = SceneHelper.ActiveScene().BuildIndex + 1;
                SceneBinding nextScene = SceneHelper.FindSceneByIndex(buildIdx);
                Services.State.LoadScene(nextScene);
            }
        }
    }
}