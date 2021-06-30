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
            public bool Move;
        }

        #region Inspector

        [SerializeField] private MouseDistanceInputFilter m_Movement = default;

        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;

        protected override void Awake()
        {
            this.CacheComponent(ref m_Transform);
        }

        public void GenerateInput(out Input outInput)
        {
            m_Movement.Process(Device, m_Transform, null, out outInput.Mouse);
            outInput.Move = Device.MouseDown(0) && !Services.Input.IsPointerOverUI();
        }
    }
}
