using UnityEngine;
using BeauUtil;
using UnityEngine.UI;

namespace Aqua
{
    public class WaterPropertyFactDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_Label = null;
        [SerializeField, Required] private LocText m_Value = null;
        [SerializeField, Required] private Graphic m_Background = null;
        [SerializeField, Required] private Graphic m_Bar = null;

        #endregion // Inspector

        public void Populate(BFWaterProperty inFact)
        {
            var propData = Assets.Property(inFact.Property);
            ColorPalette4 palette = propData.Palette();

            m_Icon.sprite = inFact.Icon;

            m_Label.SetText(propData.LabelId());
            m_Value.SetTextFromString(propData.FormatValue(inFact.Value));

            m_Label.Graphic.color = m_Value.Graphic.color = palette.Content;
            m_Background.color = palette.Shadow;
            m_Bar.color = palette.Background;

            m_Bar.rectTransform.anchorMax = new Vector2(propData.RemapValue(inFact.Value), 1f);
        }
    }
}