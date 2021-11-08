using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;

namespace Aqua
{
    public class PopulationHistoryFactDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Graphic m_IconBG = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_Label = null;
        [SerializeField, Required] private Image m_GraphIcon = null;
        [SerializeField, Required] private LocText m_GraphLabel = null;

        #endregion // Inspector

        public void Populate(BFPopulationHistory inFact)
        {
            m_IconBG.color = inFact.Critter.Color();
            m_Icon.sprite = inFact.Icon;
            m_Label.SetText(inFact.Critter.CommonName());
            m_GraphIcon.sprite = Services.Assets.Bestiary.GraphTypeToImage(inFact.Graph);
            m_GraphLabel.SetText(Services.Assets.Bestiary.GraphTypeToLabel(inFact.Graph));
        }
    }
}