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

        public virtual void SetData(ExperimentSetupData inData) { }
        public virtual void Refresh() { }


        public virtual bool? ShouldCancelOnExit() { return null; }


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
            Refresh();
        }

        #endregion // BasePanel
    }
}