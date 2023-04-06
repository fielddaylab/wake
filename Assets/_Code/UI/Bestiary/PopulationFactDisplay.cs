using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;

namespace Aqua
{
    public class PopulationFactDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Graphic m_IconBG = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_Label = null;
        [SerializeField, Required] private TMP_Text m_Population = null;

        #endregion // Inspector

        public void Populate(BFPopulation inFact)
        {
            m_IconBG.color = inFact.Critter.Color();
            m_Icon.sprite = inFact.Icon;
            m_Label.SetText(inFact.Critter.CommonName());
            m_Population.SetText(BestiaryUtils.FormatPopulation(inFact.Critter, inFact.Value + inFact.DisplayExtra, "~"));
        }
    }
}