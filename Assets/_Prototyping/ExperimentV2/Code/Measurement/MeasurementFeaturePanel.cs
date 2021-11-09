using System;
using System.Collections;
using Aqua;
using BeauRoutine.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class MeasurementFeaturePanel : BasePanel {
        #region Inspector

        [SerializeField] private Toggle m_StabilizerToggle = null;
        [SerializeField] private Toggle m_AutoFeederToggle = null;
        [SerializeField] private GameObject m_StabilizerDisabledObject = null;

        #endregion // Inspector

        [NonSerialized] private MeasurementTank.FeatureMask m_SelectedFeatures = MeasurementTank.DefaultFeatures;

        public Action<MeasurementTank.FeatureMask> OnUpdated;

        protected override void Awake() {
            base.Awake();

            m_StabilizerToggle.onValueChanged.AddListener((b) => OnFeatureChanged(MeasurementTank.FeatureMask.Stabilizer, b));
            m_AutoFeederToggle.onValueChanged.AddListener((b) => OnFeatureChanged(MeasurementTank.FeatureMask.AutoFeeder, b));

            m_StabilizerToggle.SetIsOnWithoutNotify(true);
        }

        #region BasePanel

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            bool bHasStabilizer = Save.Inventory.HasUpgrade(ItemIds.MeasurementTankStabilizerToggle);
            m_StabilizerToggle.interactable = bHasStabilizer;
            m_StabilizerDisabledObject.SetActive(!bHasStabilizer);
        }

        protected override void InstantTransitionToShow() {
            CanvasGroup.Show(null);
        }

        protected override void InstantTransitionToHide() {
            CanvasGroup.Hide(null);
        }

        protected override IEnumerator TransitionToShow() {
            return CanvasGroup.Show(0.2f, null);
        }

        protected override IEnumerator TransitionToHide() {
            return CanvasGroup.Hide(0.2f, null);
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
                m_StabilizerToggle.SetIsOnWithoutNotify(true);
                m_AutoFeederToggle.SetIsOnWithoutNotify(false);
            }
        }

        private bool ClearSelectedSet() {
            if (m_SelectedFeatures != MeasurementTank.DefaultFeatures) {
                m_SelectedFeatures = MeasurementTank.DefaultFeatures;
                return true;
            }

            return false;
        }

        #endregion // Selected Set

        private void OnFeatureChanged(MeasurementTank.FeatureMask feature, bool value) {
            if (value) {
                m_SelectedFeatures |= feature;
            } else {
                m_SelectedFeatures &= ~feature;
            }

            OnUpdated?.Invoke(m_SelectedFeatures);
        }
    }
}