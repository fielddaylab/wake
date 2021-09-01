using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using Aqua;
using Aqua.Character;
using BeauUtil;

namespace Aqua.StationMap
{    
    public class PlayerController : PlayerBody
    {
        #region Inspector

        [SerializeField] private PlayerInput m_Input = null;
        // [SerializeField] private PlayerAnimator m_Animator = null;
        [SerializeField] private ParticleSystem m_MovementParticles = null;
        
        [Header("Movement Params")]

        [SerializeField] private MovementFromOffset m_MoveParams = default;
        [SerializeField] private RotationFromOffset m_RotateParams = default;
        [SerializeField] private float m_DragEngineOn = 1;
        [SerializeField] private float m_DragEngineOff = 2;

        #endregion // Inspector

        protected override void Tick(float inDeltaTime)
        {
            PlayerInput.Input input;
            m_Input.GenerateInput(out input);

            if (input.Move && input.Mouse.NormalizedOffset.sqrMagnitude > 0)
            {
                m_MoveParams.Apply(input.Mouse.NormalizedOffset, m_Kinematics, inDeltaTime);
                m_RotateParams.Apply(input.Mouse.NormalizedOffset, m_Transform, inDeltaTime);

                m_Kinematics.Config.Drag = m_DragEngineOn;
            }
            else
            {
                m_Kinematics.Config.Drag = m_DragEngineOff;
            }
        }

        public override void TeleportTo(Vector3 inPosition)
        {
            base.TeleportTo(inPosition);

            m_MovementParticles.Stop();
            m_MovementParticles.Play();
        }
    }
}

