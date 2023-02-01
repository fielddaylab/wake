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

        public void GenerateInput(Transform inPlayerPosition, PlayerBodyStatus inStatus, out Input outInput)
        {
            if (!IsInputEnabled) {
                outInput = default(Input);
                return;
            }

            Plane p = new Plane(Vector3.back, inPlayerPosition.position);
            m_Movement.Process(Device, m_Transform, null, p, out outInput.Mouse);
            m_MovementKey.Process(Device, out outInput.Keyboard);

            if ((inStatus & PlayerBodyStatus.DisableMovement) == 0) {
                outInput.Move = (Device.MouseDown(0) && !Services.Input.IsPointerOverUI()) || outInput.Keyboard.KeyDown;
                outInput.MovementVector = outInput.Keyboard.KeyDown ? outInput.Keyboard.NormalizedOffset : outInput.Mouse.NormalizedOffset;
            } else {
                outInput.Move = false;
                outInput.MovementVector = default(Vector2);
            }
        }
    }
}
