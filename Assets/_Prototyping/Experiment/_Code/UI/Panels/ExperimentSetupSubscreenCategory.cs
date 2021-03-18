using System;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using Aqua;
using TMPro;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenCategory : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Button m_CritterButton = null;

        [SerializeField] private Button m_PropertyButton = null;
        [SerializeField] private Button m_BackButton = null;

        [SerializeField] private Transform m_NoInfo = null;

        #endregion // Inspector

        [NonSerialized] private ExperimentSetupData m_CachedData;

        public Action OnSelectCritter;

        public Action OnSelectProperty;
        public Action OnSelectBack;

        protected override void Awake()
        {
            m_CritterButton.onClick.AddListener(() => SetupMeasurementCritterY());
            m_PropertyButton.onClick.AddListener(() => OnSelectProperty?.Invoke());
            m_BackButton.onClick.AddListener(() => OnSelectBack?.Invoke());

            m_PropertyButton.enabled = false;

            UpdateDisplay();

        }

        public override void Refresh()
        {
            base.Refresh();
            UpdateDisplay();
        }

        private void UpdateDisplay() {
            if(m_CachedData.CritterX == StringHash32.Null) return;

            if(m_CachedData.GetTargets().Count == 0) {
                m_NoInfo.gameObject.SetActive(true);
                m_PropertyButton.gameObject.SetActive(false);
                m_CritterButton.gameObject.SetActive(false);
            }
            else {
                m_NoInfo.gameObject.SetActive(false);
                m_PropertyButton.gameObject.SetActive(true);
                m_CritterButton.gameObject.SetActive(true);
            }
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