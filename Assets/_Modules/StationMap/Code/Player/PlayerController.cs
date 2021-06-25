using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using Aqua;

namespace Aqua.StationMap
{    
    public class PlayerController : MonoBehaviour
    {
        #region Inspector

        [SerializeField] KinematicObject2D playerRigidBody = null;
        [SerializeField] PlayerInput playerInput = null;
        [SerializeField] PlayerAnimator playerAnimator = null;

        [SerializeField] private ParticleSystem m_MovementParticles = null;
        
        [Header("Movement Params")]

        [SerializeField] private float m_TargetVectorMaxDistance = 5;
        [SerializeField] private float m_TargetVectorMinDistance = 0.2f;
        [SerializeField] private float m_TargetVectorSpeed = 1;
        [SerializeField] private float m_DragEngineOn = 1;
        [SerializeField] private float m_DragEngineOff = 2;

        #endregion // Inspector

        void FixedUpdate()
        {
            MovePlayer();
            RotatePlayer();
        }  
 
        private void MovePlayer()
        {
            Vector2 direction = playerInput.GetDirection();
            float currentSpeed = playerInput.GetSpeed(0, playerRigidBody.Config.MaxSpeed);

            Vector2 newVelocity = new Vector2(direction.x * currentSpeed, direction.y * currentSpeed);
            Vector2 currentVelocity = playerRigidBody.State.Velocity;
            
            if (newVelocity.sqrMagnitude > currentVelocity.sqrMagnitude)
                playerRigidBody.State.Velocity += (newVelocity - currentVelocity) * Time.fixedDeltaTime * 5;
        }

        private void RotatePlayer()
        {
            float angle = playerInput.GetRotateAngle();
            if(angle != 0) {
                transform.rotation =  Quaternion.Euler (new Vector3(0f,0f,angle));
            }
        }

        public void Teleport(Vector2 inPosition)
        {
            transform.SetPosition(inPosition, Axis.XY);
            m_MovementParticles.Stop();
            m_MovementParticles.Play();
        }

    }
}

