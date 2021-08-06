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

        #endregion // Inspector

        public void Populate(BFWaterPropertyHistory inFact)
        {
            var propData = Services.Assets.WaterProp.Property(inFact.PropertyId());
            ColorPalette4 palette = propData.Palette();

            m_Icon.sprite = inFact.Icon();
            m_Icon.gameObject.SetActive(inFact.Icon());

            m_Label.SetText(propData.LabelId());

            m_Label.Graphic.color= palette.Content;
            m_Background.color = palette.Shadow;

            m_GraphIcon.sprite = Services.Assets.Bestiary.GraphTypeToImage(inFact.Graph());
        }
    }
}