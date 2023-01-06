using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using BeauPools;
using BeauRoutine;

namespace Aqua
{
    public class StateFactDisplay : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [Header("Info")]
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_Label = null;

        [Header("Backgrounds")]
        [SerializeField, Required] private Graphic m_KillBackground = null;
        [SerializeField, Required] private Graphic m_StressBackground = null;
        [SerializeField, Required] private Graphic m_AliveBackground = null;

        [Header("Ranges")]
        [SerializeField, Required] private RangeDisplay m_StressRange = null;
        [SerializeField, Required] private RangeDisplay m_AliveRange = null;
        [SerializeField] private RectTransform m_EnvironmentValueMarker = null;

        #endregion // Inspector

        private WaterPropertyId m_CachedPropertyId;

        public void Populate(BFState inFact, BFDiscoveredFlags inFlags, BestiaryDesc inEnvironment = null)
        {
            m_CachedPropertyId = inFact.Property;

            var propData = Assets.Property(inFact.Property);
            var palette = propData.Palette();

            m_StressBackground.color = palette.Shadow;
            m_AliveBackground.color = palette.Background;

            m_Icon.sprite = inFact.Icon;

            string labelFormat = Loc.Format("properties.tolerance.format", propData.LabelId());
            if ((inFlags & BFDiscoveredFlags.IsEncrypted) != 0) {
                m_Label.SetTextNoParse(Formatting.Scramble(labelFormat));
            } else {
                m_Label.SetTextFromString(labelFormat);
            }

            ActorStateTransitionRange range = inFact.Range;

            if ((inFlags & BFDiscoveredFlags.IsEncrypted) != 0) {
                uint randSeed = inFact.Id.HashValue;
                RandomizeRange(ref randSeed, ref range.AliveMin, ref range.AliveMax);
                RandomizeRange(ref randSeed, ref range.StressedMin, ref range.StressedMax);

                if (range.AliveMax < range.AliveMin) {
                    Ref.Swap(ref range.AliveMin, ref range.AliveMax);
                }

                if (range.StressedMax < range.StressedMin) {
                    Ref.Swap(ref range.StressedMin, ref range.StressedMax);
                }
            }

            if (inFact.HasDeath)
            {
                m_KillBackground.gameObject.SetActive(true);
                m_KillBackground.color = ((Color) palette.Shadow * 0.8f).WithAlpha(1);
                m_StressRange.Display(range.StressedMin, range.StressedMax, propData.MinValue(), propData.MaxValue(), !inFact.HasStressed);
            }
            else
            {
                m_KillBackground.gameObject.SetActive(false);
                m_StressRange.Display(0, 1, 0, 1, true);
            }

            m_AliveRange.Display(range.AliveMin, range.AliveMax, propData.MinValue(), propData.MaxValue(), !inFact.HasStressed);

            ((RectTransform) transform).SetSizeDelta(56, Axis.Y);

            SetEnvironment(inEnvironment);
        }

        public void SetEnvironment(BestiaryDesc inEnvironment = null) {
            if (!m_EnvironmentValueMarker || m_CachedPropertyId == WaterPropertyId.COUNT) {
                return;
            }

            if (!inEnvironment) {
                m_EnvironmentValueMarker.gameObject.SetActive(false);
            } else {
                var propData = Assets.Property(m_CachedPropertyId);
                float value = inEnvironment.GetEnvironment()[m_CachedPropertyId];
                float anchorX = propData.RemapValue(value);
                anchorX = m_AliveRange.AdjustValue(anchorX);

                Vector2 anchorMin = m_EnvironmentValueMarker.anchorMin,
                    anchorMax = m_EnvironmentValueMarker.anchorMax;

                anchorMin.x = anchorMax.x = anchorX;

                m_EnvironmentValueMarker.anchorMin = anchorMin;
                m_EnvironmentValueMarker.anchorMax = anchorMax;

                m_EnvironmentValueMarker.gameObject.SetActive(true);
            }
        }

        static private void RandomizeRange(ref uint seed, ref float min, ref float max) {
            float avg = (max + min) / 2;
            float dMin = min - avg;
            float dMax = max - avg;

            dMin *= Formatting.PseudoRandom(ref seed, 0.5f, 2f);
            dMax *= Formatting.PseudoRandom(ref seed, 0.5f, 2f);

            min = avg + dMin;
            max = avg + dMax;
        }

        void IPoolAllocHandler.OnAlloc() { }

        void IPoolAllocHandler.OnFree() {
            m_CachedPropertyId = WaterPropertyId.COUNT;
            if (m_EnvironmentValueMarker) {
                m_EnvironmentValueMarker.gameObject.SetActive(false);
            }
        }
    }
}