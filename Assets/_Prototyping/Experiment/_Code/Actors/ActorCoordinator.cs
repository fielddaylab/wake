using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using BeauPools;
using BeauUtil.Variants;

namespace ProtoAqua.Experiment
{
    public class ActorCoordinator : ServiceBehaviour
    {
        #region Inspector

        [SerializeField] private ActorTicker m_Ticker = null;
        [SerializeField] private ActorPools m_Pools = null;

        #endregion // Ticker

        [NonSerialized] private readonly List<ActorCtrl> m_AllActors = new List<ActorCtrl>();
        [NonSerialized] private DynamicPool<VariantTable> m_VariantTablePool;

        #region Register/Deregister

        public void Register(ActorCtrl inActor)
        {
            m_AllActors.Add(inActor);
        }

        public void Deregister(ActorCtrl inActor)
        {
            m_AllActors.FastRemove(inActor);
        }

        #endregion // Register/Deregister

        #region Ticking

        private void FixedUpdate()
        {
            float dt = Time.deltaTime;
            m_Ticker.Advance(dt);
            foreach(var actor in m_AllActors)
            {
                actor.Tick(m_Ticker.CurrentTimeMS());
            }
        }

        #endregion // Ticking

        #region Variant Tables

        public TempAlloc<VariantTable> BorrowTable()
        {
            return m_VariantTablePool.TempAlloc();
        }

        #endregion // Variant Tables

        public ActorPools Pools { get { return m_Pools; } }

        #region Service

        protected override void OnRegisterService()
        {
            m_VariantTablePool = new DynamicPool<VariantTable>(16, (p) => new VariantTable("<ActorCoordinatorPool>"));
            m_VariantTablePool.Prewarm();
        }

        protected override void OnDeregisterService()
        {
            m_VariantTablePool.Dispose();
            m_VariantTablePool = null;

            m_AllActors.Clear();
        }

        public override FourCC ServiceId()
        {
            return ServiceIds.AI;
        }

        #endregion // Service
    }
}