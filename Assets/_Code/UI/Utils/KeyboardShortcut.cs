using System;
using BeauPools;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class KeyboardShortcut : MonoBehaviour, IPoolAllocHandler, IInputHandler
    {
        #region Inspector

        [SerializeField] private KeyCode m_KeyCode = KeyCode.Space;
        [SerializeField] private ButtonDisplay m_ButtonDisplay = null;

        #endregion // Inspector

        [NonSerialized] private DeviceInput m_DeviceInput;
        [NonSerialized] private bool m_Registered;

        private void Awake()
        {
            m_ButtonDisplay.Assign(m_KeyCode);
        }

        private void OnEnable()
        {
            m_DeviceInput = m_DeviceInput ?? DeviceInput.Find(this);
            
            if (m_DeviceInput != null && !m_Registered)
            {
                m_DeviceInput.RegisterHandler(this);
                m_Registered = true;
            }
        }

        private void OnDisable()
        {
            if (m_DeviceInput != null && m_Registered)
            {
                m_DeviceInput.DeregisterHandler(this);
                m_Registered = false;
            }
        }

        void IInputHandler.HandleInput(DeviceInput inInput)
        {
            if (inInput.KeyPressed(m_KeyCode))
            {
                if (Services.Input.ExecuteClick(gameObject))
                {
                    m_ButtonDisplay.PlayAnimation();
                }
            }
        }

        #region IPoolAllocHandler

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
            if (m_DeviceInput != null)
            {
                if (m_Registered)
                {
                    m_DeviceInput.DeregisterHandler(this);
                    m_Registered = false;
                }
                m_DeviceInput = null;
            }
        }

        #endregion // IPoolAllocHandler
    }
}