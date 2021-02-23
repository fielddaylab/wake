using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using System.Reflection;
using BeauUtil.Variants;
using BeauRoutine.Extensions;
using ProtoCP;
using Aqua;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ProtoAqua.Experiment
{
    public class StressorTank : ExperimentTank
    {
        #region Inspector

        [SerializeField] private LocText m_Text = null;

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
                .Register<WaterPropertyId>(ExperimentEvents.StressorText, ChangeText, this);

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
            m_Text.SetText("");
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
            if (inSelection.Tank == TankType.Stressor)
            {
                gameObject.SetActive(true);
                return true;
            }

            return false;
        }

        private void SetupAddActor(StringHash32 inActorId)
        {
            ActorCtrl actor = ExperimentServices.Actors.Pools.Alloc(inActorId, m_ActorRoot);
            actor.Nav.Helper = m_ActorNavHelper;
            actor.Nav.Spawn(0);
        }

        public void ChangeText(WaterPropertyId Id) {
            
            m_Text.SetText(Services.Assets.WaterProp.Property(Id)?.LabelId() ?? null);
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

        public override int GetSpawnCount(StringHash32 inActorId)
        {
            return 1;
        }
    }
    
}