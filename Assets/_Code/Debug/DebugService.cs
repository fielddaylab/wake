#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Collections;
using System.Diagnostics;
using AquaAudio;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Services;
using BeauUtil.Variants;
using UnityEngine;
using Debug = UnityEngine.Debug;
using BeauUtil.Debugger;
using System.Collections.Generic;

namespace Aqua.Debugging
{
    public partial class DebugService : ServiceBehaviour, IDebuggable
    {
        #region Static

        [ServiceReference] static private DebugService s_Instance;

        static private DMInfo s_RootMenu;

        #endregion // Static

        #region Inspector

        [SerializeField, Required] private Canvas m_Canvas = null;
        [SerializeField] private KeyCode m_ToggleMinimalKey = KeyCode.BackQuote;
        [SerializeField, Required] private CanvasGroup m_MinimalLayer = null;
        [SerializeField, Required] private GameObject m_KeyboardReference = null;
        [SerializeField, Required] private ConsoleTimeDisplay m_TimeDisplay = null;
        [SerializeField, Required] private DMMenuUI m_DebugMenu = null;
        [Space]
        [SerializeField] private bool m_StartOn = true;

        #endregion // Inspector

        [NonSerialized] private DeviceInput m_Input;
        [NonSerialized] private bool m_MinimalOn;
        [NonSerialized] private bool m_FirstMenuToggle;
        [NonSerialized] private bool m_Paused;
        [NonSerialized] private float m_TimeScale = 1;

        private void LateUpdate()
        {
            CheckInput();

            if (m_DebugMenu.isActiveAndEnabled)
                m_DebugMenu.UpdateElements();
        }

        private void CheckInput()
        {
            if (m_Input == null)
                return;

            if (m_Input.KeyPressed(m_ToggleMinimalKey))
            {
                SetMinimalLayer(!m_MinimalOn);
            }

            if (Services.State.IsLoadingScene())
            {
                if (m_DebugMenu.isActiveAndEnabled)
                {
                    m_DebugMenu.gameObject.SetActive(false);
                    Resume();
                }
            }
            else if (m_Input.KeyDown(KeyCode.LeftShift) && m_Input.KeyPressed(KeyCode.W))
            {
                DeviceInput.BlockAll();
                
                if (m_DebugMenu.isActiveAndEnabled)
                {
                    m_DebugMenu.gameObject.SetActive(false);
                    Resume();
                }
                else
                {
                    if (!m_FirstMenuToggle)
                    {
                        m_DebugMenu.GotoMenu(s_RootMenu);
                        m_FirstMenuToggle = true;
                    }
                    m_DebugMenu.gameObject.SetActive(true);
                    SetMinimalLayer(true);
                    Pause();
                }
            }

            if (m_Input.KeyPressed(KeyCode.Backslash))
            {
                m_KeyboardReference.SetActive(!m_KeyboardReference.activeSelf);
            }

            if (m_Input.KeyDown(KeyCode.LeftShift))
            {
                if (m_Input.KeyPressed(KeyCode.Minus))
                {
                    SetTimescale(m_TimeScale / 2);
                }
                else if (m_Input.KeyPressed(KeyCode.Equals))
                {
                    if (m_TimeScale * 2 < 100)
                        SetTimescale(m_TimeScale * 2);
                }
                else if (m_Input.KeyPressed(KeyCode.Alpha0))
                {
                    SetTimescale(1);
                }
            }

            if (m_Input.KeyDown(KeyCode.LeftControl))
            {
                if (m_Input.KeyPressed(KeyCode.Return))
                {
                    TryReloadAssets();
                }
                else if (m_Input.KeyPressed(KeyCode.Space))
                {
                    SkipCutscene();
                }
            }
        }

        private void SetTimescale(float inTimeScale)
        {
            m_TimeScale = inTimeScale;
            if (!m_Paused)
                SyncTimeScale();

            m_TimeDisplay.UpdateTimescale(inTimeScale);
        }

        private void SyncTimeScale()
        {
            Time.timeScale = m_TimeScale;
            Services.Audio.DebugMix.Pitch = m_TimeScale;
            Services.Audio.DebugMix.Volume = Mathf.Clamp01(1 / m_TimeScale);
        }

        private void SkipCutscene()
        {
            if (Services.Pause.IsPaused())
                return;
            
            var cutscene = Services.Script.GetCutscene();
            if (cutscene.IsRunning())
            {
                cutscene.Skip();
            }
        }

        private void DumpConversationLog()
        {
            using (PooledStringBuilder psb = PooledStringBuilder.Create())
            {
                psb.Builder.Append("[DebugService] Dumping conversation history");

                foreach(var record in Services.Data.DialogHistory)
                {
                    psb.Builder.Append('\n').Append(record.ToDebugString());
                }

                Debug.Log(psb.Builder.Flush());
            }
        }

        private void SetMinimalLayer(bool inbOn)
        {
            m_MinimalOn = inbOn;
            m_MinimalLayer.alpha = inbOn ? 1 : 0;
            m_MinimalLayer.blocksRaycasts = inbOn;
            m_Canvas.enabled = inbOn;

            if (!inbOn)
            {
                if (m_DebugMenu.isActiveAndEnabled)
                {
                    m_DebugMenu.gameObject.SetActive(false);
                    Resume();
                }
            }
        }

        #region Pausing

        private void Pause()
        {
            if (m_Paused)
                return;
            
            Time.timeScale = 0;
            Routine.Settings.Paused = true;
            Services.Audio.DebugMix.Pause = true;
            Services.Pause.Pause();
            Services.Input.PushFlags(InputLayerFlags.System, this);
            m_Paused = true;
            m_TimeDisplay.UpdateStateLabel("PAUSED");
        }

        private void Resume()
        {
            if (!m_Paused)
                return;
            
            SyncTimeScale();
            Routine.Settings.Paused = false;
            Services.Audio.DebugMix.Pause = false;
            Services.Pause.Resume();
            Services.Input.PopFlags(this);
            m_Paused = false;
            m_TimeDisplay.UpdateStateLabel("PLAYING");
        }

        #endregion // Pausing

        #region Asset Reloading

        private void OnSceneLoaded(SceneBinding inBinding, object inContext)
        {
            TryReloadAssets();
        }

        private void TryReloadAssets()
        {
            ReloadableAssetCache.TryReloadAll();
        }

        #endregion // Asset Reloading

        #region IService

        protected override void Initialize()
        {
            #if PREVIEW
            SetMinimalLayer(false);
            #else
            SetMinimalLayer(m_StartOn);
            #endif // PREVIEW

            SceneHelper.OnSceneLoaded += OnSceneLoaded;

            m_Canvas.gameObject.SetActive(true);
            m_Input = DeviceInput.Find(m_Canvas);

            transform.FlattenHierarchy();

            RootDebugMenu();
        }

        protected override void Shutdown()
        {
            SceneHelper.OnSceneLoaded -= OnSceneLoaded;
        }

        #endregion // IService

        #region IDebuggable

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus()
        {
            #if DEVELOPMENT

            DMInfo loggingMenu = new DMInfo("Logging");
            RegisterLogToggle(loggingMenu, LogMask.Input);
            RegisterLogToggle(loggingMenu, LogMask.Physics);
            RegisterLogToggle(loggingMenu, LogMask.Scripting);
            RegisterLogToggle(loggingMenu, LogMask.Modeling);
            RegisterLogToggle(loggingMenu, LogMask.Audio);
            RegisterLogToggle(loggingMenu, LogMask.Loading, "Loading");
            RegisterLogToggle(loggingMenu, LogMask.Camera);
            RegisterLogToggle(loggingMenu, LogMask.DataService);
            RegisterLogToggle(loggingMenu, LogMask.UI);
            RegisterLogToggle(loggingMenu, LogMask.Experimentation);
            RegisterLogToggle(loggingMenu, LogMask.Observation);
            RegisterLogToggle(loggingMenu, LogMask.Argumentation);
            RegisterLogToggle(loggingMenu, LogMask.Localization);

            yield return loggingMenu;
            
            #endif // DEVELOPMENT

            yield break;
        }

        #if DEVELOPMENT

        static private void RegisterLogToggle(DMInfo inMenu, LogMask inMask, string inName = null)
        {
            inMenu.AddToggle(inName ?? inMask.ToString(), () => IsLogging(inMask),
            (b) => {
                if (b)
                    AllowLogs(inMask);
                else
                    DisallowLogs(inMask);
            });
        }

        #endif // DEVELOPMENT

        #endregion // IDebuggable
    
        #region Logging Stuff

        #if DEVELOPMENT

        static private uint s_LoggingMask = (uint) (LogMask.DEFAULT);

        static private readonly object[] NullArgs = Array.Empty<object>();

        static internal void AllowLogs(LogMask inMask)
        {
            s_LoggingMask |= (uint) inMask;
        }

        static internal void DisallowLogs(LogMask inMask)
        {
            s_LoggingMask &= ~(uint) inMask;
        }

        static public bool IsLogging(LogMask inMask) { return (s_LoggingMask & (uint) inMask) != 0; }

        static public void Log(LogMask inMask, string inMessage) { if ((s_LoggingMask & (uint) inMask) != 0) Debug.LogFormat(inMessage, NullArgs); }
        static public void Log(LogMask inMask, string inMessage, object inArg0) { if ((s_LoggingMask & (uint) inMask) != 0) Debug.LogFormat(string.Format(inMessage, inArg0), NullArgs); }
        static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1) { if ((s_LoggingMask & (uint) inMask) != 0) Debug.LogFormat(string.Format(inMessage, inArg0, inArg1), NullArgs); }
        static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1, object inArg2) { if ((s_LoggingMask & (uint) inMask) != 0) Debug.LogFormat(string.Format(inMessage, inArg0, inArg1, inArg2), NullArgs); }
        static public void Log(LogMask inMask, string inMessage, params object[] inParams) { if ((s_LoggingMask & (uint) inMask) != 0) Debug.LogFormat(string.Format(inMessage, inParams), NullArgs); }

        #else

        static internal void AllowLogs(LogMask inMask) { }
        static internal void DisallowLogs(LogMask inMask) { }
        static public bool IsLogging(LogMask inMask) { return false; }

        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, object inArg0) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1, object inArg2) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, params object[] inParams) { }

        #endif // DEVELOPMENT

        #endregion // Logging Stuff
    
        #region Debug Menu

        #if DEVELOPMENT

        static public DMInfo RootDebugMenu() { return s_RootMenu ?? (s_RootMenu = new DMInfo("Debug", 16)); }
        static public DMInfo NewDebugMenu(string inLabel, int inCapacity = 0)
        {
            return new DMInfo(inLabel, inCapacity);
        }

        #else

        static public DMInfo RootDebugMenu() { return null; }
        static public DMInfo NewDebugMenu(string inLabel, int inCapacity = 0) { return null; }

        #endif // DEVELOPMENT

        #endregion // Debug Menu
    }
}