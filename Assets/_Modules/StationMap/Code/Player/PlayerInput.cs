using System;
using Aqua.Character;
using BeauUtil;
using UnityEngine;

namespace Aqua.StationMap
{
    public class PlayerInput : WorldInput
    {
        public struct Input
        {
            public MouseDistanceInputFilter.Output Mouse;
            public DirectionKeysInputFilter.Output Keyboard;

            public bool Move;
            public Vector2 MovementVector;
        }

        #region Inspector

        [SerializeField] private MouseDistanceInputFilter m_Movement = default;
        [SerializeField] private DirectionKeysInputFilter m_MovementKey = default;

        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;

        protected override void Awake()
        {
            this.CacheComponent(ref m_Transform);
        }

        public void GenerateInput(out Input outInput)
        {
            m_Movement.Process(Device, m_Transform, null, out outInput.Mouse);
            m_MovementKey.Process(Device, out outInput.Keyboard);
            outInput.Move = (Device.MouseDown(0) && !Services.Input.IsPointerOverUI()) || outInput.Keyboard.KeyDown;
            outInput.MovementVector = outInput.Keyboard.KeyDown ? outInput.Keyboard.NormalizedOffset : outInput.Mouse.NormalizedOffset;
        }
    }
}
