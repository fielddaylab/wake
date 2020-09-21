using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using BeauPools;

namespace ProtoAqua.Experiment
{
    public class ActorMemory : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [SerializeField, Inline(InlineAttribute.DisplayType.HeaderLabel)]
        private MemoryRegion m_ShortTermMemory = null;

        [SerializeField, Inline(InlineAttribute.DisplayType.HeaderLabel)]
        private MemoryRegion m_LongTermMemory = null;

        #endregion // Inspector

        public MemoryRegion ShortTerm { get { return m_ShortTermMemory; } }
        public MemoryRegion LongTerm { get { return m_LongTermMemory; } }

        void IPoolAllocHandler.OnAlloc()
        {
            m_ShortTermMemory.Reset();
            m_LongTermMemory.Reset();
        }

        void IPoolAllocHandler.OnFree()
        {
            m_ShortTermMemory.Reset();
            m_LongTermMemory.Reset();
        }
    }
}