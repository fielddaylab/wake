using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenCategory : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Button m_CritterButton = null;

        [SerializeField] private Button m_PropertyButton = null;
        [SerializeField] private Button m_BackButton = null;

        #endregion // Inspector

        public Action OnSelectCritter;

        public Action OnSelectProperty;
        public Action OnSelectBack;

        protected override void Awake()
        {
            m_CritterButton.onClick.AddListener(() => OnSelectCritter?.Invoke());
            m_PropertyButton.onClick.AddListener(() => OnSelectProperty?.Invoke());
            m_BackButton.onClick.AddListener(() => OnSelectBack?.Invoke());
        }
    }
}