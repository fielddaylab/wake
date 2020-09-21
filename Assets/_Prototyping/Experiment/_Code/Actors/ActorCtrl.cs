using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;

namespace ProtoAqua.Experiment
{
    public class ActorCtrl : MonoBehaviour, IPooledObject<ActorCtrl>
    {
        #region Inspector

        [SerializeField] private ActorStats m_Stats = null;
        [SerializeField] private ActorStimuli m_Stimuli = null;
        [SerializeField] private ActorMemory m_Memory = null;

        #endregion // Inspector

        [NonSerialized] private VariantTable m_TemporaryStorage;
        [NonSerialized] private IPool<ActorCtrl> m_Pool;

        public ActorStats Stats { get { return m_Stats; } }
        public ActorStimuli Stimuli { get { return m_Stimuli; } }
        public ActorMemory Memory { get { return m_Memory; } }

        public VariantTable TempStorage { get { return m_TemporaryStorage ?? (m_TemporaryStorage = new VariantTable(name)); } }

        public void Tick(uint inCurrentTime)
        {
        }

        public void Recycle()
        {
            m_Pool.Free(this);
        }

        #region IPooledObject

        void IPooledObject<ActorCtrl>.OnAlloc()
        {
            ExperimentServices.Actors.Register(this);
        }

        void IPooledObject<ActorCtrl>.OnConstruct(IPool<ActorCtrl> inPool)
        {
            m_Pool = inPool;
        }

        void IPooledObject<ActorCtrl>.OnDestruct()
        {
            m_Pool = null;
        }

        void IPooledObject<ActorCtrl>.OnFree()
        {
            if (m_TemporaryStorage != null)
            {
                m_TemporaryStorage.Clear();
            }

            if (ExperimentServices.Actors)
            {
                ExperimentServices.Actors.Deregister(this);
            }
        }

        #endregion // IPooledObject
    }
}