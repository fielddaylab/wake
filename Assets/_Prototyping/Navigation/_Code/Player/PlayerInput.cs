using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProtoAqua.Navigation {
    public class PlayerInput : MonoBehaviour {

        [SerializeField] CameraController cameraController = null;
        [SerializeField] new Renderer renderer;


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
        public Vector2 GetDirection() {
            if(Input.GetMouseButton(0)) {
                    mousePosition = cameraController.ScreenToWorldOnPlane(Input.mousePosition, transform);


                    Vector2 rawDirection = (mousePosition - transform.position);
                    direction = rawDirection.normalized;
                    //Prevent Boat from actually hitting the mouse, and causing glitches
                    if(rawDirection.magnitude < 1.2f) {
                        direction = Vector2.zero;
                    }

            } else {
                    //Gradually slow down
                    if(direction.magnitude > .01f) {
                        direction = direction * .985f; 
                    } else {
                        direction = Vector2.zero;
                    }
            }
            return direction;
        }

        //TODO perform better math on this
        public float GetSpeed(float minSpeed, float maxSpeed) {
            float speed = 0;
            if(Input.GetMouseButton(0)) {
                 Vector2 rawDirection = (mousePosition - transform.position);
                 speed = Mathf.Clamp(rawDirection.magnitude, minSpeed, maxSpeed);
            }
            
            return speed;
        }

        public float GetRotateAngle() {
             if(Input.GetMouseButton(0)) {
                return AngleBetweenTwoPoints(transform.position, mousePosition);
             } else {
                 return 0;
             }
        }

        float AngleBetweenTwoPoints(Vector3 a, Vector3 b) {
            return Mathf.Atan2(a.y - b.y, a.x - b.x) * Mathf.Rad2Deg;
        }
    }
}
