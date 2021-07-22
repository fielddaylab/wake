using System;
using System.Collections.Generic;
using Aqua;
using Aqua.Profile;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.ExperimentV2
{
    public class TankStateUpdater : MonoBehaviour, ISceneOptimizable, ITimeHandler, ISceneLoadHandler
    {
        #region Inspector

        [SerializeField, HideInInspector] private SelectableTank[] m_Tanks = null;

        #endregion // Inspector

        [NonSerialized] private ScienceData m_Data;
        [NonSerialized] private Dictionary<StringHash32, SelectableTank> m_TankMap = new Dictionary<StringHash32, SelectableTank>();

        private void OnEnable()
        {
            Services.Time.Register(this);
            Services.Events.Register(GameEvents.ProfileRefresh, ReloadState, this);
        }

        private void OnDisable()
        {
            Services.Time?.Deregister(this);
            Services.Events?.DeregisterAll(this);
        }

        private void ReloadState()
        {
            m_Data = Services.Data.Profile.Science;
        }

        static private void UpdateTank(GTDate inCurrentTime, SelectableTank inTank, InProgressExperimentData inExperimentData, bool inbIsDirty)
        {
            if (inbIsDirty)
            {
                SetTankAsDirty(inTank);
                return;
            }

            if (inExperimentData == null)
            {
                SetTankAsAvailable(inTank);
                return;
            }

            float progress = GTDate.Progress(inExperimentData.Start, inExperimentData.Duration, inCurrentTime);
            if (progress < 1)
            {
                SetTankAsInProgress(inTank, progress);
            }
            else
            {
                SetTankAsFinished(inTank);
            }
        }

        static private void SetTankAsAvailable(SelectableTank inTank)
        {
            if (!Ref.Replace(ref inTank.CurrentAvailability, TankAvailability.Available))
                return;
            
            inTank.InUseRoot.SetActive(false);
            inTank.DirtyRoot.SetActive(false);
        }

        static private void SetTankAsDirty(SelectableTank inTank)
        {
            if (!Ref.Replace(ref inTank.CurrentAvailability, TankAvailability.Dirty))
                return;
            
            inTank.InUseRoot.SetActive(false);
            inTank.DirtyRoot.SetActive(true);
        }

        static private void SetTankAsFinished(SelectableTank inTank)
        {
            if (!Ref.Replace(ref inTank.CurrentAvailability, TankAvailability.TimedExperimentCompleted))
                return;
            
            inTank.InUseRoot.SetActive(true);
            inTank.DirtyRoot.SetActive(false);
            inTank.InProgressRoot.SetActive(false);
            inTank.ReadyRoot.SetActive(true);
        }

        static private void SetTankAsInProgress(SelectableTank inTank, float inProgress)
        {
            if (!Ref.Replace(ref inTank.CurrentAvailability, TankAvailability.TimedExperiment))
                return;
            
            inTank.InUseRoot.SetActive(true);
            inTank.DirtyRoot.SetActive(true);
            inTank.ReadyRoot.SetActive(false);
            inTank.InProgressRoot.SetActive(true);
            inTank.InProgressTimerScaler.SetScale(inProgress, Axis.X);
        }

        #region ITimeHandler

        TimeEvent ITimeHandler.EventMask()
        {
            return TimeEvent.Tick;
        }

        void ITimeHandler.OnTimeChanged(GTDate inGameTime)
        {
            SelectableTank tank;
            InProgressExperimentData experimentData;
            for(int i = 0, len = m_Tanks.Length; i < len; i++)
            {
                tank = m_Tanks[i];
                experimentData = m_Data.GetExperiment(tank.Id);
                UpdateTank(inGameTime, tank, experimentData, m_Data.IsTankDirty(tank.Id));
            }
        }

        #endregion // ITimeHandler

        #region ISceneOptimizable

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            m_Tanks = FindObjectsOfType<SelectableTank>();
        }

        #endif // UNITY_EDITOR

        #endregion // ISceneOptimizable

        #region ISceneLoadHandler

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            foreach(var tank in m_Tanks)
                m_TankMap.Add(tank.Id, tank);
            
            ReloadState();
        }

        #endregion // ISceneLoadHandler
    }
}