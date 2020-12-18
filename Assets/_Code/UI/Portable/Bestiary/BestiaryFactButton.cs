using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using Aqua;
using System;
using BeauUtil.Debugger;

namespace Aqua.Portable
{
    public class BestiaryFactButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Button m_Button = null;
        [SerializeField, Required] private RectTransform m_ButtonTail = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private FactSentenceDisplay m_Sentence = null;

        #endregion // Inspector

        private PlayerFactParams m_Params;
        private Action<PlayerFactParams> m_Callback;

        public void Initialize(BestiaryFactBase inFact, PlayerFactParams inParams, bool inbButtonMode, bool inbInteractable, Action<PlayerFactParams> inCallback)
        {
            m_Icon.sprite = inFact.Icon();
            m_Icon.gameObject.SetActive(inFact.Icon());
            m_Sentence.Populate(inFact, inParams);

            m_Button.targetGraphic.raycastTarget = inbButtonMode;
            m_Button.interactable = inbInteractable;
            m_ButtonTail.gameObject.SetActive(inbButtonMode);

            m_Params = inParams ?? new PlayerFactParams(inFact.Id());
            m_Callback = inCallback;
        }

        private void OnClick()
        {
            Assert.NotNull(m_Callback);
            Assert.NotNull(m_Params);

            m_Callback(m_Params);
        }

        private void Awake()
        {
            m_Button.onClick.AddListener(OnClick);
        }

        private void OnDispose()
        {
            m_Params = null;
            m_Callback = null;
        }
    }
}