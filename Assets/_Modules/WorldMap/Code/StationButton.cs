using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Aqua.WorldMap
{
    public class StationButton : MonoBehaviour, IKeyValuePair<StringHash32, StationButton>
    {
        #region Inspector

        [SerializeField, PrefabModeOnly] private PointerListener m_Input = null;
        [SerializeField, PrefabModeOnly] private CursorInteractionHint m_CursorHint = null;
        
        [Header("Display")]
        [SerializeField, PrefabModeOnly] private GameObject m_Underline = null;
        [SerializeField, PrefabModeOnly] private GameObject m_CurrentJobIcon = null;
        [SerializeField, PrefabModeOnly] private SpriteRenderer m_CurrentJobPulse = null;
        [SerializeField, PrefabModeOnly] private GameObject m_NotVisitedIcon = null;
        [SerializeField, PrefabModeOnly] private SpriteRenderer m_NotVisitedPulse = null;
        [SerializeField, PrefabModeOnly] private LocText m_Label = null;
        [SerializeField, PrefabModeOnly] private PolygonCollider2DRenderer m_Region = null;
        [SerializeField, PrefabModeOnly] private ColorGroup m_RegionColor = null;
        
        [Header("Data")]
        [SerializeField, MapId(MapCategory.Station)] private SerializedHash32 m_StationId = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Selected;
        [NonSerialized] private bool m_InputEventsRegistered;
        [NonSerialized] private bool m_AlreadyHere;
        private Routine m_HighlightColorRoutine;
        private Routine m_PulseRoutine;

        #region IKeyValue

        StringHash32 IKeyValuePair<StringHash32, StationButton>.Key { get { return m_StationId; } }

        StationButton IKeyValuePair<StringHash32, StationButton>.Value { get { return this; } }

        #endregion // IKeyValue

        public StringHash32 StationId() { return m_StationId; }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show(MapDesc inMap, bool inbCurrent, bool inbSeen, bool inbHasCurrentJob)
        {
            m_Label.SetText(inMap.ShortLabelId());
            m_CursorHint.TooltipId = inMap.LabelId();

            if (!m_InputEventsRegistered) {
                m_Input.onClick.AddListener(OnClick);
                m_Input.onPointerEnter.AddListener(OnPointerEnter);
                m_Input.onPointerExit.AddListener(OnPointerExit);
                m_InputEventsRegistered = true;
            }

            m_Input.enabled = true;
            m_Region.Collider.enabled = true;

            m_AlreadyHere = inbCurrent;

            m_Underline.SetActive(inbCurrent);

            m_RegionColor.SetAlpha(0);

            m_CurrentJobIcon.SetActive(inbHasCurrentJob);
            m_NotVisitedIcon.SetActive(!inbSeen);

            if (inbHasCurrentJob) {
                m_PulseRoutine.Replace(this, Pulse(m_CurrentJobPulse, 2, 1, 1));
            } else if (!inbSeen) {
                m_PulseRoutine.Replace(this, Pulse(m_NotVisitedPulse, 2, 1, 1));
            }

            gameObject.SetActive(true);
        }

        private void OnClick(PointerEventData unused)
        {
            if (m_Selected) {
                return;
            }

            m_Selected = true;
            m_HighlightColorRoutine.Replace(this, ColorGroupTween(m_RegionColor, GetFillColor(AQColors.HighlightYellow.WithAlpha(0.5f)), 0.2f));
            Services.Events.Dispatch(WorldMapCtrl.Event_RequestChangeStation, this);
        }

        private void OnPointerEnter(PointerEventData _) {
            if (m_Selected) {
                return;
            }

            m_HighlightColorRoutine.Replace(this, ColorGroupTween(m_RegionColor, GetFillColor(AQColors.BrightBlue.WithAlpha(0.3f)), 0.2f));
            Services.Audio.PostEvent("ui_hover");
        }

        private void OnPointerExit(PointerEventData _) {
            if (m_Selected) {
                return;
            }

            m_HighlightColorRoutine.Replace(this, ColorGroupTween(m_RegionColor, GetFillColor(AQColors.BrightBlue.WithAlpha(0)), 0.2f));
        }

        public void CancelSelected() {
            if (!m_Selected) {
                return;
            }

            m_Selected = false;
            m_HighlightColorRoutine.Replace(this, ColorGroupTween(m_RegionColor, GetFillColor(AQColors.HighlightYellow.WithAlpha(0)), 0.2f));
        }

        private Color GetFillColor(Color color) {
            if (m_AlreadyHere) {
                float alpha = color.a;
                color *= 0.5f;
                color.a = alpha;
            }
            return color;
        }

        static private Tween ColorGroupTween(ColorGroup group, Color color, float duration) {
            if (group.GetAlpha() == 0) {
                group.Color = color.WithAlpha(0);
            }
            return Tween.Color(group.Color, color, group.SetColor, duration);
        }

        static private IEnumerator Pulse(SpriteRenderer renderer, float maxScale, float duration, float delay) {
            Transform trans = renderer.transform;
            renderer.enabled = false;
            while(true) {
                yield return delay;
                renderer.enabled = true;
                trans.SetScale(1);
                renderer.SetAlpha(1);
                yield return Routine.Combine(
                    trans.ScaleTo(maxScale, duration).Ease(Curve.QuadOut),
                    renderer.FadeTo(0, duration)
                );
                renderer.enabled = false;
            }
        }
    }
}