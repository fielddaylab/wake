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

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor;
#endif // UNITY_EDITOR

[assembly: InternalsVisibleTo("Aqua.Shared.Editor")]

namespace Aqua
{
    [DefaultExecutionOrder(int.MinValue)]
    public class BootParams : MonoBehaviour
    {
        [SerializeField, Required] private string[] m_IgnoredSceneNames = null;

        #pragma warning disable CS0414

        [Header("-- DEBUG --")]
        [SerializeField, Required] private DebugService m_Debug = null;
        [SerializeField] private string m_DebugUrl = string.Empty;

        #pragma warning restore CS0414

        #if UNITY_EDITOR
        static private bool s_StartFlag = true;
        #endif // UNITY_EDITOR
        
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
            Assert.NotNull(m_Debug); // should initialize assert error catching
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
                Debug.LogFormat("[BootParams] Muting volume");
            }

            // antialiasing
            if (p.Contains("forceAA"))
            {
                Variant aa = p.GetVariant("forceAA");
                int aaVal = 2;
                if (aa.Type != VariantType.Null)
                    aaVal = aa.AsInt();
                QualitySettings.antiAliasing = aaVal;
                Debug.LogFormat("[BootParams] Antialiasing set to {0}", aaVal);
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
                    Debug.LogFormat("[BootParams] Activating debug categories '{0}'", extraCategories.ToString());
                    DebugService.AllowLogs(extraCategories);
                }
            }
        }

        static internal void ClearStartFlag()
        {
            #if UNITY_EDITOR
            s_StartFlag = false;
            #endif // UNITY_EDITOR
        }

        static public QueryParams Args { get { return s_Args; } }

        #if UNITY_EDITOR
        static public bool BootedFromCurrentScene
        {
            get { return s_StartFlag; }
        }
        #else
        public const bool BootedFromCurrentScene = false;
        #endif // UNITY_EDITOR

        #endregion // Boot Parameters

        #if UNITY_EDITOR

        private void OnValidate()
        {
            if (EditorApplication.isPlaying)
                return;

            PrefabStage stage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (stage != null)
                return;
            
            int editorSceneBuildIndex = EditorSceneManager.GetActiveScene().buildIndex;
            if (editorSceneBuildIndex > 0)
            {
                if (!gameObject.CompareTag("EditorOnly"))
                {
                    EditorApplication.delayCall += () => { if (this) gameObject.tag = "EditorOnly"; };
                }
            }
            else
            {
                if (!gameObject.CompareTag("Untagged"))
                {
                    EditorApplication.delayCall += () => { if (this) gameObject.tag = "Untagged"; };
                }
            }
        }

        #endif // UNITY_EDITOR
    }
}