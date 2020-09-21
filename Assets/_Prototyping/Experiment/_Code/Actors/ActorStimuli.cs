using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;

namespace ProtoAqua.Experiment
{
    public class ActorStimuli : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [SerializeField] private ActorSense[] m_AllSenses = null;

        #endregion // Inspector

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
        }
    }
}