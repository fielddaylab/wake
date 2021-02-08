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
    public class BestiaryRangeFactButton : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [SerializeField, Required] private Button m_Button = null;
        [SerializeField, Required] private RectTransform m_ButtonTail = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private RectTransform m_SafeRangeTransform = null;

        #endregion // Inspector

        private PlayerFactParams m_Params;
        private Action<PlayerFactParams> m_Callback;

        public void Initialize(BFStateRange inFact, PlayerFactParams inParams, bool inbButtonMode, bool inbInteractable, Action<PlayerFactParams> inCallback)
        {
            m_Icon.sprite = inFact.Icon();
            m_Icon.gameObject.SetActive(inFact.Icon());

            //Set Anchors of Safe Range (Assuming 0-100 degree range)
            
            m_SafeRangeTransform.anchorMin = new Vector2((inFact.MinSafe() / 100f), 0f);
            m_SafeRangeTransform.anchorMax = new Vector2((inFact.MaxSafe() / 100f), 1f);


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

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
            
            m_Params = null;
            m_Callback = null;
        }
    }
}