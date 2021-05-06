using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauRoutine;
using Aqua;
using UnityEngine.UI;
using BeauUtil;
using System;

namespace ProtoAqua.Navigation
{
    public class NavigationUI : SharedPanel
    {
        static public readonly StringHash32 Event_Dive = "nav:dive";

        static private readonly StringHash32 DiveTooltipKey = "ui.nav.dive.tooltip";
        static private readonly StringHash32 DockTooltipKey = "ui.nav.dock.tooltip";
        static private readonly StringHash32 DockLabelKey = "ui.nav.dock.label";
        
        private enum Mode
        {
            Dive,
            Dock,
            Inspect
        }

        #region Inspector

        [SerializeField] private Button m_InteractButton = null;
        [SerializeField] private LocText m_InteractLabel = null;
        [SerializeField] private Image m_InteractButtonIcon = null;
        [SerializeField] private CursorInteractionHint m_HoverHint = null; 
        [SerializeField] private RectTransformPinned m_PinGroup = null;
        [SerializeField] private RectTransform m_AdjustGroup = null;

        [Header("Assets")]
        [SerializeField] private Sprite m_DiveIcon = null;
        [SerializeField] private Sprite m_DockIcon = null;
        [SerializeField] private Sprite m_InspectIcon = null;

        #endregion // Inspector

        [NonSerialized] private string m_TargetScene;
        [NonSerialized] private ScriptObject m_TargetInspect;
        [NonSerialized] private Mode m_Mode;

        protected override void Start()
        {
            base.Start();

            m_InteractButton.onClick.AddListener(OnButtonClicked);
        }

        public void DisplayDive(Transform inTransform, string inLabel, string inScene)
        {
            m_Mode = Mode.Dive;
            m_TargetScene = inScene;
            m_TargetInspect = null;

            m_InteractButtonIcon.sprite = m_DiveIcon;
            m_HoverHint.TooltipId = DiveTooltipKey;
            m_InteractLabel.SetText(inLabel);
            m_PinGroup.Pin(inTransform);

            Show();
        }

        public void DisplayDock(Transform inTransform)
        {
            m_Mode = Mode.Dock;
            m_TargetScene = null;
            m_TargetInspect = null;

            m_InteractButtonIcon.sprite = m_DockIcon;
            m_HoverHint.TooltipId = DockTooltipKey;
            m_InteractLabel.SetText(DockLabelKey);
            m_PinGroup.Pin(inTransform);
            
            Show();
        }

        public void DisplayInspect(ScriptObject inObject, TextId inTextId, TextId inInspectId)
        {
            m_Mode = Mode.Inspect;
            m_TargetScene = null;
            m_TargetInspect = inObject;

            m_InteractButtonIcon.sprite = m_DockIcon;
            m_HoverHint.TooltipId = inInspectId;
            m_InteractLabel.SetText(inTextId);
            m_PinGroup.Pin(inObject.transform);
            
            Show();
        }

        #region Handlers

        private void OnButtonClicked()
        {
            switch(m_Mode)
            {
                case Mode.Dive:
                    {
                        Hide();
                        Services.Events.Dispatch(GameEvents.BeginDive, m_TargetScene);
                        Routine.Start(FadeRoutine(m_TargetScene));
                        break;
                    }

                case Mode.Dock:
                    {
                        Hide();
                        StateUtil.LoadSceneWithWipe("Ship", null);
                        break;
                    }
            }
        }

        #endregion // Handlers

        #region Panel

        protected override void OnHide(bool inbInstant)
        {
            m_TargetInspect = null;

            base.OnHide(inbInstant);
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            m_PinGroup.Unpin();
            m_AdjustGroup.SetAnchorPos(-16, Axis.Y);
            m_RootGroup.alpha = 0;

            base.OnHideComplete(inbInstant);
        }

        protected override IEnumerator TransitionToShow()
        {
            m_RootTransform.gameObject.SetActive(true);

            yield return Routine.Combine(
                m_RootGroup.FadeTo(1, 0.2f).Ease(Curve.QuadOut),
                m_AdjustGroup.AnchorPosTo(0, 0.2f, Axis.Y).Ease(Curve.QuadOut)
            );
        }

        protected override IEnumerator TransitionToHide()
        {
            yield return Routine.Combine(
                m_AdjustGroup.AnchorPosTo(-16, 0.2f, Axis.Y).Ease(Curve.QuadIn),
                m_RootGroup.FadeTo(0, 0.2f).Ease(Curve.QuadIn)
            );

            m_RootTransform.gameObject.SetActive(false);
        }

        #endregion // Panel

        static private IEnumerator FadeRoutine(string inTargetScene)
        {
            Services.UI.ShowLetterbox();
            Services.Data.SetVariable(GameVars.DiveSite, inTargetScene);
            Services.Events.Dispatch(Event_Dive, inTargetScene);
            yield return 2;
            StateUtil.LoadSceneWithWipe(inTargetScene);
            yield return 0.3f;
            Services.UI.HideLetterbox();
        }
    }
}
