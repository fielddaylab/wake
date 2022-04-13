using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceShipMove : MonoBehaviour
{

    public float shipSpeed = 1.0f;
    public float shipTurnSpeed = 1.0f;
    Vector3 newPosition;

    void Start()
    {
        newPosition = transform.position;
    }

    void Update()
    {
        float step = shipSpeed * Time.deltaTime;

        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                newPosition = hit.point;
                transform.position = Vector3.MoveTowards(transform.position, newPosition, step);
                //transform.LookAt(newPosition);
                Quaternion OriginalRot = transform.rotation;
                transform.LookAt(newPosition);
                Quaternion NewRot = transform.rotation;
                transform.rotation = OriginalRot;
                transform.rotation = Quaternion.Lerp(transform.rotation, NewRot, shipTurnSpeed * Time.deltaTime);

            }
        }

    }
}
