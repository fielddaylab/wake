using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;

namespace Aqua
{
    public class PopulationHistoryFactDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private LocText m_Label = null;
        [SerializeField, Required] private TMP_Text m_Population = null;

        #endregion // Inspector

        public void Populate(BFPopulation inFact)
        {
            m_Icon.sprite = inFact.Icon();
            m_Label.SetText(inFact.Critter().CommonName());
            m_Population.SetText(inFact.FormattedPopulation());
        }
    }
}