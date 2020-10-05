using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;


namespace ProtoAqua.Map {
    
    public class PlayerController : MonoBehaviour {

        [SerializeField] Rigidbody2D playerRigidBody = null;
        [SerializeField] float moveSpeed = .5f;
        [SerializeField] PlayerInput playerInput = null;
        [SerializeField] Transform boatRenderer = null;

        private Routine bobbingRoutine;
        

        // Start is called before the first frame update
        void Start() {
            
            bobbingRoutine.Replace(this, BobbingRoutine());
            //Routine.StartLoop(this, bobbingRoutine);
        }

        // Update is called once per frame
        void FixedUpdate() {
            movePlayer();
            rotatePlayer();
        }  
 
    
        private void movePlayer() {
            Vector2 direction = playerInput.getDirection();
            playerRigidBody.velocity = new Vector2(direction.x * moveSpeed, direction.y * moveSpeed);
        }

        private void rotatePlayer() {
            float angle = playerInput.getRotateAngle();
            transform.rotation =  Quaternion.Euler (new Vector3(0f,0f,angle));
        }


        private IEnumerator BobbingRoutine() {
            yield return boatRenderer.MoveTo(transform.position + new Vector3(0,0,-10.5f),2f);
            yield return boatRenderer.MoveTo(transform.position + new Vector3(0,0,10.5f),2f);
        }

    }
}

