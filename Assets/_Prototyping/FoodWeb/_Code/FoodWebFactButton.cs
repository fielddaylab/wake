using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using Aqua;
using Aqua.Portable;

namespace ProtoAqua.Foodweb
{
    public class FoodWebFactButton : BestiaryFactButton
    {
        #region Inspector

        [SerializeField, Required] private Button m_Button = null;
        [SerializeField, Required] private RectTransform m_ButtonTail = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private FactSentenceDisplay m_Sentence = null;

        #endregion // InspectorF
        public void InitializeFW(BFBase inFact, PlayerFactParams inParams, bool inbButtonMode)
        {
            m_Icon.sprite = inFact.Icon();
            m_Icon.gameObject.SetActive(inFact.Icon());
            m_Sentence.Populate(inFact, inParams);

            m_Button.targetGraphic.raycastTarget = inbButtonMode;
            m_ButtonTail.gameObject.SetActive(inbButtonMode);
            m_Button.onClick.AddListener(() => TargetOnClick());
        }
        public void TargetOnClick() {

        }

        public void OnFree()
        {
           m_Button.onClick.RemoveAllListeners();
        }
    }
}

