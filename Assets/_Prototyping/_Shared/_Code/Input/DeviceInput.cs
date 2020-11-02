using System;
using UnityEngine;

namespace ProtoAqua
{
    public class DeviceInput
    {
        public IInputLayer Layer { get; private set; }
        public bool Enabled { get; set; }

        public event Action<DeviceInput> OnUpdate;

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
            return Enabled && (Layer == null || Layer.IsInputEnabled);
        }

        public void Update()
        {
            if (IsActive() && OnUpdate != null)
                OnUpdate(this);
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
    }
}