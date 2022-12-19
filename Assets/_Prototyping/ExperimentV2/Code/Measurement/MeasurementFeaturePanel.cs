using System;
using System.Collections;
using Aqua;
using BeauRoutine.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class MeasurementFeaturePanel : MonoBehaviour {
        #region Inspector

        [SerializeField] private Toggle m_StabilizerToggle = null;
        [SerializeField] private Toggle m_AutoFeederToggle = null;
        [SerializeField] private GameObject m_StabilizerDisabledObject = null;
        [SerializeField] private GameObject m_AutoFeederDisabledObject = null;

        #endregion // Inspector

        [NonSerialized] private MeasurementTank.FeatureMask m_SelectedFeatures = MeasurementTank.DefaultFeatures;

        public Action<MeasurementTank.FeatureMask> OnUpdated;

        private void Awake() {
            m_StabilizerToggle.onValueChanged.AddListener((b) => OnFeatureChanged(MeasurementTank.FeatureMask.Stabilizer, b));
            m_AutoFeederToggle.onValueChanged.AddListener((b) => OnFeatureChanged(MeasurementTank.FeatureMask.AutoFeeder, b));
        }

        #region BasePanel

        private void OnEnable() {
            bool bHasStabilizer = Save.Inventory.HasUpgrade(ItemIds.WaterStabilizer);
            m_StabilizerToggle.interactable = bHasStabilizer;
            // m_StabilizerDisabledObject.SetActive(!bHasStabilizer);

            bool bHasFeeder = Save.Inventory.HasUpgrade(ItemIds.AutoFeeder);
            m_AutoFeederToggle.interactable = bHasFeeder;
            // m_AutoFeederDisabledObject.SetActive(!bHasFeeder);
        }

        #endregion // BasePanel

        #region Selected Set

        public MeasurementTank.FeatureMask Selected {
            get { return m_SelectedFeatures; }
        }

        public bool IsSelected(MeasurementTank.FeatureMask inEntry) {
            return (m_SelectedFeatures & inEntry) != 0;
        }

        public void ClearSelection() {
            if (ClearSelectedSet()) {
                m_StabilizerToggle.SetIsOnWithoutNotify(false);
                m_AutoFeederToggle.SetIsOnWithoutNotify(false);
            }
        }

        private bool ClearSelectedSet() {
            if (m_SelectedFeatures != MeasurementTank.DefaultFeatures) {
                m_SelectedFeatures = MeasurementTank.DefaultFeatures;
                OnUpdated?.Invoke(m_SelectedFeatures);
                return true;
            }

            return false;
        }

        #endregion // Selected Set

        private void OnFeatureChanged(MeasurementTank.FeatureMask feature, bool value) {
            if (value) {
                m_SelectedFeatures |= feature;
                Services.Events.Dispatch(ExperimentEvents.ExperimentEnableFeature, feature);
            } else {
                m_SelectedFeatures &= ~feature;
                Services.Events.Dispatch(ExperimentEvents.ExperimentDisableFeature, feature);
            }

            OnUpdated?.Invoke(m_SelectedFeatures);
        }
    }
}