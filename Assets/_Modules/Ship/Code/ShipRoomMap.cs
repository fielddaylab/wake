using System;
using System.Collections;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using EasyAssetStreaming;
using ProtoAqua.ExperimentV2;
using ScriptableBake;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Ship {
    public sealed class ShipRoomMap : BasePanel {

        #region Inspector

        [Header("Ship Map")]
        [SerializeField] private Button m_OpenButton = null;
        [SerializeField] private CanvasGroup m_ButtonGroup = null;
        [SerializeField] private CanvasGroup m_Fader = null;
        [SerializeField] private Vector2 m_OffscreenPos = default;
        [SerializeField] private TweenSettings m_ShowAnim = new TweenSettings(0.2f);
        [SerializeField] private TweenSettings m_HideAnim = new TweenSettings(0.2f);

        #endregion // Inspector

        [NonSerialized] private BaseInputLayer m_Input;
        [NonSerialized] private Vector2 m_OnscreenPos;
        [NonSerialized] private Routine m_ButtonAnim;

        protected override void Awake() {
            m_Input = BaseInputLayer.Find(this);
            m_OnscreenPos = Root.anchoredPosition;

            m_OpenButton.onClick.AddListener(() => Show());
            m_ButtonGroup.Show();

            Services.Events.Register(ExperimentEvents.ExperimentBegin, HideButton, this)
                .Register(ExperimentEvents.ExperimentEnded, ShowButton, this);
        }

        private void OnDestroy() {
            Services.Events?.DeregisterAll(this);
        }

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            m_Input.PushPriority();
            m_OpenButton.interactable = false;
        }

        protected override void OnHide(bool inbInstant) {
            if (!Services.Valid) {
                return;
            }

            m_Input?.PopPriority();
            m_OpenButton.interactable = true;
            base.OnHide(inbInstant);
        }

        protected override void OnHideComplete(bool inbInstant) {
            base.OnHideComplete(inbInstant);

            Root.SetAnchorPos(m_OffscreenPos);
        }

        protected override IEnumerator TransitionToHide() {
            yield return Routine.Combine(
                m_Fader.Hide(m_HideAnim.Time),
                Root.AnchorPosTo(m_OffscreenPos, m_HideAnim)
            );
            CanvasGroup.Hide();
        }

        protected override IEnumerator TransitionToShow() {
            CanvasGroup.Show();
            using(Script.DisableInput()) {
                while(Streaming.IsLoading()) {
                    yield return null;
                }
            }
            yield return Routine.Combine(
                m_Fader.Show(m_ShowAnim.Time),
                Root.AnchorPosTo(m_OnscreenPos, m_ShowAnim)
            );
        }

        protected override void InstantTransitionToHide() {
            m_Fader.Hide();
            CanvasGroup.Show();
            Root.SetAnchorPos(m_OffscreenPos);
        }

        protected override void InstantTransitionToShow() {
            m_Fader.Show();
            CanvasGroup.Hide();
            Root.SetAnchorPos(m_OnscreenPos);
        }

        #region Handlers

        private void HideButton() {
            m_ButtonAnim.Replace(this, m_ButtonGroup.Hide(0.2f));
        }

        private void ShowButton() {
            m_ButtonAnim.Replace(this, m_ButtonGroup.Show(0.2f));
        }

        #endregion // Handlers
    }
}