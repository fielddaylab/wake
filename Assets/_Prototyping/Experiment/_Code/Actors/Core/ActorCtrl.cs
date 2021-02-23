using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ActorCtrl : MonoBehaviour, IPooledObject<ActorCtrl>
    {
        public struct CallbackSet
        {
            public Action OnCreate;
            public Action OnStartSpawn;
            public Action OnFinishSpawn;
            public Action OnThink;
            public Action OnStopThink;
        }

        #region Inspector

        [SerializeField] private ActorConfig m_Config = null;
        [SerializeField] private ActorBody m_Body = null;
        [SerializeField] private ActorStats m_Stats = null;
        [SerializeField] private ActorNav m_Nav = null;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Id;
        [NonSerialized] private TempAlloc<VariantTable> m_TemporaryStorage;
        [NonSerialized] private IPool<ActorCtrl> m_Pool;
        [NonSerialized] private CallbackSet m_Callbacks;
        [NonSerialized] private ExperimentSettings m_Settings;

        [NonSerialized] private ActorModule[] m_AllModules;

        [NonSerialized] private uint m_LastTimeTick;
        [NonSerialized] private uint m_NextThinkTick;

        public StringHash32 Id { get { return m_Id; } }
        public BestiaryDesc Besitary { get { return m_Config.BestiaryEntry(); } }
        public ActorConfig Config { get { return m_Config; } }
        public ActorBody Body { get { return m_Body; } }
        public ActorStats Stats { get { return m_Stats; } }
        public ActorNav Nav { get { return m_Nav; } }
        public ref CallbackSet Callbacks { get { return ref m_Callbacks; } }

        public VariantTable TempStorage
        {
            get
            {
                if (m_TemporaryStorage == null)
                {
                    m_TemporaryStorage = ExperimentServices.Actors.BorrowTable();
                }

                return m_TemporaryStorage.Object;
            }
        }

        public void Tick(uint inCurrentTime, uint inCurrentThink)
        {
            uint elapsedTicks = inCurrentThink - m_LastTimeTick;
            m_Stats.Tick(elapsedTicks);
            m_LastTimeTick = inCurrentThink;

            if (inCurrentThink >= m_NextThinkTick)
            {
                m_NextThinkTick = inCurrentThink + m_Settings.ThinkSpacing();
                m_Callbacks.OnThink?.Invoke();
            }
        }

        public void Recycle()
        {
            m_Pool.Free(this);
        }

        #region IPooledObject

        void IPooledObject<ActorCtrl>.OnAlloc()
        {
            ExperimentServices.Actors.Register(this);
            m_Id = ExperimentServices.Actors.NextId(m_Config.name);
            m_Callbacks.OnCreate?.Invoke();
        }

        void IPooledObject<ActorCtrl>.OnConstruct(IPool<ActorCtrl> inPool)
        {
            m_Pool = inPool;
            m_Settings = Services.Tweaks.Get<ExperimentSettings>();

            m_AllModules = GetComponentsInChildren<ActorModule>(true);
        }

        void IPooledObject<ActorCtrl>.OnDestruct()
        {
            m_Pool = null;
        }

        void IPooledObject<ActorCtrl>.OnFree()
        {
            Ref.Dispose(ref m_TemporaryStorage);
            
            m_LastTimeTick = 0;
            m_NextThinkTick = 0;
            m_Id = StringHash32.Null;

            if (ExperimentServices.Actors)
            {
                ExperimentServices.Actors.Deregister(this);
            }
        }

        #endregion // IPooledObject
    }
}