using UnityEngine;
using Aqua;
using UnityEngine.UI;
using TMPro;
using BeauUtil;
using Aqua.Portable;

namespace ProtoAqua.Portable
{
    public class RequestTest : MonoBehaviour
    {
        [SerializeField, Required] private Button m_SelectCritterButton = null;
        [SerializeField, Required] private Button m_SelectEnvironmentButton = null;
        [SerializeField, Required] private Button m_SelectFactButton = null;

        [SerializeField, Required] private TMP_Text m_CritterText = null;
        [SerializeField, Required] private TMP_Text m_EnvironmentText = null;
        [SerializeField, Required] private TMP_Text m_FactText = null;

        private void Awake()
        {
            m_SelectCritterButton.onClick.AddListener(OnSelectCritter);
            m_SelectEnvironmentButton.onClick.AddListener(OnSelectEnvironment);
            m_SelectFactButton.onClick.AddListener(OnSelectFact);
        }

        private void OnSelectCritter()
        {
            var request = new BestiaryApp.SelectBestiaryEntryRequest(BestiaryDescCategory.Critter);
            Services.UI.FindPanel<PortableMenu>().Open(request);
            request.Return.OnComplete( (s) => {
                m_CritterText.SetText("Selected: " + Services.Assets.Bestiary.Get(s).CommonName());
            }).OnFail(() => {
                m_CritterText.SetText("Selected: Nothing");
            });
        }

        private void OnSelectEnvironment()
        {
            var request = new BestiaryApp.SelectBestiaryEntryRequest(BestiaryDescCategory.Environment);
            Services.UI.FindPanel<PortableMenu>().Open(request);
            request.Return.OnComplete( (s) => {
                m_EnvironmentText.SetText("Selected: " + Services.Assets.Bestiary.Get(s).CommonName());
            }).OnFail(() => {
                m_EnvironmentText.SetText("Selected: Nothing");
            });
        }

        private void OnSelectFact()
        {
            var request = new BestiaryApp.SelectFactRequest(BestiaryDescCategory.Critter);
            Services.UI.FindPanel<PortableMenu>().Open(request);
            request.Return.OnComplete( (s) => {
                m_FactText.SetText("Selected: " + s.Fact.GenerateSentence(s));
            }).OnFail(() => {
                m_FactText.SetText("Selected: Nothing");
            });
        }
    }
}