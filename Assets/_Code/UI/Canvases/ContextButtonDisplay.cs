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

        [Header("Label")]
        [SerializeField] private LocText m_Label = null;

        [Header("Button")]
        [SerializeField] private Button m_ActionButton = null;
        [SerializeField] private ColorGroup m_ActionButtonColor = null;
        [SerializeField] private ColorGroup m_ActionLabelColor = null;
        [SerializeField] private LocText m_ActionLabel = null;
        [SerializeField] private CursorInteractionHint m_HoverHint = null;
        
        [Header("Pin")]
        [SerializeField] private RectTransformPinned m_PinGroup = null;
        [SerializeField] private RectTransform m_AdjustGroup = null;
        
        [Header("Colors")]
        [SerializeField] private ColorPalette2 m_ActiveButtonPalette = default;
        [SerializeField] private ColorPalette2 m_LockedButtonPalette = default;

        [Header("Defaults")]
        [SerializeField] private TextId m_MapLabel = null;
        [SerializeField] private TextId m_MapActionLabel = null;
        [SerializeField] private TextId m_MapActionLockedLabel = null;
        [SerializeField] private TextId m_InspectLabel = null;
        [SerializeField] private TextId m_InspectActionLabel = null;
        [SerializeField] private TextId m_InspectActionLockedLabel = null;
        [SerializeField] private TextId m_BackLabel = null;
        [SerializeField] private TextId m_BackActionLabel = null;
        [SerializeField] private TextId m_BackActionLockedLabel = null;

        #endregion // Inspector

        [NonSerialized] private RuntimeObjectHandle<SceneInteractable> m_TargetInteract;

        protected override void Start() {
            base.Start();

            m_ActionButton.onClick.AddListener(OnButtonClicked);
        }

        public void DisplayInteract(SceneInteractable inObject) {
            m_TargetInteract = inObject;
            Script.WriteVariable(GameVars.InteractObject, inObject.Parent.Id());

            string label = null;
            TextId actionLabel = null;
            bool locked = inObject.Locked();

            switch(inObject.Mode()) {
                case SceneInteractable.InteractionMode.GoToMap: {
                    Assert.True(!inObject.TargetMapId().IsEmpty, "Interaction {0} has no assigned map", inObject);
                    label = Loc.Format(inObject.Label(m_MapLabel), Assets.Map(inObject.TargetMapId()).LabelId());
                    actionLabel = locked ? inObject.LockedActionLabel(m_MapActionLockedLabel) : inObject.ActionLabel(m_MapActionLabel);
                    break;
                }

                case SceneInteractable.InteractionMode.GoToPreviousScene: {
                    label = Loc.Find(inObject.Label(m_BackLabel));
                    actionLabel = locked ? inObject.LockedActionLabel(m_BackActionLockedLabel) : inObject.ActionLabel(m_BackActionLabel);
                    break;
                }

                case SceneInteractable.InteractionMode.Inspect: {
                    label = Loc.Find(inObject.Label(m_InspectLabel));
                    actionLabel = locked ? inObject.LockedActionLabel(m_InspectActionLockedLabel) : inObject.ActionLabel(m_InspectActionLabel);
                    break;
                }
            }

            if (locked) {
                m_ActionButtonColor.Color = m_LockedButtonPalette.Background;
                m_ActionLabelColor.Color = m_LockedButtonPalette.Content;
            } else {
                m_ActionButtonColor.Color = m_ActiveButtonPalette.Background;
                m_ActionLabelColor.Color = m_ActiveButtonPalette.Content;
            }

            m_Label.SetTextFromString(label);
            m_ActionLabel.SetText(actionLabel);
            m_ActionButton.interactable = inObject.CanInteract();
            m_HoverHint.TooltipOverride = label;
            m_HoverHint.TooltipId = null;

            inObject.ConfigurePin(m_PinGroup);

            Show();
            SetInputState(true);
        }

        public void ClearInteract(SceneInteractable inObject) {
            if (m_TargetInteract != inObject) {
                return;
            }

            m_TargetInteract = null;
            Script.WriteVariable(GameVars.InteractObject, null);
            Hide();
        }

        #region Handlers

        private void OnButtonClicked() {
            m_TargetInteract.Object?.Interact();
        }

        #endregion // Handlers

        #region Panel

        protected override void OnHide(bool inbInstant) {
            base.OnHide(inbInstant);

            if (WasShowing()) {
                Services.Events.Dispatch(GameEvents.ContextHide);
            }
        }

        protected override void OnHideComplete(bool inbInstant) {
            m_TargetInteract = null;

            m_PinGroup.Unpin();
            m_AdjustGroup.SetAnchorPos(-16, Axis.Y);
            m_RootGroup.alpha = 0;

            base.OnHideComplete(inbInstant);
        }

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            if (!WasShowing()) {
                Services.Events.Dispatch(GameEvents.ContextDisplay);
            }
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

        #endregion // Panel
    
        static public void Display(SceneInteractable inObject) {
            Services.UI.FindPanel<ContextButtonDisplay>()?.DisplayInteract(inObject);
        }

        static public void Clear(SceneInteractable inObject) {
            Services.UI?.FindPanel<ContextButtonDisplay>()?.ClearInteract(inObject);
        }

        static public bool IsDisplaying() {
            return Services.UI?.FindPanel<ContextButtonDisplay>().IsShowing() ?? false;
        }
    }
}