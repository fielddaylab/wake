using System;
using System.Collections;
using BeauUtil;
using Leaf.Runtime;

namespace Aqua.Scripting
{
    /// <summary>
    /// Handle for a scripting thread.
    /// </summary>
    public struct ScriptThreadHandle : IEquatable<ScriptThreadHandle>
    {
        private LeafThreadHandle m_Thread;
        
        internal ScriptThreadHandle(LeafThreadHandle inHandle)
        {
            m_Thread = inHandle;
        }

        internal ScriptThread GetThread()
        {
            return m_Thread.GetThread<ScriptThread>();
        }

        /// <summary>
        /// Name for this thread.
        /// </summary>
        public string Name() { return GetThread()?.Name; }

        /// <summary>
        /// Context for the script thread.
        /// </summary>
        public IScriptContext Context() { return GetThread()?.Context; }

        /// <summary>
        /// Target of the script thread.
        /// </summary>
        public StringHash32 TargetId() { return GetThread()?.Target() ?? StringHash32.Null; }

        /// <summary>
        /// Trigger of this thread.
        /// </summary>
        public StringHash32 TriggerId() { return GetThread()?.TriggerId() ?? StringHash32.Null; }

        /// <summary>
        /// Initialize node of this thread.
        /// </summary>
        public string RootNodeName() { return GetThread()?.InitialNodeName() ?? string.Empty; }

        /// <summary>
        /// Pauses the thread.
        /// </summary>
        public void Pause()
        {
            GetThread()?.Pause();
        }

        /// <summary>
        /// Returns if the thread is paused.
        /// </summary>
        public bool IsPaused()
        {
            var thread = GetThread();
            if (thread != null)
                return thread.IsPaused();
            return false;
        }

        /// <summary>
        /// Resumes the thread.
        /// </summary>
        public void Resume()
        {
            GetThread()?.Resume();
        }

        /// <summary>
        /// Returns if this handle is running.
        /// </summary>
        public bool IsRunning()
        {
            var thread = GetThread();
            if (thread != null)
                return thread.IsRunning();
            return false;
        }

        public IEnumerator Wait()
        {
            return GetThread()?.Wait();
        }

        public IEnumerator Skip()
        {
            var thread = GetThread();
            if (thread != null)
            {
                thread.Skip();
                return thread.Wait();
            }
            return null;
        }

        /// <summary>
        /// Kills the thread.
        /// </summary>
        public void Kill()
        {
            GetThread()?.Kill();
        }

        public bool Equals(ScriptThreadHandle other)
        {
            return m_Thread == other.m_Thread;
        }
    }
}