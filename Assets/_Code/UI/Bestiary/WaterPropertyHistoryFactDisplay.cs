using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;

namespace Aqua
{
    public class WaterPropertyHistoryFactDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_Label = null;
        [SerializeField, Required] private Graphic m_Background = null;
        [SerializeField, Required] private Image m_GraphIcon = null;
        [SerializeField, Required] private LocText m_GraphLabel = null;

        #endregion // Inspector

        public void Populate(BFWaterPropertyHistory inFact)
        {
            var propData = Assets.Property(inFact.Property);
            ColorPalette4 palette = propData.Palette();

            m_Icon.sprite = inFact.Icon;

            m_Label.SetText(propData.LabelId());

            m_Label.Graphic.color = m_GraphLabel.Graphic.color = palette.Content;
            m_Background.color = palette.Shadow;

            m_GraphIcon.sprite = Services.Assets.Bestiary.GraphTypeToImage(inFact.Graph);
            m_GraphLabel.SetText(Services.Assets.Bestiary.GraphTypeToLabel(inFact.Graph));
        }
    }
}