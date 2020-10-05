using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProtoAqua
{
    public class FastBootController : MonoBehaviour, ISceneLoadHandler
    {
        [SerializeField, Required] private string m_DefaultScene = "DebugTitle";

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            var queryParams = Services.Data.PeekQueryParams();
            string targetScene = m_DefaultScene;

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

            Services.State.LoadScene("*" + targetScene, null);
        }
    }
}