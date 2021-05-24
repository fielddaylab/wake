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
        [SerializeField, Required] private RangeDisplay m_StressRange = null;
        [SerializeField, Required] private RangeDisplay m_AliveRange = null;

        #endregion // Inspector

        private BFBase m_Fact;
        private Action<BFBase> m_Callback;

        public void Initialize(BFState inFact, bool inbButtonMode, bool inbInteractable, Action<BFBase> inCallback)
        {
            var propData = Services.Assets.WaterProp.Property(inFact.PropertyId());

            Sprite spr = inFact.Icon();
            if (!spr)
                spr = propData.Icon();

            m_Icon.sprite = spr;
            m_Icon.gameObject.SetActive(spr);

            ActorStateTransitionRange range = inFact.Range();

            m_StressRange.Display(range.StressedMin, range.StressedMax, propData.MinValue(), propData.MaxValue());
            m_AliveRange.Display(range.AliveMin, range.AliveMax, propData.MinValue(), propData.MaxValue());

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
            
            m_Fact = null;
            m_Callback = null;
        }
    }
}