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
    public class MeasurementTank : ExperimentTank
    {
        #region Inspector

        [SerializeField] private float m_SpawnDelay = 0.05f;

        [SerializeField] private SpriteRenderer m_Sprite = null;

        [SerializeField] private ColorGroup m_WaterColor = null;

        #endregion // Inspector

        [NonSerialized] private AudioHandle m_AudioLoop;

        [NonSerialized] private Routine m_IdleRoutine;
        [NonSerialized] private float m_IdleDuration = 0;
        [NonSerialized] private float min_Alpha = 0;
        [NonSerialized] private float max_Alpha = 0;
        [NonSerialized] private Color m_CurrentColor;
        [NonSerialized] private float defaultAlpha;


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

            min_Alpha = m_WaterColor.GetAlpha();
            max_Alpha = min_Alpha + 0.25f;
            m_CurrentColor = m_WaterColor.GetColor();
            defaultAlpha = m_WaterColor.GetAlpha();
        }


        protected override void OnEnable()
        {
            base.OnEnable();

            Services.Events.Register<StringHash32>(ExperimentEvents.SetupAddActor, SetupAddActor, this)
                .Register<StringHash32>(ExperimentEvents.SetupRemoveActor, SetupRemoveActor, this)
                .Register<WaterPropertyId>(ExperimentEvents.SetupAddWaterProperty, SetupWaterProperty, this)
                .Register(ExperimentEvents.SetupRemoveWaterProperty, SetupRemoveWaterProperty, this);

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
            if (inSelection.Tank == TankType.Measurement)
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

        private void SetupWaterProperty(WaterPropertyId inPropId)
        {
            m_Sprite.sprite = Services.Assets.WaterProp.Property(inPropId).Icon();
        }

        private void SetupRemoveWaterProperty()
        {
            m_Sprite.sprite = null;
        }

        private void ResetIdle()
        {
            m_IdleDuration = 0;
        }

    }
    
}