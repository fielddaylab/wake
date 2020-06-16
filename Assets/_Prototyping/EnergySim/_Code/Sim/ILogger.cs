using System;
using System.Text;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public interface ILogger
    {
        void Log(string inString);
        void Log(string inFormat, object inArg0);
        void Log(string inFormat, object inArg0, object inArg1);
        void Log(string inFormat, object inArg0, object inArg1, object inArg2);
        void Log(string inFormat, params object[] inArgs);

        void Warn(string inString);
        void Warn(string inFormat, object inArg0);
        void Warn(string inFormat, object inArg0, object inArg1);
        void Warn(string inFormat, object inArg0, object inArg1, object inArg2);
        void Warn(string inFormat, params object[] inArgs);

        void Error(string inString);
        void Error(string inFormat, object inArg0);
        void Error(string inFormat, object inArg0, object inArg1);
        void Error(string inFormat, object inArg0, object inArg1, object inArg2);
        void Error(string inFormat, params object[] inArgs);

        void Reset();
        void Flush();
    }

    public class UnityDebugLogger : ILogger
    {
        static readonly UnityDebugLogger Default = new UnityDebugLogger();

        public UnityDebugLogger(string inPrefix = null)
        {
            m_Prefix = inPrefix;
        }

        #region Formatting

        private readonly StringBuilder m_Builder = new StringBuilder(256);
        private readonly string m_Prefix;

        private string FormatString(string inMsg)
        {
            if (string.IsNullOrEmpty(m_Prefix))
                return inMsg;
            m_Builder.Length = 0;
            m_Builder.Append(m_Prefix);
            m_Builder.Append(inMsg);
            return m_Builder.ToString();
        }

        private string FormatString(string inMsg, object inArg0)
        {
            m_Builder.Length = 0;
            if (!string.IsNullOrEmpty(m_Prefix))
                m_Builder.Append(m_Prefix);
            m_Builder.AppendFormat(inMsg, inArg0);
            return m_Builder.ToString();
        }

        private string FormatString(string inMsg, object inArg0, object inArg1)
        {
            m_Builder.Length = 0;
            if (!string.IsNullOrEmpty(m_Prefix))
                m_Builder.Append(m_Prefix);
            m_Builder.AppendFormat(inMsg, inArg0, inArg1);
            return m_Builder.ToString();
        }

        private string FormatString(string inMsg, object inArg0, object inArg1, object inArg2)
        {
            m_Builder.Length = 0;
            if (!string.IsNullOrEmpty(m_Prefix))
                m_Builder.Append(m_Prefix);
            m_Builder.AppendFormat(inMsg, inArg0, inArg1, inArg2);
            return m_Builder.ToString();
        }

        private string FormatString(string inMsg, params object[] inArgs)
        {
            m_Builder.Length = 0;
            if (!string.IsNullOrEmpty(m_Prefix))
                m_Builder.Append(m_Prefix);
            m_Builder.AppendFormat(inMsg, inArgs);
            return m_Builder.ToString();
        }

        #endregion // Formatting

        #region Error

        public void Error(string inString)
        {
            Debug.LogError(FormatString(inString));
        }

        public void Error(string inFormat, object inArg0)
        {
            Debug.LogError(FormatString(inFormat, inArg0));
        }

        public void Error(string inFormat, object inArg0, object inArg1)
        {
            Debug.LogError(FormatString(inFormat, inArg0, inArg1));
        }

        public void Error(string inFormat, object inArg0, object inArg1, object inArg2)
        {
            Debug.LogError(FormatString(inFormat, inArg0, inArg1, inArg2));
        }

        public void Error(string inFormat, params object[] inArgs)
        {
            Debug.LogError(FormatString(inFormat, inArgs));
        }

        #endregion // Error

        #region Log

        public void Log(string inString)
        {
            Debug.Log(FormatString(inString));
        }

        public void Log(string inFormat, object inArg0)
        {
            Debug.Log(FormatString(inFormat, inArg0));
        }

        public void Log(string inFormat, object inArg0, object inArg1)
        {
            Debug.Log(FormatString(inFormat, inArg0, inArg1));
        }

        public void Log(string inFormat, object inArg0, object inArg1, object inArg2)
        {
            Debug.Log(FormatString(inFormat, inArg0, inArg1, inArg2));
        }

        public void Log(string inFormat, params object[] inArgs)
        {
            Debug.Log(FormatString(inFormat, inArgs));
        }

        #endregion // Log

        #region Warn

        public void Warn(string inString)
        {
            Debug.LogWarning(FormatString(inString));
        }

        public void Warn(string inFormat, object inArg0)
        {
            Debug.LogWarning(FormatString(inFormat, inArg0));
        }

        public void Warn(string inFormat, object inArg0, object inArg1)
        {
            Debug.LogWarning(FormatString(inFormat, inArg0, inArg1));
        }

        public void Warn(string inFormat, object inArg0, object inArg1, object inArg2)
        {
            Debug.LogWarning(FormatString(inFormat, inArg0, inArg1, inArg2));
        }

        public void Warn(string inFormat, params object[] inArgs)
        {
            Debug.LogWarning(FormatString(inFormat, inArgs));
        }

        #endregion // Warn

        #region State

        public void Reset()
        {
        }

        public void Flush()
        {
        }

        #endregion // State
    }

    public class BatchedUnityDebugLogger : ILogger
    {
        static readonly BatchedUnityDebugLogger Default = new BatchedUnityDebugLogger();

        static private readonly string MsgPrefix = "";
        static private readonly string WarnPrefix = "WARN: ";
        static private readonly string ErrorPrefix = "ERR: ";

        #region Formatting

        private readonly StringBuilder m_Builder = new StringBuilder(256);

        private void FormatString(string inPrefix, string inMsg)
        {
            if (m_Builder.Length > 0)
                m_Builder.Append('\n');
            
            if (!string.IsNullOrEmpty(inPrefix))
                m_Builder.Append(inPrefix);
            m_Builder.Append(inMsg);
        }

        private void FormatString(string inPrefix, string inMsg, object inArg0)
        {
            if (m_Builder.Length > 0)
                m_Builder.Append('\n');
            
            if (!string.IsNullOrEmpty(inPrefix))
                m_Builder.Append(inPrefix);
            m_Builder.AppendFormat(inMsg, inArg0);
        }

        private void FormatString(string inPrefix, string inMsg, object inArg0, object inArg1)
        {
            if (m_Builder.Length > 0)
                m_Builder.Append('\n');
            
            if (!string.IsNullOrEmpty(inPrefix))
                m_Builder.Append(inPrefix);
            m_Builder.AppendFormat(inMsg, inArg0, inArg1);
        }

        private void FormatString(string inPrefix, string inMsg, object inArg0, object inArg1, object inArg2)
        {
            if (m_Builder.Length > 0)
                m_Builder.Append('\n');
            
            if (!string.IsNullOrEmpty(inPrefix))
                m_Builder.Append(inPrefix);
            m_Builder.AppendFormat(inMsg, inArg0, inArg1, inArg2);
        }

        private void FormatString(string inPrefix, string inMsg, params object[] inArgs)
        {
            if (m_Builder.Length > 0)
                m_Builder.Append('\n');
            
            if (!string.IsNullOrEmpty(inPrefix))
                m_Builder.Append(inPrefix);
            m_Builder.AppendFormat(inMsg, inArgs);
        }

        #endregion // Formatting

        #region Error

        public void Error(string inString)
        {
            FormatString(ErrorPrefix, inString);
        }

        public void Error(string inFormat, object inArg0)
        {
            FormatString(ErrorPrefix, inFormat, inArg0);
        }

        public void Error(string inFormat, object inArg0, object inArg1)
        {
            FormatString(ErrorPrefix, inFormat, inArg0, inArg1);
        }

        public void Error(string inFormat, object inArg0, object inArg1, object inArg2)
        {
            FormatString(ErrorPrefix, inFormat, inArg0, inArg1, inArg2);
        }

        public void Error(string inFormat, params object[] inArgs)
        {
            FormatString(ErrorPrefix, inFormat, inArgs);
        }

        #endregion // Error

        #region Log

        public void Log(string inString)
        {
            FormatString(MsgPrefix, inString);
        }

        public void Log(string inFormat, object inArg0)
        {
            FormatString(MsgPrefix, inFormat, inArg0);
        }

        public void Log(string inFormat, object inArg0, object inArg1)
        {
            FormatString(MsgPrefix, inFormat, inArg0, inArg1);
        }

        public void Log(string inFormat, object inArg0, object inArg1, object inArg2)
        {
            FormatString(MsgPrefix, inFormat, inArg0, inArg1, inArg2);
        }

        public void Log(string inFormat, params object[] inArgs)
        {
            FormatString(MsgPrefix, inFormat, inArgs);
        }

        #endregion // Log

        #region Warn

        public void Warn(string inString)
        {
            FormatString(WarnPrefix, inString);
        }

        public void Warn(string inFormat, object inArg0)
        {
            FormatString(WarnPrefix, inFormat, inArg0);
        }

        public void Warn(string inFormat, object inArg0, object inArg1)
        {
            FormatString(WarnPrefix, inFormat, inArg0, inArg1);
        }

        public void Warn(string inFormat, object inArg0, object inArg1, object inArg2)
        {
            FormatString(WarnPrefix, inFormat, inArg0, inArg1, inArg2);
        }

        public void Warn(string inFormat, params object[] inArgs)
        {
            FormatString(WarnPrefix, inFormat, inArgs);
        }

        #endregion // Warn

        #region State

        public void Reset()
        {
            m_Builder.Length = 0;
        }

        public void Flush()
        {
            Debug.Log(m_Builder.Flush());
        }

        #endregion // State
    }
}