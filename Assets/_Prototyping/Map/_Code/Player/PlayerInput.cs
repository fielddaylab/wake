using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProtoAqua.Map {
    public class PlayerInput : MonoBehaviour {

        [SerializeField] CameraController cameraController = null;
        [SerializeField] Renderer renderer;


        private Vector3 mousePosition;
        private Vector2 direction;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        //Returns the direction the player will move
        //Returns 0 if no input
        public Vector2 getDirection() {
            if(Input.GetMouseButton(0)) {
                    mousePosition = cameraController.ScreenToWorldOnPlane(Input.mousePosition, transform);


                    Vector2 rawDirection = (mousePosition - transform.position);
                    direction = rawDirection.normalized;
                    
                    //Prevent Boat from actually hitting the mouse, and causing glitches
                    if(rawDirection.x < 1.0f && rawDirection.x > -1.0f && rawDirection.y < 1.0f && rawDirection.y > -1.0f) {
                        direction = Vector2.zero;
                    }

            } else {
                    direction = Vector2.zero;
            }
            return direction;
        }

        public float getRotateAngle() {
            return AngleBetweenTwoPoints(transform.position, mousePosition);
        }

        float AngleBetweenTwoPoints(Vector3 a, Vector3 b) {
            return Mathf.Atan2(a.y - b.y, a.x - b.x) * Mathf.Rad2Deg;
        }
    }
}
