using System;
using UnityEngine;
using Aqua;
using BeauUtil.Debugger;

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

        private void Awake()
        {

            m_CachedSettings = Services.Tweaks.Get<ExperimentSettings>();

            Services.Events.Register(ExperimentEvents.SetupInitialSubmit, OnInitialSubmit, this)
                .Register(ExperimentEvents.ExperimentTeardown, OnTeardown, this)
                .Register(ExperimentEvents.ExperimentBegin, OnBegin, this)
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
        }

        private void GenerateResult(ExperimentResultData ioResult)
        {
            ioResult.Duration = ExperimentServices.Actors.TimeDuration();
            Services.Data.SetVariable(ExperimentVars.ExperimentDuration, ioResult.Duration);
            m_CurrentTank.GenerateResult(ioResult);
            Services.Data.SetVariable(ExperimentVars.ExperimentBehaviorCount, ioResult.NewFactIds.Count);
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
            m_CurrentData = inData;

            for(int i = 0; i < m_Tanks.Length; ++i)
            {
                if (m_Tanks[i].TryHandle(inData))
                {
                    m_CurrentTank = m_Tanks[i];
                    return;
                }
            }

            Log.Error("[ExperimentTankSpawner] Unhandled tank selection type {0}+{1}", inData.Tank, inData.EnvironmentId);
        }

        private void SetVars(ExperimentSetupData inData)
        {
            var settings = Services.Tweaks.Get<ExperimentSettings>();

            m_CurrentData = inData;

            Services.Data.SetVariable(ExperimentVars.TankType, inData.Tank.ToString());
            Services.Data.SetVariable(ExperimentVars.TankTypeLabel, settings.GetTank(inData.Tank).ShortLabelId.Hash());
            
            if (inData.Tank.Equals(TankType.Foundational))
            {
                Services.Data.SetVariable(ExperimentVars.EcoType, inData.EnvironmentId);
                Services.Data.SetVariable(ExperimentVars.EcoTypeLabel, Services.Assets.Bestiary.Get(inData.EnvironmentId).CommonName().Hash());
            }
        }
    }
}