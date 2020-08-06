using System.Collections;
using BeauRoutine;
using BeauRoutine.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class SimIntroPanel : BasePanel
    {
        #region Inspector

        [Header("Scenario")]

        [SerializeField] private TMP_Text m_ScenarioName = null;
        [SerializeField] private TMP_Text m_AuthorName = null;
        [SerializeField] private TMP_Text m_ScenarioDescription = null;

        [SerializeField] private TMP_Text m_PartnerQuote = null;
        [SerializeField] private Button m_ContinueButton = null;

        [SerializeField] private string m_DefaultPartnerQuote = null;

        #endregion // Inspector

        protected override void Start()
        {
            base.Start();

            m_ContinueButton.onClick.AddListener(() => Hide());
        }

        public void Display(ScenarioPackageHeader inHeader, bool inbInstant)
        {
            m_ScenarioName.SetText(inHeader.Name);
            m_AuthorName.SetText(inHeader.Author);
            m_ScenarioDescription.SetText(inHeader.Description);

            string partnerQuote = inHeader.PartnerIntroQuote;
            if (string.IsNullOrEmpty(partnerQuote))
            {
                partnerQuote = m_DefaultPartnerQuote;
            }
            m_PartnerQuote.SetText(partnerQuote);

            if (inbInstant)
            {
                InstantShow();
            }
            else
            {
                Show();
            }
        }

        protected override IEnumerator TransitionToHide()
        {
            return null;
        }

        protected override IEnumerator TransitionToShow()
        {
            return null;
        }
    }
}