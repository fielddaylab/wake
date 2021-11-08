using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Portable
{
    public class BestiaryFactButton : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [SerializeField, Required] private CanvasGroup m_Group = null;
        [SerializeField, Required] private Button m_Button = null;

        #endregion // Inspector

        public BFBase Fact;
        private Action<BFBase> m_Callback;
        private bool m_ButtonMode;

        public void Initialize(BFBase inFact, bool inbButtonMode, bool inbInteractable, Action<BFBase> inCallback)
        {
            m_Group.blocksRaycasts = inbButtonMode;
            m_Group.alpha = inbButtonMode && !inbInteractable ? 0.5f : 1;

            m_ButtonMode = inbButtonMode;
            m_Button.interactable = !inbButtonMode || inbInteractable;

            Fact = inFact;
            m_Callback = inCallback;
        }

        public void UpdateInteractable(bool inbInteractable) {
            m_Button.interactable = !m_ButtonMode || inbInteractable;
        }

        private void OnClick()
        {
            Assert.NotNull(m_Callback);
            Assert.NotNull(Fact);

            m_Callback(Fact);
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
            Fact = null;
            m_Callback = null;
        }
    }
}