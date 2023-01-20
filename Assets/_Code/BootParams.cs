#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
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
using System.Collections.Generic;
using BeauRoutine;
using System.Text;

[assembly: InternalsVisibleTo("Aqua.Shared.Editor")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace Aqua
{
    [DefaultExecutionOrder(int.MinValue)]
    public class BootParams : MonoBehaviour
    {
        [SerializeField, Required] private string[] m_IgnoredSceneNames = null;

        #pragma warning disable CS0414

        [Header("-- DEBUG --")]
        [SerializeField] private string m_DebugUrl = string.Empty;

        #pragma warning restore CS0414

        #if UNITY_EDITOR
        static private bool s_StartFlag = true;
        #endif // UNITY_EDITOR
        
        private bool m_HasPersisted = false;
        static private QueryParams s_Args;

        private void Awake()
        {
            m_HasPersisted = true;
            
            BuildInfo.Load();
            Input.multiTouchEnabled = false;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            foreach(var sceneName in m_IgnoredSceneNames)
            {
                SceneHelper.IgnoreSceneByName(sceneName);
            }

            #if !DEVELOPMENT
            Debug.Log("[BootParams] Debug mode disabled");
            #if UNITY_EDITOR
            DebugService debug = GetComponentInChildren<DebugService>();
            if (debug)
                DestroyImmediate(debug.gameObject);
            #endif // UNITY_EDITOR
            #else
            DebugService debug = GetComponentInChildren<DebugService>();
            Assert.NotNull(debug); // should initialize assert error catching
            Debug.Log("[BootParams] Debug mode enabled");
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

            TransformHelper.FlattenHierarchy(transform);

            #if DEVELOPMENT

            List<DMInfo> debugMenus = new List<DMInfo>(8);
            foreach(var debugService in Services.AllDebuggable())
            {
                Log.Msg("[BootParams] Getting debug menus from {0}", debugService);
                foreach(var menu in debugService.ConstructDebugMenus())
                {
                    Assert.NotNull(menu, "Provided menu is null");
                    Log.Msg("[BootParams] ...menu {0}", menu.Header.Label);
                    debugMenus.Add(menu);
                }
            }
            Debug.LogFormat("[BootParams] Found '{0}' debug menus", debugMenus.Count);
            debugMenus.Sort((a, b) => a.Header.Label.CompareTo(b.Header.Label));
            
            DMInfo rootMenu = DebugService.RootDebugMenu();
            foreach(var menu in debugMenus)
            {
                Debug.LogFormat("[BootParams] Adding debug menu '{0}'", menu.Header.Label);
                rootMenu.AddSubmenu(menu);
            }

            #endif // DEVELOPMENT

            LoadBootParamsSecondPass();
            Async.InvokeAsync(RetrieveExpensiveSystemResources);

            CrashHandler.Register();
            CrashHandler.DisplayCrash += OnCrash;
            #if !UNITY_EDITOR
            CrashHandler.Enabled = true;
            #endif // !UNITY_EDITOR
        }

        private void OnDestroy()
        {
            if (m_HasPersisted)
            {
                CrashHandler.Deregister();
                Services.Shutdown();
            }
        }
    
        #region Boot Parameters

        static private void LoadBootParamsFirstPass()
        {
            QueryParams p = s_Args;

            // mute
            if (p.Contains("mute"))
            {
                AudioListener.volume = 0;
                Log.Msg("[BootParams] Muting volume");
            }

            // antialiasing
            if (p.Contains("forceAA"))
            {
                Variant aa = p.GetVariant("forceAA");
                int aaVal = 2;
                if (aa.Type != VariantType.Null)
                    aaVal = aa.AsInt();
                QualitySettings.antiAliasing = aaVal;
                Log.Msg("[BootParams] Antialiasing set to {0}", aaVal);
            }

            if (p.Contains("clearData"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Log.Msg("[BootParams] All save data deleted");
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
                    Log.Msg("[BootParams] Activating debug categories '{0}'", extraCategories);
                    DebugService.AllowLogs(extraCategories);
                }
            }
        }
        
        static private void LoadBootParamsSecondPass()
        {
            QueryParams p = s_Args;

            if (p.Contains("disableSaving"))
            {
                Services.Data.ForceNoSaving(true);
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
    
        static private void RetrieveExpensiveSystemResources() {
            Char.IsWhiteSpace('0');
        }

        static private void OnCrash(Exception e, string error, out string context) {
            Time.timeScale = 0;
            Routine.Settings.Paused = true;
            Services.Audio.DebugMix.Pause = true;
            Services.Pause.Pause();
            Services.Input.PushFlags(0);

            DebugService debug = FindObjectOfType<DebugService>();
            if (debug != null) {
                debug.enabled = false;
            }

            var save = Save.Current;
            StringHash32 jobId;

            StringBuilder contextBuilder = new StringBuilder(512);
            contextBuilder.Append("Profile Name:\t").Append(save?.Id ?? "(not loaded)");
            contextBuilder.Append("\nCurrent Job:\t");
            if (save != null) {
                if ((jobId = save.Jobs.CurrentJobId).IsEmpty) {
                    contextBuilder.Append("(no job)");
                } else {
                    contextBuilder.Append(Assets.NameOf(jobId));
                }
            } else {
                contextBuilder.Append("(not loaded)");
            }
            contextBuilder.Append("\nCurrent Scene:\t").Append(SceneHelper.ActiveScene().Name);
            if (Services.State.IsLoadingScene()) {
                contextBuilder.Append(" (loading)");
            } else {
                contextBuilder.Append(" (loaded)");
            }
            context = contextBuilder.Flush();
        }
    }
}