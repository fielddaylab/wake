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
    public class ExperimentTankSpawner : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private ExperimentTank[] m_Tanks = null;

        #endregion // Inspector

        private void Awake()
        {
            Services.Events.Register(ExperimentEvents.SetupInitialSubmit, OnInitialSubmit, this)
                .Register(ExperimentEvents.ExperimentTeardown, OnTeardown, this);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void OnInitialSubmit(object inArg)
        {
            TankSelectionData selectionData = (TankSelectionData) inArg;
            ActivateTank(selectionData);
            SetVars(selectionData);
        }

        private void OnTeardown()
        {
            foreach(var tank in m_Tanks)
                tank.Hide();

            Services.Data.SetVariable(ExperimentVars.TankType, null);
            Services.Data.SetVariable(ExperimentVars.EcoType, null);
        }

        private void ActivateTank(TankSelectionData inData)
        {
            for(int i = 0; i < m_Tanks.Length; ++i)
            {
                if (m_Tanks[i].TryHandle(inData))
                    return;
            }

            Debug.LogErrorFormat("[ExperimentTankSpawner] Unhandled tank selection type {0}+{1}", inData.Tank, inData.EcosystemId.ToDebugString());
        }

        private void SetVars(TankSelectionData inData)
        {
            Services.Data.SetVariable(ExperimentVars.TankType, inData.Tank.ToString());
            Services.Data.SetVariable(ExperimentVars.EcoType, inData.EcosystemId);
        }
    }
}