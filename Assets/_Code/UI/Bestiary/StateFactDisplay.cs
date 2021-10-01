using UnityEngine;
using BeauUtil;
using UnityEngine.UI;

namespace Aqua
{
    public class StateFactDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_Label = null;
        [SerializeField, Required] private Graphic m_KillBackground = null;
        [SerializeField, Required] private RangeDisplay m_StressRange = null;
        [SerializeField, Required] private RangeDisplay m_AliveRange = null;

        #endregion // Inspector

        public void Populate(BFState inFact)
        {
            var propData = Assets.Property(inFact.Property);
            m_Icon.sprite = inFact.Icon;

            m_Label.SetText(propData.LabelId());

            ActorStateTransitionRange range = inFact.Range;

            if (inFact.HasDeath)
            {
                m_KillBackground.gameObject.SetActive(true);
                m_StressRange.Display(range.StressedMin, range.StressedMax, propData.MinValue(), propData.MaxValue());
            }
            else
            {
                m_KillBackground.gameObject.SetActive(false);
                m_StressRange.Display(0, 1, 0, 1);
            }

            m_AliveRange.Display(range.AliveMin, range.AliveMax, propData.MinValue(), propData.MaxValue());
        }
    }
}