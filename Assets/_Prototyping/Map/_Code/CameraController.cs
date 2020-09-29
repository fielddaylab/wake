using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    [SerializeField] Camera mainCamera = null;
    [SerializeField] Transform target;


    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        transform.position = new Vector3(
            Mathf.Clamp(target.position.x, -20f, 20f),
            Mathf.Clamp(target.position.y, -10f, 10f),
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
