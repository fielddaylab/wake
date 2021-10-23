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

        private BFBase m_Fact;
        private Action<BFBase> m_Callback;

        public void Initialize(BFBase inFact, bool inbButtonMode, bool inbInteractable, Action<BFBase> inCallback)
        {
            m_Group.blocksRaycasts = inbButtonMode;
            m_Group.alpha = inbButtonMode && !inbInteractable ? 0.5f : 1;

            m_Button.interactable = !inbButtonMode || inbInteractable;

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
            m_Fact = null;
            m_Callback = null;
        }
    }
}