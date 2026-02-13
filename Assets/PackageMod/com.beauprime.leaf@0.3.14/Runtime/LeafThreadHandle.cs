/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    6 June 2021
 * 
 * File:    LeafThreadHandle.cs
 * Purpose: Handle to a leaf thread.
 */

using System;
using System.Collections;

namespace Leaf.Runtime
{
    /// <summary>
    /// Handle to a thread state.
    /// </summary>
    public struct LeafThreadHandle : IEquatable<LeafThreadHandle>
    {
        private LeafThreadState m_State;
        private uint m_Id;

        internal LeafThreadHandle(LeafThreadState inState, uint inHandle)
        {
            m_State = inState;
            m_Id = inHandle;
        }

        /// <summary>
        /// Returns the name of the thread.
        /// </summary>
        public string Name() { return GetThread()?.Name; }

        /// <summary>
        /// Returns if the thread is currently running.
        /// </summary>
        public bool IsRunning()
        {
            return GetThread()?.IsRunning() ?? false;
        }

        /// <summary>
        /// Peeks at the currently executing node.
        /// </summary>
        public LeafNode PeekNode()
        {
            LeafThreadState state = GetThread();
            if (state != null && state.InternalStackSize() > 0)
            {
                LeafNode node;
                uint pc;
                state.InternalReadState(0, out node, out pc);
                return node;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the thread state, if it is still valid.
        /// </summary>
        public LeafThreadState GetThread()
        {
            if (m_Id > 0 && m_State != null && (!m_State.m_Running || m_State.m_Id != m_Id))
            {
                m_State = null;
                m_Id = 0;
            }

            return m_State;
        }

        /// <summary>
        /// Retrieves the thread state, if it is still valid.
        /// </summary>
        public TThread GetThread<TThread>() where TThread : LeafThreadState
        {
            if (m_Id > 0 && m_State != null && (!m_State.m_Running || m_State.m_Id != m_Id))
            {
                m_State = null;
                m_Id = 0;
            }

            return (TThread) m_State;
        }

        /// <summary>
        /// Waits for the thread to complete.
        /// </summary>
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
            m_State = null;
            m_Id = 0;
        }

        #region Overrides and Operators

        public bool Equals(LeafThreadHandle other)
        {
            return GetThread() == other.GetThread();
        }

        public override bool Equals(object obj)
        {
            if (obj is LeafThreadHandle)
                return Equals((LeafThreadHandle) obj);
            return false;
        }

        public override int GetHashCode()
        {
            var thread = GetThread();
            if (thread != null)
            {
                return thread.GetHashCode() << 5 ^ m_Id.GetHashCode();
            }
            return (int) m_Id;
        }

        static public bool operator==(LeafThreadHandle left, LeafThreadHandle right)
        {
            return left.Equals(right);
        }

        static public bool operator!=(LeafThreadHandle left, LeafThreadHandle right)
        {
            return !left.Equals(right);
        }

        #endregion // Overrides and Operators
    }
}