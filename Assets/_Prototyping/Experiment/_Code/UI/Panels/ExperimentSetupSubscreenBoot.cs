using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenBoot : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Button m_ContinueButton = null;

        #endregion // Inspector

        [NonSerialized] public Action OnSelectContinue;

        protected override void Awake()
        {
            m_ContinueButton.onClick.AddListener(() => OnSelectContinue?.Invoke());
        }

        public Action GetAction() {
            return OnSelectContinue;
        }
    }
}