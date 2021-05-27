using BeauUtil;
using System;
using UnityEngine;
using BeauRoutine.Extensions;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreen : BasePanel
    {
        #region Inspector

        [Header("Subscreen")]
        [SerializeField] private string m_ScreenId = null;

        #endregion // Inspector

        [NonSerialized] private ExperimentSettings m_CachedSettings;

        protected ExperimentSettings Config { get; private set; }
        protected ExperimentSetupData Setup { get; private set; }

        public virtual void Initialize(ExperimentSetupData inData, ExperimentSettings inConfig)
        {
            Setup = inData;
            Config = inConfig;
        }

        public virtual bool? ShouldCancelOnExit() { return null; }

        protected virtual void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        protected virtual void RestoreState() { }

        #region BasePanel

        protected override void OnShow(bool inbInstant)
        {
            if (!WasShowing())
            {
                Services.Data.SetVariable(ExperimentVars.SetupPanelScreen, m_ScreenId);
            }
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            RestoreState();
        }

        #endregion // BasePanel
    }
}