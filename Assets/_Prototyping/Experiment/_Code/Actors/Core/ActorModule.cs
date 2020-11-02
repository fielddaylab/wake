using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Debugger;

namespace ProtoAqua.Experiment
{
    public abstract class ActorModule : MonoBehaviour, IPoolAllocHandler, IPoolConstructHandler
    {
        [NonSerialized] private ActorCtrl m_Actor;
        [NonSerialized] private ActorCoordinator m_Coordinator;

        public ActorCtrl Actor { get { return m_Actor; } }
        public ActorCoordinator Coordinator { get { return m_Coordinator; } }

        #region Shortcuts

        protected TValue GetProperty<TValue>(PropertyName inKey)
        {
            return Actor.Config.GetProperty<TValue>(inKey);
        }

        protected TValue GetProperty<TValue>(PropertyName inKey, TValue inDefault)
        {
            return Actor.Config.GetProperty<TValue>(inKey, inDefault);
        }

        protected bool HasProperty(PropertyName inKey, bool inbIncludePrototype = true)
        {
            return Actor.Config.HasProperty(inKey, inbIncludePrototype);
        }

        #endregion // Shortcuts

        #region IPool

        public virtual void OnAlloc()
        {
            m_Coordinator = ExperimentServices.Actors;
        }

        public virtual void OnConstruct()
        {
            m_Actor = GetComponent<ActorCtrl>();
            if (!m_Actor)
            {
                Transform check = transform.parent;
                do
                {
                    m_Actor = check.GetComponent<ActorCtrl>();
                    check = check.parent;
                }
                while(!m_Actor && check);
            }
            Assert.NotNull(m_Actor, "ActorModule requires an ActorCtrl");
        }

        public virtual void OnDestruct()
        {
        }

        public virtual void OnFree()
        {
            Routine.StopAll(this);
            m_Coordinator = null;
        }

        #endregion // IPool
    }
}