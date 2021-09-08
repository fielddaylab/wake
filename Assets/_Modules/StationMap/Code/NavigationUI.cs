using System.Collections;
using UnityEngine;
using BeauRoutine;
using UnityEngine.UI;
using BeauUtil;
using System;

namespace Aqua.StationMap
{
    public class NavigationUI : SharedPanel
    {
        static public readonly StringHash32 Event_Dive = "nav:dive";

        static private readonly StringHash32 DiveTooltipKey = "ui.nav.dive.tooltip";
        static private readonly StringHash32 DockTooltipKey = "ui.nav.dock.tooltip";
        static private readonly StringHash32 DockLabelKey = "ui.nav.dock.label";
        static private readonly StringHash32 MapTooltipKey = "ui.nav.map.tooltip";
        
        private enum Mode
        {
            Dive,
            Dock,
            Inspect,
            Map
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
        [SerializeField] private Sprite m_MapIcon = null;

        #endregion // Inspector

        [NonSerialized] private MapDesc m_TargetMap;
        [NonSerialized] private ScriptObject m_TargetInspect;
        [NonSerialized] private Mode m_Mode;

        protected override void Start()
        {
            base.Start();

            m_InteractButton.onClick.AddListener(OnButtonClicked);
        }

        public void DisplayDive(Transform inTransform, StringHash32 inMapId)
        {
            m_Mode = Mode.Dive;
            m_TargetMap = Assets.Map(inMapId);
            m_TargetInspect = null;

            m_InteractButtonIcon.sprite = m_DiveIcon;
            m_HoverHint.TooltipId = DiveTooltipKey;
            m_HoverHint.TooltipOverride = null;
            m_InteractLabel.SetText(m_TargetMap.LabelId());
            m_PinGroup.Pin(inTransform);

            Show();
        }

        public void DisplayMap(Transform inTransform, StringHash32 inMapId)
        {
            m_Mode = Mode.Map;
            m_TargetMap = Assets.Map(inMapId);
            m_TargetInspect = null;

            m_InteractButtonIcon.sprite = m_MapIcon;
            m_HoverHint.TooltipId = default;
            m_HoverHint.TooltipOverride = Loc.Format(MapTooltipKey, m_TargetMap.LabelId());
            m_InteractLabel.SetText(m_TargetMap.LabelId());
            m_PinGroup.Pin(inTransform);

            Show();
        }

        public void DisplayDock(Transform inTransform)
        {
            m_Mode = Mode.Dock;
            m_TargetMap = null;
            m_TargetInspect = null;

            m_InteractButtonIcon.sprite = m_DockIcon;
            m_HoverHint.TooltipId = DockTooltipKey;
            m_HoverHint.TooltipOverride = null;
            m_InteractLabel.SetText(DockLabelKey);
            m_PinGroup.Pin(inTransform);
            
            Show();
        }

        public void DisplayInspect(ScriptObject inObject, TextId inLabelId, TextId inTooltipId)
        {
            m_Mode = Mode.Inspect;
            m_TargetMap = null;
            m_TargetInspect = inObject;

            m_InteractButtonIcon.sprite = m_InspectIcon;
            m_HoverHint.TooltipId = inTooltipId;
            m_HoverHint.TooltipOverride = null;
            m_InteractLabel.SetText(inLabelId);
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
                        Services.Events.Dispatch(GameEvents.BeginDive, m_TargetMap.name);
                        Routine.Start(FadeRoutine(m_TargetMap));
                        break;
                    }

                case Mode.Map:
                    {
                        Hide();
                        StateUtil.LoadMapWithWipe(m_TargetMap.Id(), null);
                        break;
                    }

                case Mode.Dock:
                    {
                        Hide();
                        StateUtil.LoadSceneWithWipe("Ship", null);
                        break;
                    }

                case Mode.Inspect:
                    {
                        ScriptObject.Inspect(m_TargetInspect);
                        break;
                    }
            }
        }

        #endregion // Handlers

        #region Panel

        protected override void OnHideComplete(bool inbInstant)
        {
            m_TargetMap = null;
            m_TargetInspect = null;

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

        static private IEnumerator FadeRoutine(MapDesc inMap)
        {
            Services.UI.ShowLetterbox();
            Services.Events.Dispatch(Event_Dive, inMap);
            yield return 2;
            StateUtil.LoadMapWithWipe(inMap.Id(), Services.Data.Profile.Map.CurrentStationId());
            yield return 0.3f;
            Services.UI.HideLetterbox();
        }
    }
}
