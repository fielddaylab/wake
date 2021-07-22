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

        // [SerializeField] private SpriteRenderer m_Sprite = null;

        [SerializeField] private ColorGroup m_WaterColor = null;

        #endregion // Inspector

        [NonSerialized] private AudioHandle m_AudioLoop;

        [NonSerialized] private float min_Alpha = 0;
        [NonSerialized] private float max_Alpha = 0;
        [NonSerialized] private Color m_CurrentColor;
        [NonSerialized] private float defaultAlpha;


        protected override void Awake()
        {
            base.Awake();

            min_Alpha = m_WaterColor.GetAlpha();
            max_Alpha = min_Alpha + 0.25f;
            m_CurrentColor = m_WaterColor.GetColor();
            defaultAlpha = m_WaterColor.GetAlpha();
        }


        protected override void OnEnable()
        {
            base.OnEnable();

            Services.Events.Register<StringHash32>(ExperimentEvents.SetupAddActor, SetupAddActor, this)
                .Register<StringHash32>(ExperimentEvents.SetupRemoveActor, SetupRemoveActor, this);
            // .Register<WaterPropertyId>(ExperimentEvents.SetupAddWaterProperty, SetupWaterProperty, this)
            // .Register(ExperimentEvents.SetupRemoveWaterProperty, SetupRemoveWaterProperty, this);

            m_AudioLoop = Services.Audio.PostEvent("tank_water_loop");
        }

        protected override void OnDisable()
        {
            base.OnEnable();

            Services.Events?.DeregisterAll(this);

            m_AudioLoop.Stop();
        }

        public override void GenerateResult(ExperimentResultData ioData)
        {
            base.GenerateResult(ioData);

            BestiaryDesc critter = Services.Assets.Bestiary.Get(ioData.Setup.CritterId);
            ActorStateId state = critter.EvaluateActorState(ioData.Setup.EnvironmentProperties, out var _);
            BFProduce produceFact = BestiaryUtils.FindProduceRule(critter, ioData.Setup.PropertyId, state);
            BFConsume consumeFact = BestiaryUtils.FindConsumeRule(critter, ioData.Setup.PropertyId, state);
            
            if (produceFact != null)
            {
                if (Services.Data.Profile.Bestiary.RegisterFact(produceFact.Id()))
                {
                    ioData.NewFactIds.Add(produceFact.Id());
                }
            }

            if (consumeFact != null)
            {
                if (Services.Data.Profile.Bestiary.RegisterFact(consumeFact.Id()))
                {
                    ioData.NewFactIds.Add(consumeFact.Id());
                }
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
    }
    
}