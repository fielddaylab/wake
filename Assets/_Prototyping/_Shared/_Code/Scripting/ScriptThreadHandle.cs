using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAqua
{
    /// <summary>
    /// Handle for a scripting thread.
    /// </summary>
    public struct ScriptThreadHandle
    {
        private string m_Id;
        private IScriptContext m_Context;
        private Routine m_Routine;

        internal ScriptThreadHandle(string inId, IScriptContext inContext, Routine inRoutine)
        {
            m_Id = inId;
            m_Context = inContext;
            m_Routine = inRoutine;
        }

        /// <summary>
        /// Id for this thread.
        /// </summary>
        public string Id() { return m_Id; }

        /// <summary>
        /// Context for the script thread.
        /// </summary>
        public IScriptContext Context() { return m_Context; }

        /// <summary>
        /// Currently executing routine.
        /// </summary>
        public Routine Routine() { return m_Routine; }

        /// <summary>
        /// Pauses the thread.
        /// </summary>
        public void Pause()
        {
            m_Routine.Pause();
        }

        /// <summary>
        /// Returns if the thread is paused.
        /// </summary>
        public bool IsPaused()
        {
            return m_Routine.GetPaused();
        }

        /// <summary>
        /// Resumes the thread.
        /// </summary>
        public void Resume()
        {
            m_Routine.Resume();
        }

        /// <summary>
        /// Returns if this handle is running.
        /// </summary>
        public bool IsRunning()
        {
            return m_Routine.Exists();
        }

        /// <summary>
        /// Kills the thread.
        /// </summary>
        public void Kill()
        {
            Services.Script.KillThread(this);
            m_Id = null;
            m_Context = null;
            m_Routine = default(Routine);
        }
    }
}