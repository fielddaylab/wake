using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProtoAqua.Map {
    
    public class PlayerController : MonoBehaviour {

        [SerializeField] Rigidbody2D playerRigidBody;
        [SerializeField] float moveSpeed = 100f;

    

        private Vector3 mousePosition;
        private Vector2 direction;




        
        // Start is called before the first frame update
        void Start() {

        }


        // Update is called once per frame
        void Update() {
            if(Input.GetMouseButton(0)) {
                mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                direction = (mousePosition - transform.position).normalized;
                playerRigidBody.velocity = new Vector2(direction.x * moveSpeed, direction.y * moveSpeed);
                //vs

            }
            else {
                playerRigidBody.velocity = Vector2.zero;
            }
        }

        
    }
}

