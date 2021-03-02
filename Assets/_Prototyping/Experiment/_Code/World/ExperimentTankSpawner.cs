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
using System.Collections.Generic;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentTankSpawner : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private ExperimentTank[] m_Tanks = null;

        #endregion // Inspector

        [NonSerialized] private ExperimentTank m_CurrentTank = null;

        [NonSerialized] private TankType m_CurrentTankType = TankType.None;

        [NonSerialized] private ExperimentSettings m_CachedSettings;

        [NonSerialized] private ExperimentSetupData m_CurrentData = null;
        [NonSerialized] private HashSet<StringHash32> m_ObservedBehaviors = new HashSet<StringHash32>();

        private void Awake()
        {

            m_CachedSettings = Services.Tweaks.Get<ExperimentSettings>();

            Services.Events.Register(ExperimentEvents.SetupInitialSubmit, OnInitialSubmit, this)
                .Register(ExperimentEvents.ExperimentTeardown, OnTeardown, this)
                .Register(ExperimentEvents.ExperimentBegin, OnBegin, this)
                .Register<StringHash32>(ExperimentEvents.BehaviorAddedToLog, OnBehaviorRecorded, this)
                .Register<ExperimentResultData>(ExperimentEvents.ExperimentRequestSummary, GenerateResult, this)
                .Register<TankType>(ExperimentEvents.SetupTank, SetTank, this);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void OnInitialSubmit(object inArg)
        {
            ExperimentSetupData selectionData = (ExperimentSetupData) inArg;
            ActivateTank(selectionData);
            SetVars(selectionData);
        }

        private void SetTank(TankType tank) {
            m_CurrentTankType = tank;
        }

        private void OnBegin()
        {
            ExperimentServices.Actors.BeginTicking();
            Services.Data.SetVariable(ExperimentVars.ExperimentRunning, true);
            m_CurrentTank.OnExperimentStart();
            if(m_CurrentTankType.Equals(TankType.Stressor)) {
                Services.Events.Dispatch(ExperimentEvents.StressorColor, m_CurrentData.PropertyId);
            }
            m_ObservedBehaviors.Clear();
        }

        private void GenerateResult(ExperimentResultData ioResult)
        {
            ioResult.Duration = ExperimentServices.Actors.TimeDuration();
            Services.Data.SetVariable(ExperimentVars.ExperimentDuration, ioResult.Duration);
            Services.Data.SetVariable(ExperimentVars.ExperimentBehaviorCount, m_ObservedBehaviors.Count);
            foreach(var behavior in m_ObservedBehaviors)
                ioResult.ObservedBehaviorIds.Add(behavior);
            m_ObservedBehaviors.Clear();
        }

        private void OnTeardown()
        {
            ExperimentServices.Actors.StopTicking();
            m_CurrentTank.OnExperimentEnd();

            foreach(var tank in m_Tanks)
                tank.Hide();

            m_CurrentTank = null;

            Services.Data.SetVariable(ExperimentVars.TankType, null);
            Services.Data.SetVariable(ExperimentVars.TankTypeLabel, null);
            Services.Data.SetVariable(ExperimentVars.EcoType, null);
            Services.Data.SetVariable(ExperimentVars.EcoTypeLabel, null);
            Services.Data.SetVariable(ExperimentVars.ExperimentRunning, false);

            ExperimentServices.Actors.Pools.ResetAll();
        }

        private void ActivateTank(ExperimentSetupData inData)
        {
            m_ObservedBehaviors.Clear();

            m_CurrentData = inData;

            for(int i = 0; i < m_Tanks.Length; ++i)
            {
                if (m_Tanks[i].TryHandle(inData))
                {
                    m_CurrentTank = m_Tanks[i];
                    return;
                }
            }

            Debug.LogErrorFormat("[ExperimentTankSpawner] Unhandled tank selection type {0}+{1}", inData.Tank, inData.EcosystemId.ToDebugString());
        }

        private void SetVars(ExperimentSetupData inData)
        {
            var settings = Services.Tweaks.Get<ExperimentSettings>();

            m_CurrentData = inData;

            Services.Data.SetVariable(ExperimentVars.TankType, inData.Tank.ToString());
            Services.Data.SetVariable(ExperimentVars.TankTypeLabel, settings.GetTank(inData.Tank).ShortLabelId.Hash());
            if (inData.Tank.Equals(TankType.Foundational))
            {
                Services.Data.SetVariable(ExperimentVars.EcoType, inData.EcosystemId);
                Services.Data.SetVariable(ExperimentVars.EcoTypeLabel, Services.Assets.Bestiary.Get(inData.EcosystemId).CommonName());
            }

        }

        private void OnBehaviorRecorded(StringHash32 inBehaviorId)
        {
            m_ObservedBehaviors.Add(inBehaviorId);
        }
    }
}