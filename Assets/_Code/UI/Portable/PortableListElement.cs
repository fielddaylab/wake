using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using BeauPools;
using System;
using Aqua;

namespace Aqua.Portable
{
    public class PortableListElement : MonoBehaviour, IPoolAllocHandler
    {
        [Serializable] public class Pool : SerializablePool<PortableListElement> { }

        #region Inspector

        [SerializeField, Required] private Toggle m_Toggle = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField] private LocText m_Text = null;
        [SerializeField] private CursorInteractionHint m_Cursor = null;

        #endregion // Inspector

        public object Data { get; private set; }

        private Action<PortableListElement, bool> m_Callback;

        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        public void Initialize(Sprite inIcon, ToggleGroup inToggleGroup, StringHash32 inLabel, object inData, Action<PortableListElement, bool> inCallback)
        {
            m_Toggle.SetIsOnWithoutNotify(false);
            m_Toggle.group = inToggleGroup;
            m_Icon.sprite = inIcon;
            Data = inData;
            
            if (m_Text)
                m_Text.SetText(inLabel);
            if (m_Cursor)
                m_Cursor.TooltipId = inLabel;
            
            m_Callback = inCallback;
        }

        private void OnToggleValueChanged(bool inbValue)
        {
            m_Callback?.Invoke(this, inbValue);
        }

        public void SetState(bool inbActive)
        {
            m_Toggle.SetIsOnWithoutNotify(inbActive);
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