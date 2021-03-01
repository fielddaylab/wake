using System;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenCategory : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Button m_CritterButton = null;

        [SerializeField] private Button m_PropertyButton = null;
        [SerializeField] private Button m_BackButton = null;

        [NonSerialized] private ExperimentSetupData m_CachedData;

        #endregion // Inspector

        public Action OnSelectCritter;

        public Action OnSelectProperty;
        public Action OnSelectBack;

        protected override void Awake()
        {
            m_CritterButton.onClick.AddListener(() => SetupMeasurementCritterY());
            m_PropertyButton.onClick.AddListener(() => OnSelectProperty?.Invoke());
            m_BackButton.onClick.AddListener(() => OnSelectBack?.Invoke());
        }

        public override void SetData(ExperimentSetupData inData)
        {
            base.SetData(inData);
            m_CachedData = inData;
        }

        private void SetupMeasurementCritterY() {
            m_CachedData.SetTargets("critter");

            OnSelectCritter?.Invoke();
        }
    }
}