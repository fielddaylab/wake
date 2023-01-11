using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua.Debugging
{
    public class CrashHandler : MonoBehaviour {
        public Canvas Canvas;

        static public bool Enabled = false;

        static private bool s_Registered;
        static private CrashHandler s_Instance;

        static public event Action<Exception> OnCrash;

        static public void Register() {
            if (s_Registered) {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            s_Registered = true;
        }

        static public void Deregister() {
            if (!s_Registered) {
                return;
            }

            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            s_Registered = false;
        }

        static private void OnDomainUnload(object sender, EventArgs e) {
            Deregister();
        }

        static private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) {
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
                OnCrash?.Invoke((Exception) e.ExceptionObject);
            }
        }
    }
}