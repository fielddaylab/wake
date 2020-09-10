#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Collections;
using System.Globalization;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject m_Debug = null;
        [SerializeField] private string[] m_IgnoredSceneNames = null;

        private void Awake()
        {
            #if !DEVELOPMENT
            Debug.Log("[Bootstrap] Debug mode disabled");
            DestroyImmediate(m_Debug);
            #else
            Debug.Log("[Bootstrap] Debug mode enabled");
            m_Debug = null;
            #endif // !DEVELOPMENT

            BuildInfo.Load();
            Input.multiTouchEnabled = false;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Services.AutoSetup(gameObject);

            foreach(var sceneName in m_IgnoredSceneNames)
            {
                SceneHelper.IgnoreSceneByName(sceneName);
            }
        }
    }
}