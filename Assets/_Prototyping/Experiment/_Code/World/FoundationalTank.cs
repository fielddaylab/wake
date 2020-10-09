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

        protected override void OnEnable()
        {
            base.OnEnable();

            Services.Events.Register<StringHash32>(ExperimentEvents.SetupAddActor, SetupAddActor, this)
                .Register<StringHash32>(ExperimentEvents.SetupRemoveActor, SetupRemoveActor, this);
        }

        protected override void OnDisable()
        {
            base.OnEnable();

            Services.Events?.Deregister<StringHash32>(ExperimentEvents.SetupAddActor, SetupAddActor)
                .Deregister<StringHash32>(ExperimentEvents.SetupRemoveActor, SetupRemoveActor);
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
                actor.Nav.SetHelper(m_ActorNavHelper);
                actor.Nav.Spawn(spawnCount * RNG.Instance.NextFloat(0.8f, 1.2f) * m_SpawnDelay);
            }
        }

        private void SetupRemoveActor(StringHash32 inActorId)
        {
            ExperimentServices.Actors.Pools.Reset(inActorId);
            Services.UI.WorldFaders.Flash(Color.black, 0.2f);
        }
    }
}