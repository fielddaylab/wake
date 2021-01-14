using System.Collections;
using System.Collections.Generic;
using Aqua;
using UnityEngine;

namespace ProtoAqua.Navigation
{
    public class PlayerInput : WorldInput
    {

        [SerializeField] CameraController cameraController = null;


        private Vector3 mousePosition;
        private Vector2 direction;

        //Returns the direction the player will move
        //Returns 0 if no input
        public Vector2 GetDirection()
        {
            if (this.IsInputEnabled && Input.GetMouseButton(0) && !Services.Input.IsPointerOverUI())
            {
                mousePosition = cameraController.ScreenToWorldOnPlane(Input.mousePosition, transform);


                Vector2 rawDirection = (mousePosition - transform.position);
                direction = rawDirection.normalized;
                //Prevent Boat from actually hitting the mouse, and causing glitches
                if (rawDirection.magnitude < 1.2f)
                {
                    direction = Vector2.zero;
                }

            }
            else
            {
                //Gradually slow down
                if (direction.magnitude > .01f)
                {
                    direction = direction * .985f;
                }
                else
                {
                    direction = Vector2.zero;
                }
            }
            return direction;
        }

        //TODO perform better math on this
        public float GetSpeed(float minSpeed, float maxSpeed)
        {
            float speed = 0;
            if (this.IsInputEnabled && Input.GetMouseButton(0) && !Services.Input.IsPointerOverUI())
            {
                Vector2 rawDirection = (mousePosition - transform.position);
                speed = Mathf.Clamp(rawDirection.magnitude, minSpeed, maxSpeed);
            }

            return speed;
        }

        public float GetRotateAngle()
        {
            if (this.IsInputEnabled && Input.GetMouseButton(0) && !Services.Input.IsPointerOverUI())
            {
                return AngleBetweenTwoPoints(transform.position, mousePosition);
            }
            else
            {
                return 0;
            }
        }

        float AngleBetweenTwoPoints(Vector3 a, Vector3 b)
        {
            return Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
        }
    }
}
