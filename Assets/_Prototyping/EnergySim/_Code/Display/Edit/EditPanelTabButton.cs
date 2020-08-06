using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class EditPanelTabButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Toggle m_Toggle = null;
        [SerializeField] private GameObject m_Content = null;

        #endregion // Inspector

        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener(OnValueChanged);
            OnValueChanged(m_Toggle.isOn);
        }

        public void RegisterGroup(ToggleGroup inGroup)
        {
            m_Toggle.group = inGroup;
        }

        private void OnValueChanged(bool inbSelected)
        {
            m_Content.gameObject.SetActive(inbSelected);
        }

        public void Select()
        {
            m_Toggle.isOn = true;
        }

        public void Deselect()
        {
            m_Toggle.isOn = false;
        }
    }
}