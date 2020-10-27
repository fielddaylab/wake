using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using System.Reflection;
using BeauUtil.Variants;
using BeauRoutine.Extensions;
using ProtoCP;

namespace ProtoAqua.Experiment
{
    public class FoundationalTank : ExperimentTank
    {
        #region Inspector

        [SerializeField] private float m_SpawnDelay = 0.05f;

        #endregion // Inspector

        [NonSerialized] private AudioHandle m_AudioLoop;

        [NonSerialized] private Routine m_IdleRoutine;
        [NonSerialized] private float m_IdleDuration = 0;

        protected override void Awake()
        {
            base.Awake();

            m_BaseInput.OnInputDisabled.AddListener(() => {
                m_IdleRoutine.Pause();
            });
            m_BaseInput.OnInputEnabled.AddListener(() => {
                m_IdleRoutine.Resume();
                m_IdleDuration /= 2;
            });
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Services.Events.Register<StringHash32>(ExperimentEvents.SetupAddActor, SetupAddActor, this)
                .Register<StringHash32>(ExperimentEvents.SetupRemoveActor, SetupRemoveActor, this)
                .Register(ExperimentEvents.AttemptObserveBehavior, ResetIdle, this);

            m_AudioLoop = Services.Audio.PostEvent("tank_water_loop");
        }

        protected override void OnDisable()
        {
            base.OnEnable();

            Services.Events?.DeregisterAll(this);

            m_AudioLoop.Stop();
        }

        public override void OnExperimentStart()
        {
            base.OnExperimentStart();

            ResetIdle();
            m_IdleRoutine.Replace(this, IdleTimer());
        }

        public override void OnExperimentEnd()
        {
            m_IdleRoutine.Stop();

            base.OnExperimentEnd();
        }

        private IEnumerator IdleTimer()
        {
            while(true)
            {
                m_IdleDuration += Routine.DeltaTime;
                if (m_IdleDuration >= 30)
                {
                    m_IdleDuration = 0;
                    Services.Script.TriggerResponse(ExperimentTriggers.ExperimentIdle);
                }

                yield return null;
            }
        }

        public override bool TryHandle(ExperimentSetupData inSelection)
        {
            if (inSelection.Tank == TankType.Foundational)
            {
                gameObject.SetActive(true);
                return true;
            }

            return false;
        }

        private void SetupAddActor(StringHash32 inActorId)
        {
            int spawnCount = GetSpawnCount(inActorId);
            while(spawnCount-- > 0)
            {
                ActorCtrl actor = ExperimentServices.Actors.Pools.Alloc(inActorId, m_ActorRoot);
                actor.Nav.Helper = m_ActorNavHelper;
                actor.Nav.Spawn(spawnCount * RNG.Instance.NextFloat(0.8f, 1.2f) * m_SpawnDelay);
            }
        }

        private void SetupRemoveActor(StringHash32 inActorId)
        {
            ExperimentServices.Actors.Pools.Reset(inActorId);
            Services.UI.WorldFaders.Flash(Color.black, 0.2f);
        }

        private void ResetIdle()
        {
            m_IdleDuration = 0;
        }
    }
}