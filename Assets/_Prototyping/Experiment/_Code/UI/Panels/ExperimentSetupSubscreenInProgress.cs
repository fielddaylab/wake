using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenInProgress : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Button m_EndButton = null;

        #endregion // Inspector

        public override bool? ShouldCancelOnExit() { return false; }

        public Action OnSelectEnd;

        protected override void Awake()
        {
            m_EndButton.onClick.AddListener(() => OnSelectEnd?.Invoke());
        }

        public Action GetAction() {
            return OnSelectEnd;
        }
    }
}