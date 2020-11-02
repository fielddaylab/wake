using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenHypothesis : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Button m_ContinueButton = null;
        [SerializeField] private Button m_BackButton = null;

        #endregion // Inspector

        public Action OnSelectContinue;
        public Action OnSelectBack;

        protected override void Awake()
        {
            m_ContinueButton.onClick.AddListener(() => OnSelectContinue?.Invoke());
            m_BackButton.onClick.AddListener(() => OnSelectBack?.Invoke());
        }
    }
}