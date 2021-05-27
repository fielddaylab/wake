using System;
using UnityEngine;
using BeauUtil;
using BeauRoutine;
using Aqua;

namespace ProtoAqua.Experiment
{
    public abstract class ExperimentTank : MonoBehaviour
    {
        #region Inspector

        [Header("Actors")]

        [SerializeField] protected Transform m_ActorRoot = null;
        [SerializeField] protected ActorNavHelper m_ActorNavHelper = null;
        [SerializeField] private SpawnCount[] m_ActorSpawns = null;

        #endregion // Inspector

        [NonSerialized] protected BaseInputLayer m_BaseInput;

        protected virtual void Awake()
        {
            m_BaseInput = BaseInputLayer.Find(this);
        }

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }

        public virtual void OnExperimentStart() { }
        public virtual void OnExperimentEnd() { }

        public virtual void GenerateResult(ExperimentResultData ioData) { }

        public abstract bool TryHandle(ExperimentSetupData inSelection);
        public virtual void Hide()
        {
            gameObject.SetActive(false);
            Routine.StopAll(this);
        }

        public virtual int GetSpawnCount(StringHash32 inActorId)
        {
            int val;
            m_ActorSpawns.TryGetValue(inActorId, out val);
            return val;
        }
    }
}