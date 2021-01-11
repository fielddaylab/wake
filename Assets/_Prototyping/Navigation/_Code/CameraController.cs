using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProtoAqua.Navigation {
    public class CameraController : MonoBehaviour {

        [SerializeField] Camera mainCamera = null;
        [SerializeField] Transform target;

        [SerializeField] float maxX = 0;
        [SerializeField] float minX = 0;
        [SerializeField] float maxY = 0;
        [SerializeField] float minY = 0;


        // Start is called before the first frame update
        void Start() {
            
        }

        
        // Update is called once per frame
        void Update() {
            transform.position = new Vector3(
                Mathf.Clamp(target.position.x, minX, maxX),
                Mathf.Clamp(target.position.y, minY, maxY),
                transform.position.z
            );
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
