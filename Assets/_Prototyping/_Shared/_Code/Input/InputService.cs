using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAqua
{
    public class InputService : ServiceBehaviour
    {
        private readonly List<IInputLayer> m_AllInputLayers = new List<IInputLayer>(32);
        private int m_CurrentPriority;
        private readonly Stack<int> m_PriorityStack = new Stack<int>(8);
        private InputLayerFlags m_CurrentFlags = InputLayerFlags.All;
        private readonly Stack<InputLayerFlags> m_FlagsStack = new Stack<InputLayerFlags>(8);

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

        public void PushPriority(int inPriority)
        {
            m_PriorityStack.Push(m_CurrentPriority);
            m_CurrentPriority = inPriority;

            BroadcastPriorityUpdate();
        }

        public void PushPriority(IInputLayer inInputLayer)
        {
            PushPriority(inInputLayer.Priority);
        }

        public void PopPriority()
        {
            if (m_PriorityStack.Count > 0)
            {
                m_CurrentPriority = m_PriorityStack.Pop();
                BroadcastPriorityUpdate();
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

        public void PushFlags(InputLayerFlags inputLayerFlags)
        {
            m_FlagsStack.Push(m_CurrentFlags);
            m_CurrentFlags = inputLayerFlags;

            BroadcastFlagsUpdate();
        }

        public void PopFlags()
        {
            if (m_FlagsStack.Count > 0)
            {
                m_CurrentFlags = m_FlagsStack.Pop();
                BroadcastFlagsUpdate();
            }
            else
            {
                Debug.LogErrorFormat("[InputService] Attempting to pop flags with nothing on stack.");
                m_CurrentFlags = InputLayerFlags.All;
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

        #region IService

        public override FourCC ServiceId()
        {
            return ServiceIds.Input;
        }

        #endregion // IService
    }
}