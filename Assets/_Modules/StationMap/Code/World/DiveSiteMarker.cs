using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.StationMap {
    public sealed class DiveSiteMarker : MonoBehaviour {
        #region Inspector

        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private RectTransformPinned m_Pin = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private Transform m_EdgeArrow = null;

        [Header("Background")]
        [SerializeField] private Image m_BG = null;
        [SerializeField] private Sprite m_DefaultBGSprite = null;
        [SerializeField] private Sprite m_EdgeBGSprite = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_GroupFade;

        private void Awake() {
            m_EdgeArrow.gameObject.SetActive(false);
            m_Pin.OnClampUpdate.AddListener((t, e) => {
                if (e != 0) {
                    m_EdgeArrow.gameObject.SetActive(true);
                    m_EdgeArrow.SetRotation(GetAngle(e), Axis.Z, Space.Self);
                    m_BG.sprite = m_EdgeBGSprite;
                } else {
                    m_EdgeArrow.gameObject.SetActive(false);
                    m_BG.sprite = m_DefaultBGSprite;
                }
            });
        }

        public void Pin(Transform transform, StringHash32 mapId) {
            m_Pin.Pin(transform);
            m_Pin.OnClampUpdate.Invoke(transform, m_Pin.LastClampedEdges);

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

        static private float GetAngle(RectEdges edges) {
            if ((edges & RectEdges.Bottom) != 0) {
                return 270;
            }
            if ((edges & RectEdges.Right) != 0) {
                return 0;
            }
            if ((edges & RectEdges.Left) != 0) {
                return 180;
            }
            return 90;
        }
    }
}