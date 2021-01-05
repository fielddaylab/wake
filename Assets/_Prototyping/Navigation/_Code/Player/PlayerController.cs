using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;


namespace ProtoAqua.Navigation {
    
    public class PlayerController : MonoBehaviour {

        [SerializeField] Rigidbody2D playerRigidBody = null;
        [SerializeField] PlayerInput playerInput = null;
        [SerializeField] PlayerAnimator playerAnimator = null;

        [SerializeField] float minSpeed = 2;
        [SerializeField] float maxSpeed = 10;



        

        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void FixedUpdate() {
            MovePlayer();
            RotatePlayer();
        }  
 
    
        private void MovePlayer() {
            Vector2 direction = playerInput.GetDirection();
            float currentSpeed = playerInput.GetSpeed(minSpeed, maxSpeed);

            Vector2 newVelocity = new Vector2(direction.x * currentSpeed, direction.y * currentSpeed);
            Vector2 currentVelocity = playerRigidBody.velocity;
            playerRigidBody.AddForce(newVelocity - currentVelocity, ForceMode2D.Force);

            playerAnimator.HandleBobbing(direction);    
        }

        private void RotatePlayer() {
            float angle = playerInput.GetRotateAngle();
            if(angle != 0) {
                transform.rotation =  Quaternion.Euler (new Vector3(0f,0f,angle));
            }
        }


    }
}

