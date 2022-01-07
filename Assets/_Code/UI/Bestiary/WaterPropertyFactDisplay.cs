using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using BeauRoutine;
using System;
using TMPro;

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

            string labelFormat = Loc.Format("properties.measurement.format", propData.LabelId());
            m_Label.SetTextFromString(labelFormat);
            m_Value.SetTextFromString(propData.FormatValue(inFact.Value));

            m_Background.color = palette.Background;
            m_Value.Graphic.color = palette.Content;

            float remap = propData.RemapValue(inFact.Value);
            remap = RangeDisplay.AdjustValue(remap, 0.9f);
            m_Bar.rectTransform.SetAnchorX(remap);

            RectTransform valueTransform = m_Value.Graphic.rectTransform;
            TMP_Text valueText = m_Value.Graphic;
            if (remap < 0.4f) {
                valueTransform.pivot = new Vector2(0, 0.5f);
                valueTransform.SetAnchorPos(Math.Abs(valueTransform.anchoredPosition.x), Axis.X);
                valueText.alignment = TextAlignmentOptions.Left;
            } else {
                valueTransform.pivot = new Vector2(1, 0.5f);
                valueTransform.SetAnchorPos(-Math.Abs(valueTransform.anchoredPosition.x), Axis.X);
                valueText.alignment = TextAlignmentOptions.Right;
            }
        }
    }
}