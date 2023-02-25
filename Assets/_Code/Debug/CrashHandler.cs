using System;
using UnityEngine;
using EasyBugReporter;
using TMPro;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua.Debugging
{
    public class CrashHandler : MonoBehaviour {
        public Canvas Canvas;
        public TMP_Text ProgressText;
        public TMP_Text ExceptionText;
        public Button DumpButton;

        [NonSerialized] private int m_DumpCounter;

        private void Awake() {
            DumpButton.onClick.AddListener(() => {
                BugReporter.DumpContext();
                Canvas.enabled = false;
                m_DumpCounter = 3;
            });
        }

        private void LateUpdate() {
            if (m_DumpCounter > 0 && --m_DumpCounter == 0) {
                Canvas.enabled = true;
            }
        }

        private void Populate(string exception, string context) {
            ProgressText.SetText(context);
            ExceptionText.SetText(exception);
        }

        #region Static API

        static public bool Enabled = false;

        public delegate void OnCrashDelegate(Exception exception, string error);
        public delegate void CrashDisplayDelegate(Exception exception, string error, out string outContext);

        static private bool s_Registered;
        static private CrashHandler s_Instance;

        static public event OnCrashDelegate OnCrash;
        static public event CrashDisplayDelegate DisplayCrash;

        static public void Register() {
            if (s_Registered) {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            Application.logMessageReceived += OnApplicationLog;
            s_Registered = true;

            DumpSourceCollection src = new DumpSourceCollection();
            src.Add(new ScreenshotContext());
            src.Add(new LogContext(EasyBugReporter.LogTypeMask.Development | EasyBugReporter.LogTypeMask.Log));
            src.Add(new UnityContext());
            src.Add(new SystemInfoContext());
            BugReporter.DefaultSources = src;
        }

        static public void Deregister() {
            if (!s_Registered) {
                return;
            }

            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            Application.logMessageReceived -= OnApplicationLog;
            s_Registered = false;
        }

        static private void OnDomainUnload(object sender, EventArgs e) {
            Deregister();
        }

        static private void OnApplicationLog(string condition, string stackTrace, LogType type) {
            if (type != LogType.Exception) {
                return;
            }

            OnExceptionEncountered(condition, null);
        }

        static private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) {
            OnExceptionEncountered(null, e.ExceptionObject as Exception);
        }

        static private void OnExceptionEncountered(string exceptionInfo, Exception exception) {
            if (!Enabled) {
                return;
            }

            #if UNITY_EDITOR
            if (EditorApplication.isCompiling || EditorApplication.isPaused || !EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }
            #endif // UNITY_EDITOR

            if (!s_Instance) {
                s_Instance = Instantiate(Resources.Load<CrashHandler>("CrashHandler"));
                string context = null;
                OnCrash?.Invoke(exception, exceptionInfo);
                DisplayCrash?.Invoke(exception, exceptionInfo, out context);
                s_Instance.Populate(exception?.Message ?? exceptionInfo, context);
            }
        }

        #endregion // Static API
    }
}