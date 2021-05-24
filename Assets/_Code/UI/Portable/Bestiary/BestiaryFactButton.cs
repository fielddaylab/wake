using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using Aqua;
using System;
using BeauUtil.Debugger;
using BeauPools;

namespace Aqua.Portable
{
    public class BestiaryFactButton : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [SerializeField, Required] private Button m_Button = null;
        [SerializeField, Required] private RectTransform m_ButtonTail = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private FactSentenceDisplay m_Sentence = null;

        #endregion // Inspector

        private BFBase m_Fact;
        private Action<BFBase> m_Callback;

        public void Initialize(BFBehavior  inFact, bool inbButtonMode, bool inbInteractable, Action<BFBase> inCallback)
        {
            m_Icon.sprite = inFact.Icon();
            m_Icon.gameObject.SetActive(inFact.Icon());
            m_Sentence.Populate(inFact);

            m_Button.targetGraphic.raycastTarget = inbButtonMode;
            m_Button.interactable = inbInteractable;
            m_ButtonTail.gameObject.SetActive(inbButtonMode);

            m_Fact = inFact;
            m_Callback = inCallback;
        }

        private void OnClick()
        {
            Assert.NotNull(m_Callback);
            Assert.NotNull(m_Fact);

            m_Callback(m_Fact);
        }

        private void Awake()
        {
            m_Button.onClick.AddListener(OnClick);
        }

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
            m_Sentence.Clear();
            m_Fact = null;
            m_Callback = null;
        }
    }
}