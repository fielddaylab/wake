using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using Aqua;
using UnityEngine;

namespace Aqua.Scripting
{
    /// <summary>
    /// Handle for a scripting thread.
    /// </summary>
    public struct ScriptThreadHandle
    {
        private ScriptThread m_Thread;
        private uint m_Id;
        
        internal ScriptThreadHandle(ScriptThread inThread, uint inId)
        {
            m_Thread = inThread;
            m_Id = inId;
        }

        private ScriptThread GetThread()
        {
            if (m_Thread != null && !m_Thread.HasId(m_Id))
            {
                m_Thread = null;
                m_Id = 0;
            }
            return m_Thread;
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

        /// <summary>
        /// Kills the thread.
        /// </summary>
        public void Kill()
        {
            GetThread()?.Kill();
        }
    }
}