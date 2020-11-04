using BeauRoutine.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class SimCompletePanel : BasePanel
    {
        #region Inspector

        [Header("Help")]
        [SerializeField] private TMP_Text m_PartnerQuote = null;
        [SerializeField] private Button m_ContinueButton = null;

        [SerializeField] private string m_DefaultPartnerQuote = null;

        #endregion // Inspector

        protected override void Start()
        {
            base.Start();
            
            m_ContinueButton.onClick.AddListener(OnContinue);
        }

        private void OnContinue()
        {
            Hide();
            Services.Script.TriggerResponse(SimTriggers.ModelSynced);
        }

        public void Display(ScenarioPackageHeader inHeader)
        {
            string partnerQuote = inHeader.PartnerCompleteQuote;
            if (string.IsNullOrEmpty(partnerQuote))
            {
                partnerQuote = m_DefaultPartnerQuote;
            }
            m_PartnerQuote.SetText(partnerQuote);

            Show();
        }
    }
}