#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Collections;
using System.Collections.Generic;
using System.Text;
using Aqua.Debugging;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Testing {
    public class AutomationTester : ServiceBehaviour, IDebuggable {
        #if DEVELOPMENT

        private Routine m_CurrentTest;
        private StringBuilder m_StringBuilder = new StringBuilder(1024);
        private bool m_TestPassed;
        private bool m_TimedOut;

        private void RunTest(string testName, IEnumerator test, float timeOut) {
            m_CurrentTest.Replace(this, RunTest_Routine(testName, test, timeOut)).SetPriority(10000);
            DebugService.Hide();
        }

        private IEnumerator RunTest_Routine(string testName, IEnumerator test, float timeOut) {
            yield return null;
            TestSetup();
            try
            {
                yield return Routine.Race(
                    test, TimeOut(timeOut)
                );
                m_TestPassed = !m_TimedOut;
            }
            finally
            {
                TestTeardown(testName);
            }
        }

        private IEnumerator TimeOut(float timeOut) {
            yield return Routine.WaitRealSeconds(timeOut);
            m_TimedOut = true;
        }

        private void TestSetup() {
            Application.logMessageReceived -= OnLogMessage;
            Application.logMessageReceived += OnLogMessage;
            Time.timeScale = 1;
            m_StringBuilder.Length = 0;
            m_TestPassed = false;
            m_TimedOut = false;
            Assert.DeregisterLogHook();
            Assert.SetFailureMode(Assert.FailureMode.Automatic);
            DebugService.Hide();
        }

        private void TestCancel() {
            m_CurrentTest.Stop();
        }

        private void TestTeardown(string testName) {
            Application.logMessageReceived -= OnLogMessage;
            Assert.RegisterLogHook();
            Assert.SetFailureMode(Assert.FailureMode.User);
            if (m_TestPassed) {
                Log.Msg("[AutomationTester] Test '{0}' passed\n\n{1}", testName, m_StringBuilder.Flush());
            } else {
                Log.Error("[AutomationTester] Test '{0}' failed! Oh no!\n\n{1}", testName, m_StringBuilder.Flush());
            }
        }

        private void OnLogMessage(string condition, string stackTrace, UnityEngine.LogType type) {
            Report(m_StringBuilder, condition, stackTrace, type);
            switch(type) {
                case LogType.Exception:
                case LogType.Assert: {
                    TestCancel();
                    break;
                }
            }
        }

        #region IDebuggable

        public IEnumerable<DMInfo> ConstructDebugMenus() {
            // DMInfo menu = new DMInfo("Auto Testing", 8);

            // menu.AddButton("Scene Loading", () => RunTest("Scene Loading", SceneLoadValidation.LoadAllScenes(), 60));

            // yield return menu;

            yield break;
        }

        #endregion // IDebuggable

        #region Logging

        static private void Report(StringBuilder sb, string condition, string stackTrace, UnityEngine.LogType type) {
            if (sb.Length > 0) {
                sb.Append('\n');
            }

            switch(type) {
                case LogType.Assert: {
                    sb.Append("ASSERT: ");
                    break;
                }

                case LogType.Error: {
                    sb.Append("ERROR: ");
                    break;
                }

                case LogType.Exception: {
                    sb.Append("EXCEPTION: ");
                    break;
                }

                case LogType.Warning: {
                    sb.Append("WARN: ");
                    break;
                }
            }
            sb.Append(condition).Append('\n').Append(stackTrace);
        }

        #endregion // Logging

        #endif // DEVELOPMENT
    }
}