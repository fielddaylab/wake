using System;
using System.Diagnostics;
using UnityEngine;

namespace EasyBugReporter {

    /// <summary>
    /// Collects logs with a rolling buffer.
    /// </summary>
    public class LogContext : IDumpSystem {
        #region Consts

        public const LogTypeMask DefaultMask = 
        #if UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
            LogTypeMask.Development;
        #else
            LogTypeMask.Production;
        #endif

        static private readonly DumpStyle FailureStyle = new DumpStyle(FontStyle.Bold, Color.white, Color.magenta);
        static private readonly DumpStyle ErrorStyle = new DumpStyle(FontStyle.Bold, Color.white, Color.red);
        static private readonly DumpStyle WarningStyle = new DumpStyle(FontStyle.Normal, Color.black, Color.yellow);
        static private readonly DumpStyle NormalStyle = new DumpStyle(FontStyle.Normal, default, default);

        #endregion // Consts

        #region Types

        private struct LogData {
            public string Msg;
            public string StackTrace;
            public long Timestamp;
            public LogType Type;
        }

        #endregion // Types

        private WindowBuffer<LogData> m_CompleteBuffer;
        private WindowBuffer<LogData> m_WarningErrorBuffer;
        private WindowBuffer<LogData> m_FailureBuffer;
        private long m_FrozenTS;
        private readonly LogTypeMask m_Mask;

        public LogContext(LogTypeMask mask = DefaultMask, int completeLogWindow = 64, int warningErrorWindow = 64, int failureWindow = 32) {
            m_Mask = mask;
            if (mask == 0) {
                return;
            }

            m_CompleteBuffer = new WindowBuffer<LogData>(completeLogWindow);
            if ((m_Mask & LogTypeMask.WarningsAndErrors) != 0) {
                m_WarningErrorBuffer = new WindowBuffer<LogData>(warningErrorWindow);
            }

            if ((m_Mask & LogTypeMask.Failures) != 0) {
                m_FailureBuffer = new WindowBuffer<LogData>(failureWindow);
            }
        }

        public bool Dump(IDumpWriter writer) {
            if (m_Mask == 0 || m_CompleteBuffer.Count == 0) {
                return true;
            }

            writer.BeginSection("Logs");

            if ((m_Mask & LogTypeMask.Failures) != 0 && m_FailureBuffer.Count > 0) {
                writer.Header(string.Format("FAILURES ({0}/{1})", m_FailureBuffer.Count, m_FailureBuffer.Total));
                writer.BeginTable();
                WriteTableHeader(writer);
                for(int i = 0; i < m_FailureBuffer.Count; i++) {
                    Write(writer, m_FailureBuffer[i], m_FrozenTS);
                }
                writer.EndTable();
            }

            if ((m_Mask & LogTypeMask.WarningsAndErrors) != 0 && m_WarningErrorBuffer.Count > 0) {
                writer.Header(string.Format("WARNINGS/ERRORS ({0}/{1})", m_WarningErrorBuffer.Count, m_WarningErrorBuffer.Total));
                writer.BeginTable();
                WriteTableHeader(writer);
                for(int i = 0; i < m_WarningErrorBuffer.Count; i++) {
                    Write(writer, m_WarningErrorBuffer[i], m_FrozenTS);
                }
                writer.EndTable();
            }

            writer.Header(string.Format("LOGS ({0}/{1})", m_CompleteBuffer.Count, m_CompleteBuffer.Total));
            writer.BeginTable();
            WriteTableHeader(writer);
            for(int i = 0; i < m_CompleteBuffer.Count; i++) {
                Write(writer, m_CompleteBuffer[i], m_FrozenTS);
            }
            writer.EndTable();

            writer.EndSection();

            return true;
        }

        static private void WriteTableHeader(IDumpWriter writer) {
            if (!writer.SupportsTables) {
                return;
            }

            writer.BeginTableRow();
            writer.TableCellHeader("Type");
            writer.TableCellHeader("Time");
            writer.TableCellHeader("Message");
            writer.TableCellHeader("Stack Trace");
            writer.EndTableRow();
        }

        static private void Write(IDumpWriter writer, LogData data, long reportTimestamp) {
            writer.BeginTableRow();

            switch(data.Type) {
                case LogType.Error: {
                    writer.TableCellText("ERROR", ErrorStyle);
                    break;
                }
                case LogType.Warning: {
                    writer.TableCellText("WARNING", WarningStyle);
                    break;
                }
                case LogType.Assert: {
                    writer.TableCellText("ASSERT", FailureStyle);
                    break;
                }
                case LogType.Exception: {
                    writer.TableCellText("EXCEPTION", FailureStyle);
                    break;
                }
                case LogType.Log: {
                    writer.TableCellText("INFO", NormalStyle);
                    break;
                }
            }

            TimeSpan timeSince = TimeSpan.FromTicks(reportTimestamp - data.Timestamp);

            writer.TableCellText(string.Format("{0} seconds ago ({1}ms)", timeSince.TotalSeconds.ToString("F3"), Math.Ceiling(timeSince.TotalMilliseconds)));

            writer.TableCellText(data.Msg);
            writer.TableCellText(data.StackTrace);

            writer.EndTableRow();
        }

        public void Initialize() {
            if (m_Mask != 0) {
                Application.logMessageReceived += Application_logMessageReceived;
            }
        }

        public void Shutdown() {
            if (m_Mask != 0) {
                Application.logMessageReceived -= Application_logMessageReceived;
            }

            m_FailureBuffer.Reset();
            m_CompleteBuffer.Reset();
            m_WarningErrorBuffer.Reset();
        }

        public void Freeze() {
            m_FrozenTS = Stopwatch.GetTimestamp();
        }

        public void Unfreeze() {
            m_FrozenTS = 0;
        }

        private void Application_logMessageReceived(string logString, string stackTrace, LogType type) {
            // if frozen, or this type of log is masked out, then ignore
            if (m_FrozenTS > 0 || (m_Mask & (LogTypeMask) (1 << (int) type)) == 0) {
                return;
            }

            LogData log = new LogData() {
                Msg = logString,
                StackTrace = stackTrace,
                Type = type,
                Timestamp = Stopwatch.GetTimestamp()
            };

            m_CompleteBuffer.Write(log);
            switch(type) {
                case LogType.Warning:
                case LogType.Error: {
                    m_WarningErrorBuffer.Write(log);
                    break;
                }

                case LogType.Assert:
                case LogType.Exception: {
                    m_FailureBuffer.Write(log);
                    break;
                }
            }
        }
    }

    public enum LogTypeMask {
        Error = 1 << LogType.Error,
        Warning = 1 << LogType.Warning,
        Assert = 1 << LogType.Assert,
        Exception = 1 << LogType.Exception,
        Log = 1 << LogType.Log,

        Development = Error | Warning | Assert | Exception,
        Production = Error | Exception | Assert,

        WarningsAndErrors = Warning | Error,
        Failures = Exception | Assert,

        All = Error | Warning | Assert | Exception | Log
    }
}