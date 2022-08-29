using UnityEngine;
using BeauUtil.UI;
using BeauUtil;
using UnityEngine.UI;
using BeauRoutine;
using System;

namespace Aqua.StationMap
{
    public sealed class DiveSiteMarker : MonoBehaviour
    {
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private RectTransformPinned m_Pin = null;

        [NonSerialized] private Routine m_GroupFade;

        public void Pin(Transform transform, StringHash32 mapId) {
            m_Pin.Pin(transform);

            MapDesc mapInfo = Assets.Map(mapId);
            TextId label = mapInfo.MarkerLabelId();
            if (!label.IsEmpty) {
                m_Label.SetText(label);
            }
        }

        public void FadeOut() {
            m_GroupFade.Replace(this, m_Group.FadeTo(0, 0.2f));
        }

        public void FadeIn() {
            m_GroupFade.Replace(this, m_Group.FadeTo(1, 0.2f));
        }
    }
}