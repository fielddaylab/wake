#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Collections;
using System.Globalization;
using BeauUtil;
using Aqua.DebugConsole;
using UnityEngine;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Aqua.Shared.Editor")]

namespace Aqua
{
    [DefaultExecutionOrder(int.MinValue)]
    public class Bootstrap : MonoBehaviour
    {
        #pragma warning disable CS0414

        [SerializeField, Required] private DebugService m_Debug = null;
        [SerializeField, Required] private string[] m_IgnoredSceneNames = null;

        #pragma warning restore CS0414

        private void Awake()
        {
            BuildInfo.Load();
            Input.multiTouchEnabled = false;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            foreach(var sceneName in m_IgnoredSceneNames)
            {
                SceneHelper.IgnoreSceneByName(sceneName);
            }

            #if !DEVELOPMENT
            Debug.Log("[Bootstrap] Debug mode disabled");
            DestroyImmediate(m_Debug.gameObject);
            #else
            Debug.Log("[Bootstrap] Debug mode enabled");
            #endif // !DEVELOPMENT

            Services.AutoSetup(gameObject);
        }

        private void OnDestroy()
        {
            Services.Shutdown();
        }
    }
}