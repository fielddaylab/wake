using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using BeauPools;

namespace Aqua
{
    public class StateFactDisplay : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [Header("Info")]
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_Label = null;

        [Header("Backgrounds")]
        [SerializeField, Required] private Graphic m_IconBackground = null;
        [SerializeField, Required] private Graphic m_KillBackground = null;
        [SerializeField, Required] private Graphic m_StressBackground = null;
        [SerializeField, Required] private Graphic m_AliveBackground = null;

        [Header("Ranges")]
        [SerializeField, Required] private RangeDisplay m_StressRange = null;
        [SerializeField, Required] private RangeDisplay m_AliveRange = null;
        [SerializeField] private RectTransform m_EnvironmentValueMarker = null;

        #endregion // Inspector

        private WaterPropertyId m_CachedPropertyId;

        public void Populate(BFState inFact, BestiaryDesc inEnvironment = null)
        {
            m_CachedPropertyId = inFact.Property;

            var propData = Assets.Property(inFact.Property);
            var palette = propData.Palette();

            m_IconBackground.color = palette.Background;
            m_StressBackground.color = palette.Shadow;
            m_AliveBackground.color = palette.Background;

            m_Icon.sprite = inFact.Icon;

            m_Label.SetText(propData.LabelId());
            m_Label.Graphic.color = palette.Content;

            ActorStateTransitionRange range = inFact.Range;

            if (inFact.HasDeath)
            {
                m_KillBackground.gameObject.SetActive(true);
                m_KillBackground.color = ((Color) palette.Shadow * 0.8f).WithAlpha(1);
                m_StressRange.Display(range.StressedMin, range.StressedMax, propData.MinValue(), propData.MaxValue());
            }
            else
            {
                m_KillBackground.gameObject.SetActive(false);
                m_StressRange.Display(0, 1, 0, 1);
            }

            m_AliveRange.Display(range.AliveMin, range.AliveMax, propData.MinValue(), propData.MaxValue());

            SetEnvironment(inEnvironment);
        }

        public void SetEnvironment(BestiaryDesc inEnvironment = null) {
            if (!m_EnvironmentValueMarker || m_CachedPropertyId == WaterPropertyId.MAX) {
                return;
            }

            if (!inEnvironment) {
                m_EnvironmentValueMarker.gameObject.SetActive(false);
            } else {
                var propData = Assets.Property(m_CachedPropertyId);
                float value = inEnvironment.GetEnvironment()[m_CachedPropertyId];
                float anchorX = propData.RemapValue(value);

                Vector2 anchorMin = m_EnvironmentValueMarker.anchorMin,
                    anchorMax = m_EnvironmentValueMarker.anchorMax;

                anchorMin.x = anchorMax.x = anchorX;

                m_EnvironmentValueMarker.anchorMin = anchorMin;
                m_EnvironmentValueMarker.anchorMax = anchorMax;

                m_EnvironmentValueMarker.gameObject.SetActive(true);
            }
        }

        void IPoolAllocHandler.OnAlloc() { }

        void IPoolAllocHandler.OnFree() {
            m_CachedPropertyId = WaterPropertyId.MAX;
            if (m_EnvironmentValueMarker) {
                m_EnvironmentValueMarker.gameObject.SetActive(false);
            }
        }
    }
}