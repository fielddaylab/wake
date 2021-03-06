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

        private void Awake()
        {
            m_DeviceInput = BaseInputLayer.Find(this).Device;
            m_ButtonDisplay.Assign(m_KeyCode);
        }

        private void OnEnable()
        {
            m_DeviceInput = m_DeviceInput ?? BaseInputLayer.Find(this).Device;
            m_DeviceInput.RegisterHandler(this);
        }

        private void OnDisable()
        {
            if (m_DeviceInput != null)
            {
                m_DeviceInput.DeregisterHandler(this);
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
                m_DeviceInput.DeregisterHandler(this);
                m_DeviceInput = null;
            }
        }

        #endregion // IPoolAllocHandler
    }
}