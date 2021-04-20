using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public class DeviceInput
    {
        static private bool s_Reset;

        static public void BlockAll()
        {
            s_Reset = true;
        }

        static public void ClearBlock()
        {
            s_Reset = false;
        }

        public IInputLayer Layer { get; private set; }
        public bool Enabled { get; set; }

        public event Action<DeviceInput> OnUpdate;
        private BufferedCollection<IInputHandler> m_UpdateHandlers;

        public DeviceInput()
        {
            Layer = null;
            Enabled = true;
        }

        public DeviceInput(IInputLayer inLayer)
        {
            Layer = inLayer;
            Enabled = true;
        }

        public bool IsActive()
        {
            return !s_Reset && Enabled && (Layer == null || Layer.IsInputEnabled);
        }

        public void RegisterHandler(IInputHandler inHandler)
        {
            if (m_UpdateHandlers == null)
                m_UpdateHandlers = new BufferedCollection<IInputHandler>();
            Assert.False(m_UpdateHandlers.Contains(inHandler), "Cannot register same handler '{0}' twice!", inHandler);
            m_UpdateHandlers.Add(inHandler);
        }

        public void DeregisterHandler(IInputHandler inHandler)
        {
            if (m_UpdateHandlers != null)
            {
                Assert.True(m_UpdateHandlers.Contains(inHandler), "Cannot deregister handler '{0}' more than once!", inHandler);
                m_UpdateHandlers.Remove(inHandler);
            }
        }

        public void Update()
        {
            if (IsActive())
            {
                if (m_UpdateHandlers != null)
                {
                    m_UpdateHandlers.ForEach(HandleInputPtr, this);
                }
                OnUpdate?.Invoke(this);
            }
        }

        static private readonly Action<IInputHandler, DeviceInput> HandleInputPtr = HandleInput;
        static private void HandleInput(IInputHandler inHandler, DeviceInput inInput)
        {
            inHandler.HandleInput(inInput);
        }

        public bool KeyDown(KeyCode inKeyCode)
        {
            return IsActive() && Input.GetKey(inKeyCode);
        }

        public bool KeyPressed(KeyCode inKeyCode)
        {
            return IsActive() && Input.GetKeyDown(inKeyCode);
        }

        public bool KeyReleased(KeyCode inKeyCode)
        {
            return IsActive() && Input.GetKeyUp(inKeyCode);
        }

        public bool MouseDown(int inMouseButton)
        {
            return IsActive() && Input.GetMouseButton(inMouseButton);
        }

        public bool MousePressed(int inMouseButton)
        {
            return IsActive() && Input.GetMouseButtonDown(inMouseButton);
        }

        public bool MouseReleased(int inMouseButton)
        {
            return IsActive() && Input.GetMouseButtonUp(inMouseButton);
        }

        static public DeviceInput Find(Component inComponent)
        {
            return BaseInputLayer.Find(inComponent)?.Device;
        }
    }
}