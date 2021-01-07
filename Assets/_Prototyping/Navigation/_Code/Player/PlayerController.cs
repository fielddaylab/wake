using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using Aqua;


namespace ProtoAqua.Navigation {
    
    public class PlayerController : MonoBehaviour {

        [SerializeField] Rigidbody2D playerRigidBody = null;
        [SerializeField] PlayerInput playerInput = null;
        [SerializeField] PlayerAnimator playerAnimator = null;

        [SerializeField] float minSpeed = 2;
        [SerializeField] float maxSpeed = 10;


        // Start is called before the first frame update
        void Start() {
            //@TODO Ensure this works not sure how to test it currently
            if(Services.Data.Profile.Map.getPlayerTransform() != null) {
                this.transform.position = Services.Data.Profile.Map.getPlayerTransform().position;
            }
           Services.Data.Profile.Map.setPlayerTransform(this.transform);
        }

        // Update is called once per frame
        void FixedUpdate() {
            MovePlayer();
            RotatePlayer();
            Debug.Log(Services.Data.Profile.Map.getPlayerTransform().position.x);
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

