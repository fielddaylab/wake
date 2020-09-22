using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProtoAqua.Map {
    
    public class PlayerController : MonoBehaviour {

        [SerializeField] Rigidbody2D playerRigidBody = null;
        [SerializeField] float moveSpeed = .5f;
        [SerializeField] Camera mainCamera = null;

    

        private Vector3 mousePosition;
        private Vector2 direction;

        


        
        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void FixedUpdate() {


            if(Input.GetMouseButton(0)) {
                mousePosition = ScreenToWorldOnPlane(Input.mousePosition, transform);

                direction = (mousePosition - transform.position).normalized;
                playerRigidBody.velocity = new Vector2(direction.x * moveSpeed, direction.y * moveSpeed);

            }
            else {
                playerRigidBody.velocity = Vector2.zero;
            }

            
        }

        public Vector3 ScreenToWorldOnPlane(Vector2 inScreenPos, Transform inWorldRef) {
            Vector3 screenPos = inScreenPos;
            screenPos.z = 1;

            Plane p = new Plane(-mainCamera.transform.forward, inWorldRef.position);
            Ray r = mainCamera.ScreenPointToRay(screenPos);

            float dist;
            p.Raycast(r, out dist);

            return r.GetPoint(dist);
        }

        
    }
}

