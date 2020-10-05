using BeauData;
using BeauUtil;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProtoAqua
{
    public class InputService : ServiceBehaviour
    {
        public const int DefaultPriority = int.MinValue;

        #region Types

        private struct PriorityRecord
        {
            public readonly int Priority;
            public readonly object Context;

            public PriorityRecord(int inPriority, object inContext)
            {
                Priority = inPriority;
                Context = inContext;
            }
        }

        private struct FlagsRecord
        {
            public readonly InputLayerFlags Flags;
            public readonly object Context;

            public FlagsRecord(InputLayerFlags inFlags, object inContext)
            {
                Flags = inFlags;
                Context = inContext;
            }
        }

        #endregion // Types

        #region Inspector

        [SerializeField, Required] private ExposedPointerInputModule m_InputModule = null;

        #endregion // Inspector

        [NonSerialized] private readonly List<IInputLayer> m_AllInputLayers = new List<IInputLayer>(32);
        [NonSerialized] private int m_CurrentPriority = DefaultPriority;
        [NonSerialized] private InputLayerFlags m_CurrentFlags = InputLayerFlags.Default;

        [NonSerialized] private readonly List<PriorityRecord> m_PriorityStack = new List<PriorityRecord>(8);
        [NonSerialized] private readonly List<FlagsRecord> m_FlagsStack = new List<FlagsRecord>(8);

        #region Input Layers

        public bool RegisterInput(IInputLayer inInputLayer)
        {
            if (!m_AllInputLayers.Contains(inInputLayer))
            {
                m_AllInputLayers.Add(inInputLayer);
                inInputLayer.UpdateSystemPriority(m_CurrentPriority);
                inInputLayer.UpdateSystemFlags(m_CurrentFlags);
                return true;
            }

            return false;
        }

        public bool DeregisterInput(IInputLayer inInputLayer)
        {
            return m_AllInputLayers.FastRemove(inInputLayer);
        }

        #endregion // Input Layers

        #region Priority

        public void PushPriority(IInputLayer inInputLayer)
        {
            PushPriority(inInputLayer.Priority, inInputLayer);
        }

        public void PushPriority(int inPriority, object inContext = null)
        {
            Debug.LogFormat("[InputService] Pushed priority {0} with context '{1}'", inPriority, inContext);

            m_PriorityStack.Add(new PriorityRecord(inPriority, inContext));
            m_CurrentPriority = inPriority;

            BroadcastPriorityUpdate();
        }

        public void PopPriority(object inContext = null)
        {
            if (m_PriorityStack.Count > 0)
            {
                for(int i = m_PriorityStack.Count - 1; i >= 0; --i)
                {
                    if (m_PriorityStack[i].Context == inContext)
                    {
                        m_PriorityStack.RemoveAt(i);
                        if (i == m_PriorityStack.Count)
                        {
                            m_CurrentPriority = i > 0 ? m_PriorityStack[i - 1].Priority : DefaultPriority;
                            Debug.LogFormat("[InputService] Popped priority with context '{0}', new priority is {1} with context '{2}'", inContext, m_CurrentPriority, i == 0 ? "null" : m_PriorityStack[i - 1].Context);
                        }
                        else
                        {
                            Debug.LogFormat("[InputService] Popped priority with context '{0}'", inContext);
                        }

                        BroadcastPriorityUpdate();
                        return;
                    }
                }
            }
            else
            {
                Debug.LogErrorFormat("[InputService] Attempting to pop priority with nothing on stack.");
                m_CurrentPriority = 0;
            }
        }

        private void BroadcastPriorityUpdate()
        {
            for(int i = m_AllInputLayers.Count - 1; i >= 0; --i)
            {
                m_AllInputLayers[i].UpdateSystemPriority(m_CurrentPriority);
            }
        }

        #endregion // Priority

        #region Flags

        public void PushFlags(IInputLayer inLayer)
        {
            PushFlags(inLayer.Flags, inLayer);
        }

        public void PushFlags(InputLayerFlags inFlags, object inContext = null)
        {
            Debug.LogFormat("[InputService] Pushed flags {0} with context '{1}'", inFlags, inContext);

            m_FlagsStack.Add(new FlagsRecord(inFlags, inContext));
            m_CurrentFlags = inFlags;

            BroadcastFlagsUpdate();
        }

        public void PopFlags(object inContext = null)
        {
            if (m_FlagsStack.Count > 0)
            {
                for(int i = m_FlagsStack.Count - 1; i >= 0; --i)
                {
                    if (m_FlagsStack[i].Context == inContext)
                    {
                        m_FlagsStack.RemoveAt(i);
                        if (i == m_FlagsStack.Count)
                        {
                            m_CurrentFlags = i > 0 ? m_FlagsStack[i - 1].Flags : InputLayerFlags.Default;
                            Debug.LogFormat("[InputService] Popped flags with context '{0}', new flags are {1} with context '{2}'", inContext, m_CurrentFlags, i == 0 ? "null" : m_FlagsStack[i - 1].Context);
                        }
                        else
                        {
                            Debug.LogFormat("[InputService] Popped flags with context '{0}'", inContext);
                        }
                        BroadcastFlagsUpdate();
                        return;
                    }
                }
            }
            else
            {
                Debug.LogErrorFormat("[InputService] Attempting to pop flags with nothing on stack.");
                m_CurrentFlags = InputLayerFlags.Default;
            }
        }

        private void BroadcastFlagsUpdate()
        {
            for(int i = m_AllInputLayers.Count - 1; i >= 0; --i)
            {
                m_AllInputLayers[i].UpdateSystemFlags(m_CurrentFlags);
            }
        }

        #endregion // Flags

        #region Pointer

        public bool IsPointerOverUI()
        {
            return m_InputModule.IsPointerOverCanvas();
        }

        #endregion // Pointer

        #region Unity Events

        private void LateUpdate()
        {
            for(int i = m_AllInputLayers.Count - 1; i >= 0; --i)
            {
                m_AllInputLayers[i].Device.Update();
            }
        }

        #endregion // Unity Events

        #region IService

        public override FourCC ServiceId()
        {
            return ServiceIds.Input;
        }

        #endregion // IService

        [ContextMenu("Clear All Stacks")]
        private void ClearStacks()
        {
            m_FlagsStack.Clear();
            m_PriorityStack.Clear();

            m_CurrentPriority = DefaultPriority;
            m_CurrentFlags = InputLayerFlags.Default;

            BroadcastFlagsUpdate();
            BroadcastPriorityUpdate();
        }
    }
}