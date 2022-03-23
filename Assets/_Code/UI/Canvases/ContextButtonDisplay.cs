using System;
using System.Collections;
using Aqua.Character;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil.Debugger;

namespace Aqua {
    public class ContextButtonDisplay : SharedPanel {

        #region Inspector

        [Header("Components")]
        [SerializeField] private Button m_InteractButton = null;
        [SerializeField] private LocText m_InteractLabel = null;
        [SerializeField] private Image m_InteractButtonIcon = null;
        [SerializeField] private CursorInteractionHint m_HoverHint = null;
        [SerializeField] private RectTransformPinned m_PinGroup = null;
        [SerializeField] private RectTransform m_AdjustGroup = null;

        [Header("Defaults")]
        [SerializeField] private Sprite m_MapIcon = null;
        [SerializeField] private TextId m_MapLabel = null;
        [SerializeField] private Sprite m_InspectIcon = null;
        [SerializeField] private TextId m_InspectLabel = null;
        [SerializeField] private Sprite m_BackIcon = null;
        [SerializeField] private TextId m_BackLabel = null;
        [SerializeField] private TextId m_LockedLabel = null;

        #endregion // Inspector

        [NonSerialized] private SceneInteractable m_TargetInteract;
        private Routine m_BumpAnimation;

        protected override void Start() {
            base.Start();

            m_InteractButton.onClick.AddListener(OnButtonClicked);
        }

        public void DisplayInteract(SceneInteractable inObject) {
            m_TargetInteract = inObject;
            Script.WriteVariable(GameVars.InteractObject, inObject.Parent.Id());

            Sprite icon = null;
            string label = null;

            switch(inObject.Mode()) {
                case SceneInteractable.InteractionMode.GoToMap: {
                    icon = inObject.Icon(m_MapIcon);
                    Assert.True(!inObject.TargetMapId().IsEmpty, "Interaction {0} has no assigned map", inObject);
                    label = Loc.Format(inObject.Label(m_MapLabel), Assets.Map(inObject.TargetMapId()).ShortLabelId());
                    break;
                }

                case SceneInteractable.InteractionMode.GoToPreviousScene: {
                    icon = inObject.Icon(m_BackIcon);
                    label = Loc.Find(inObject.Label(m_BackLabel));
                    break;
                }

                case SceneInteractable.InteractionMode.Inspect: {
                    icon = inObject.Icon(m_InspectIcon);
                    label = Loc.Find(inObject.Label(m_InspectLabel));
                    break;
                }
            }

            m_InteractButtonIcon.sprite = icon;
            m_InteractButtonIcon.gameObject.SetActive(icon);
            m_InteractButton.interactable = inObject.CanInteract();
            m_HoverHint.TooltipOverride = label;
            m_HoverHint.TooltipId = null;
            m_InteractLabel.SetTextFromString(label);

            inObject.ConfigurePin(m_PinGroup);

            Show();
            SetInputState(true);
        }

        public void DisplayLocked(SceneInteractable inObject) {
            if (m_TargetInteract != inObject)
                return;

            string label = Loc.Find(inObject.LockedLabel(m_LockedLabel));

            m_InteractButtonIcon.gameObject.SetActive(false);
            m_InteractButton.interactable = false;
            m_HoverHint.TooltipOverride = label;
            m_InteractLabel.SetTextFromString(label);

            if (!IsTransitioning()) {
                m_BumpAnimation.Replace(this, BumpAnimation());
            }
        }

        public void ClearInteract(SceneInteractable inObject) {
            if (!m_TargetInteract.IsReferenceEquals(inObject)) {
                return;
            }

            m_TargetInteract = null;
            Script.WriteVariable(GameVars.InteractObject, null);
            Hide();
        }

        #region Handlers

        private void OnButtonClicked() {
            m_TargetInteract?.Interact();
        }

        #endregion // Handlers

        #region Panel

        protected override void OnHide(bool inbInstant) {
            base.OnHide(inbInstant);
            m_BumpAnimation.Stop();
        }

        protected override void OnHideComplete(bool inbInstant) {
            m_TargetInteract = null;

            m_PinGroup.Unpin();
            m_AdjustGroup.SetAnchorPos(-16, Axis.Y);
            m_RootGroup.alpha = 0;
            m_InteractButtonIcon.sprite = null;

            base.OnHideComplete(inbInstant);
        }

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);
            m_BumpAnimation.Stop();
        }

        protected override IEnumerator TransitionToShow() {
            m_RootTransform.gameObject.SetActive(true);

            yield return Routine.Combine(
                m_RootGroup.FadeTo(1, 0.2f).Ease(Curve.QuadOut),
                m_AdjustGroup.AnchorPosTo(0, 0.2f, Axis.Y).Ease(Curve.QuadOut)
            );
        }

        protected override IEnumerator TransitionToHide() {
            yield return Routine.Combine(
                m_AdjustGroup.AnchorPosTo(-16, 0.2f, Axis.Y).Ease(Curve.QuadIn),
                m_RootGroup.FadeTo(0, 0.2f).Ease(Curve.QuadIn)
            );

            m_RootTransform.gameObject.SetActive(false);
        }

        private IEnumerator BumpAnimation() {
            m_RootTransform.gameObject.SetActive(true);
            m_AdjustGroup.SetAnchorPos(-8, Axis.Y);
            m_RootGroup.alpha = 0.5f;

            yield return Routine.Combine(
                m_RootGroup.FadeTo(1, 0.2f).Ease(Curve.QuadOut),
                m_AdjustGroup.AnchorPosTo(0, 0.2f, Axis.Y).Ease(Curve.QuadOut)
            );
        }

        #endregion // Panel
    
        static public void Display(SceneInteractable inObject) {
            Services.UI.FindPanel<ContextButtonDisplay>()?.DisplayInteract(inObject);
        }

        static public void Locked(SceneInteractable inObject) {
            Services.UI.FindPanel<ContextButtonDisplay>()?.DisplayLocked(inObject);
        }

        static public void Clear(SceneInteractable inObject) {
            Services.UI?.FindPanel<ContextButtonDisplay>()?.ClearInteract(inObject);
        }
    }
}