#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Diagnostics;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Services;
using UnityEngine;
using Debug = UnityEngine.Debug;
using BeauUtil.Debugger;
using System.Collections.Generic;
using TMPro;

namespace Aqua.Debugging
{
    [ServiceDependency(typeof(EventService))]
    public partial class DebugService : ServiceBehaviour, IDebuggable
    {
        #if DEVELOPMENT

        #region Static

        [ServiceReference, UnityEngine.Scripting.Preserve] static private DebugService s_Instance;

        static private DMInfo s_RootMenu;

        #endregion // Static

        #region Inspector

        [SerializeField, Required] private Canvas m_Canvas = null;
        [SerializeField] private KeyCode m_ToggleMinimalKey = KeyCode.BackQuote;
        [SerializeField, Required] private CanvasGroup m_MinimalLayer = null;
        [SerializeField, Required] private GameObject m_KeyboardReference = null;
        [SerializeField, Required] private ConsoleTimeDisplay m_TimeDisplay = null;
        [SerializeField, Required] private ConsoleCamera m_DebugCamera = null;
        [SerializeField, Required] private DMMenuUI m_DebugMenu = null;
        [SerializeField, Required] private GameObject m_CameraReference = null;
        [SerializeField, Required] private TMP_Text m_StreamingDebugText = null;
        [Space]
        [SerializeField] private bool m_StartOn = true;

        #endregion // Inspector

        [NonSerialized] private DeviceInput m_Input;
        [NonSerialized] private bool m_MinimalOn;
        [NonSerialized] private bool m_FirstMenuToggle;
        [NonSerialized] private bool m_Paused;
        [NonSerialized] private float m_TimeScale = 1;
        [NonSerialized] private bool m_VisibilityWhenDebugMenuOpened;
        [NonSerialized] private Vector2 m_CameraCursorPivot;
        [NonSerialized] private uint m_LastKnownStreaming;

        private void LateUpdate()
        {
            CheckInput();

            if (m_DebugMenu.isActiveAndEnabled)
                m_DebugMenu.UpdateElements();

            if (m_MinimalOn)
            {
                if (Ref.Replace(ref m_LastKnownStreaming, Streaming.LoadCount()))
                {
                    m_StreamingDebugText.SetText(m_LastKnownStreaming.ToString());
                }
            }
        }

        private void CheckInput()
        {
            if (m_Input == null)
                return;

            if (m_Input.KeyPressed(m_ToggleMinimalKey))
            {
                SetMinimalLayer(!m_MinimalOn);
            }

            if (Script.IsLoading)
            {
                ClearDebugCamera();
                if (m_DebugMenu.isActiveAndEnabled)
                {
                    m_DebugMenu.gameObject.SetActive(false);
                    Resume();
                }
            }
            else
            {
                UpdateMenuControls();
                UpdateCameraControls();
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

        private void UpdateMenuControls()
        {
            if (m_Input.KeyDown(KeyCode.LeftShift) && m_Input.KeyPressed(KeyCode.W))
            {
                DeviceInput.BlockAll();
                
                if (m_DebugMenu.isActiveAndEnabled)
                {
                    m_DebugMenu.gameObject.SetActive(false);
                    Resume();
                    SetMinimalLayer(m_VisibilityWhenDebugMenuOpened);
                }
                else
                {
                    if (!m_FirstMenuToggle)
                    {
                        m_DebugMenu.GotoMenu(s_RootMenu);
                        m_FirstMenuToggle = true;
                    }
                    m_DebugMenu.gameObject.SetActive(true);
                    m_VisibilityWhenDebugMenuOpened = m_MinimalOn;
                    SetMinimalLayer(true);
                    Pause();
                }
            }

            if (m_DebugMenu.isActiveAndEnabled)
            {
                if (m_Input.MousePressed(1))
                    m_DebugMenu.TryPopMenu();
                if (m_Input.KeyPressed(KeyCode.LeftArrow) || m_Input.KeyPressed(KeyCode.A))
                    m_DebugMenu.TryPreviousPage();
                if (m_Input.KeyPressed(KeyCode.RightArrow) || m_Input.KeyPressed(KeyCode.D))
                    m_DebugMenu.TryNextPage();
            }
        }

        private void UpdateCameraControls()
        {
            if (m_Input.KeyDown(KeyCode.LeftShift) && m_Input.KeyPressed(KeyCode.C))
            {
                DeviceInput.BlockAll();

                if (m_DebugCamera.Camera())
                {
                    ClearDebugCamera();
                }
                else
                {
                    SetMinimalLayer(true);
                    m_CameraReference.SetActive(true);
                    m_DebugCamera.SetCamera(Services.Camera.Current, Services.Camera.RootTransform);
                }
            }

            if (m_DebugCamera.Camera())
            {
                Vector3 move = default;
                bool bHadInput = false;

                if (m_Input.KeyDown(KeyCode.LeftArrow) || m_Input.KeyDown(KeyCode.A))
                {
                    move.x -= 1;
                    bHadInput = true;
                }
                if (m_Input.KeyDown(KeyCode.RightArrow) || m_Input.KeyDown(KeyCode.D))
                {
                    move.x += 1;
                    bHadInput = true;
                }
                if (m_Input.KeyDown(KeyCode.UpArrow) || m_Input.KeyDown(KeyCode.W))
                {
                    move.z += 1;
                    bHadInput = true;
                }
                if (m_Input.KeyDown(KeyCode.DownArrow) || m_Input.KeyDown(KeyCode.S))
                {
                    move.z -= 1;
                    bHadInput = true;
                }
                if (m_Input.KeyDown(KeyCode.Q))
                {
                    move.y -= 1;
                    bHadInput = true;
                }
                if (m_Input.KeyDown(KeyCode.E))
                {
                    move.y += 1;
                    bHadInput = true;
                }

                if (m_Input.KeyDown(KeyCode.LeftShift))
                    move *= 4;

                m_DebugCamera.MoveRelative(move * Time.unscaledDeltaTime);

                if (m_Input.MousePressed(1))
                {
                    m_CameraCursorPivot = m_Input.ScreenMousePosition();
                    bHadInput = true;

                    #if UNITY_EDITOR
                    UnityEditor.EditorGUIUtility.SetWantsMouseJumping(1);
                    #endif // UNITY_EDITOR
                }
                else if (m_Input.MouseDown(1))
                {
                    Vector2 newPos = m_Input.ScreenMousePosition();
                    Vector2 mouseShift = newPos - m_CameraCursorPivot;
                    m_CameraCursorPivot = newPos;

                    Vector3 eulerShift;
                    eulerShift.x = -mouseShift.y;
                    eulerShift.y = mouseShift.x;
                    eulerShift.z = 0;

                    m_DebugCamera.Rotate(eulerShift * Time.unscaledDeltaTime);
                    bHadInput = true;
                }
                else
                {
                    #if UNITY_EDITOR
                    UnityEditor.EditorGUIUtility.SetWantsMouseJumping(0);
                    #endif // UNITY_EDITOR
                }

                if (bHadInput)
                    DeviceInput.BlockAll();
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
            if (Script.IsPaused)
                return;
            
            var cutscene = Services.Script.GetCutscene();
            if (cutscene.IsRunning())
            {
                cutscene.Skip();
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

                ClearDebugCamera();
            }
        }

        private void ClearDebugCamera()
        {
            if (m_DebugCamera.SetCamera(null))
            {
                Services.Camera.DebugResetToLastState();
                m_CameraReference.SetActive(false);

                #if UNITY_EDITOR
                UnityEditor.EditorGUIUtility.SetWantsMouseJumping(0);
                #endif // UNITY_EDITOR
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

        private void HideMenu()
        {
            if (m_DebugMenu.isActiveAndEnabled)
            {
                m_DebugMenu.gameObject.SetActive(false);
                Resume();
            }
        }

        #region IService

        protected override void Initialize()
        {
            #if !UNITY_EDITOR
            SetMinimalLayer(false);
            #else
            SetMinimalLayer(m_StartOn);
            #endif // PREVIEW

            SceneHelper.OnSceneLoaded += OnSceneLoaded;
            Services.Events.Register(GameEvents.ProfileLoaded, HideMenu, this);

            m_Canvas.gameObject.SetActive(true);
            m_Input = DeviceInput.Find(m_Canvas);

            transform.FlattenHierarchy();

            #if DEVELOPMENT
            RootDebugMenu();
            #endif // DEVELOPMENT
        }

        protected override void Shutdown()
        {
            SceneHelper.OnSceneLoaded -= OnSceneLoaded;
            Services.Events?.DeregisterAll(this);
        }

        #endregion // IService

        #region IDebuggable

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus()
        {
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
            RegisterLogToggle(loggingMenu, LogMask.Time);
            yield return loggingMenu;
        }

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

        #endregion // IDebuggable

        #endif // DEVELOPMENT
    
        #region Logging Stuff

        #if DEVELOPMENT

        static private uint s_LoggingMask = (uint) (LogMask.DEFAULT);

        static internal void AllowLogs(LogMask inMask)
        {
            s_LoggingMask |= (uint) inMask;
        }

        static internal void DisallowLogs(LogMask inMask)
        {
            s_LoggingMask &= ~(uint) inMask;
        }

        static public bool IsLogging(LogMask inMask) { return (s_LoggingMask & (uint) inMask) != 0; }

        static public void Log(LogMask inMask, string inMessage) { if ((s_LoggingMask & (uint) inMask) != 0) BeauUtil.Debugger.Log.Msg(inMessage); }
        static public void Log(LogMask inMask, string inMessage, object inArg0) { if ((s_LoggingMask & (uint) inMask) != 0) BeauUtil.Debugger.Log.Msg(inMessage, inArg0); }
        static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1) { if ((s_LoggingMask & (uint) inMask) != 0) BeauUtil.Debugger.Log.Msg(inMessage, inArg0, inArg1); }
        static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1, object inArg2) { if ((s_LoggingMask & (uint) inMask) != 0) BeauUtil.Debugger.Log.Msg(inMessage, inArg0, inArg1, inArg2); }
        static public void Log(LogMask inMask, string inMessage, params object[] inParams) { if ((s_LoggingMask & (uint) inMask) != 0) BeauUtil.Debugger.Log.Msg(inMessage, inParams); }

        static public void Hide() { s_Instance.HideMenu(); }

        #else

        static internal void AllowLogs(LogMask inMask) { }
        static internal void DisallowLogs(LogMask inMask) { }
        static public bool IsLogging(LogMask inMask) { return false; }

        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, object inArg0) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, object inArg0, object inArg1, object inArg2) { }
        [Conditional("ALWAYS_EXCLUDE")] static public void Log(LogMask inMask, string inMessage, params object[] inParams) { }

        [Conditional("ALWAYS_EXCLUDE")] static public void Hide() { }

        #endif // DEVELOPMENT

        #endregion // Logging Stuff
    
        #region Debug Menu

        #if DEVELOPMENT

        static public DMInfo RootDebugMenu() { return s_RootMenu ?? (s_RootMenu = new DMInfo("Debug", 16)); }

        #endif // DEVELOPMENT

        #endregion // Debug Menu
    }
}