using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using BeauPools;
using System;
using Aqua;

namespace ProtoAqua.Portable
{
    public class PortableListElement : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [SerializeField, Required] private Toggle m_Toggle = null;
        [SerializeField, Required] private ColorGroup m_Background = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField] private LocText m_Text = null;

        #endregion // Inspector

        public object Data { get; private set; }

        private Action<PortableListElement, bool> m_Callback;

        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        public void Initialize(Sprite inIcon, Color inBackgroundColor, ToggleGroup inToggleGroup, string inText, object inData, Action<PortableListElement, bool> inCallback)
        {
            m_Toggle.SetIsOnWithoutNotify(false);
            m_Toggle.group = inToggleGroup;
            m_Background.Color = inBackgroundColor;
            m_Icon.sprite = inIcon;
            Data = inData;
            
            if (m_Text)
                m_Text.SetText(inText);
            
            m_Callback = inCallback;
        }

        private void OnToggleValueChanged(bool inbValue)
        {
            m_Callback?.Invoke(this, inbValue);
        }

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
            m_Toggle.group = null;
            Data = null;
            m_Callback = null;
        }
    }
}