#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Collections;
using System.Globalization;
using BeauUtil;
using Aqua.Debugging;
using UnityEngine;
using System.Runtime.CompilerServices;
using BeauUtil.Variants;
using System;
using BeauUtil.Debugger;

[assembly: InternalsVisibleTo("Aqua.Shared.Editor")]

namespace Aqua
{
    [DefaultExecutionOrder(int.MinValue)]
    public class BootParams : MonoBehaviour
    {
        #pragma warning disable CS0414

        [SerializeField, Required] private string[] m_IgnoredSceneNames = null;

        [Header("-- DEBUG --")]
        [SerializeField, Required] private DebugService m_Debug = null;
        [SerializeField] private string m_DebugUrl = string.Empty;

        #pragma warning restore CS0414

        static private QueryParams s_Args;

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

            string url;
            #if UNITY_EDITOR
            url = m_DebugUrl;
            #else
            url = Application.absoluteURL;
            #endif // UNITY_EDITOR

            s_Args = new QueryParams();
            s_Args.TryParse(url);

            LoadBootParamsFirstPass();
            Services.AutoSetup(gameObject);
        }

        private void OnDestroy()
        {
            Services.Shutdown();
        }
    
        #region Boot Parameters

        static private void LoadBootParamsFirstPass()
        {
            QueryParams p = s_Args;

            // mute
            if (p.Contains("mute"))
            {
                AudioListener.volume = 0;
            }

            // debug
            StringSlice debug = p.Get("debug");
            if (!debug.IsEmpty)
            {
                StringSlice[] categories = debug.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                LogMask extraCategories = 0;
                foreach(var category in categories)
                {
                    if (Enum.TryParse<LogMask>(category.ToString(), true, out LogMask nextCategory))
                        extraCategories |= nextCategory;
                }

                if (extraCategories != 0)
                {
                    Debug.LogFormat("Activating debug categories '{0}'", extraCategories.ToString());
                    DebugService.AllowLogs(extraCategories);
                }
            }
        }

        static public QueryParams Args { get { return s_Args; } }

        #endregion // Boot Parameters
    }
}