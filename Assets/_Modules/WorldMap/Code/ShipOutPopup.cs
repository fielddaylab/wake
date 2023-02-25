using System;
using System.Collections;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using EasyAssetStreaming;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.WorldMap {
    public class ShipOutPopup : BasePanel
    {
        #region Inspector

        [Header("Ship Out")]
        [SerializeField] private LocText m_StationName = null;
        [SerializeField] private LocText m_StationDesc = null;
        [SerializeField] private GameObject m_HasCurrentJobGroup = null;
        [SerializeField] private StreamingUGUITexture m_StationHeaderImage = null;
        
        [Header("Stats")]
        [SerializeField] private GameObject m_StatsGroup = null;
        [SerializeField] private GameObject m_AvailableGroup = null;
        [SerializeField] private TMP_Text m_AvailableCount = null;
        [SerializeField] private GameObject m_CompletedGroup = null;
        [SerializeField] private TMP_Text m_CompletedCount = null;

        [Header("Controls")]
        [SerializeField] private Button m_ShipOutButton = null;
        [SerializeField] private LocText m_ShipOutLabel = null;
        [SerializeField] private TextId m_ShipOutActiveLabel = null;
        [SerializeField] private TextId m_ShipOutInactiveLabel = null;

        [Header("Animation")]
        [SerializeField] private RectTransform m_ContentsTransform = null;
        [SerializeField] private LayoutGroup m_TotalLayout = null;

        #endregion // Inspector

        public Action OnShipOutClicked;

        [NonSerialized] private BaseInputLayer m_Input = null;

        protected override void Awake() {
            base.Awake();

            m_Input = BaseInputLayer.Find(this);
            m_ShipOutButton.onClick.AddListener(() => OnShipOutClicked?.Invoke());
        }

        public void Populate(MapDesc station, JobProgressSummary summary) {
            m_HasCurrentJobGroup.SetActive(summary.HasActive);
            m_StationName.SetText(station.LabelId());
            m_StationDesc.SetText(station.StationDescId());
            m_StationHeaderImage.Path = station.HeaderImagePath();

            if (station.Id() == MapIds.FinalStation) {
                m_StatsGroup.SetActive(false);
            } else {
                int availableCount = summary.Available + summary.InProgress;
                if (summary.HasActive) {
                    availableCount--;
                }

                m_AvailableCount.SetText(availableCount.ToStringLookup());
                m_CompletedCount.SetText(((int) summary.Completed).ToStringLookup());
                m_CompletedGroup.SetActive(summary.Completed > 0);
                m_AvailableGroup.SetActive(true);
                m_StatsGroup.SetActive(true);
            }

            bool atStation = Save.Current.Map.CurrentStationId() == station.Id();
            m_ShipOutButton.interactable = !atStation;
            m_ShipOutLabel.SetText(atStation ? m_ShipOutInactiveLabel : m_ShipOutActiveLabel);
        }

        protected override void OnHideComplete(bool inbInstant) {
            base.OnHideComplete(inbInstant);
            m_ContentsTransform.SetScale(0, Axis.Y);
            m_Input.PopPriority();
        }

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);
            m_Input.PushPriority();
        }

        protected override void OnShowComplete(bool inbInstant) {
            base.OnShowComplete(inbInstant);
            m_ContentsTransform.SetScale(1, Axis.Y);
        }

        protected override IEnumerator TransitionToShow() {
            m_RootGroup.gameObject.SetActive(true);
            m_StationHeaderImage.Preload();
            yield return m_RootGroup.FadeTo(1, 0.2f);
            while(m_StationHeaderImage.IsLoading()) {
                yield return null;
            }
            yield return m_ContentsTransform.ScaleTo(1, 0.3f, Axis.Y).Ease(Curve.CubeOut).OnUpdate((f) => {
                m_TotalLayout.ForceRebuild();
            });;
        }

        protected override IEnumerator TransitionToHide() {
            yield return m_ContentsTransform.ScaleTo(0, 0.3f, Axis.Y).Ease(Curve.CubeIn).OnUpdate((f) => {
                m_TotalLayout.ForceRebuild();
            });
            yield return m_RootGroup.FadeTo(0, 0.2f);
            m_RootGroup.gameObject.SetActive(false);
        }
    }
}