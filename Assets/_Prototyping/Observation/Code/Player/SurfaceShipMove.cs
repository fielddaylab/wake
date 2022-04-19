using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceShipMove : MonoBehaviour
{

    public float shipSpeed = 1.0f;
    public float shipTurnSpeed = 1.0f;
    Vector3 newPosition;
    private bool slowing = false;
    private float distance = 0.0f;

    void Start()
    {
        newPosition = transform.position;
    }

    void Update()
    {
        float step = shipSpeed * Time.deltaTime;

        if (Input.GetMouseButton(0))
        {
            slowing = false;
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                newPosition = hit.point;
                transform.position = Vector3.MoveTowards(transform.position, newPosition, step);
                Quaternion OriginalRot = transform.rotation;
                transform.LookAt(newPosition);
                Quaternion NewRot = transform.rotation;
                transform.rotation = OriginalRot;
                transform.rotation = Quaternion.Lerp(transform.rotation, NewRot, shipTurnSpeed * Time.deltaTime);

            }

        }

        if (Input.GetMouseButtonUp(0))
        {
            slowing = true;
            distance = Vector3.Distance(transform.position, newPosition);
        }

        if (slowing)
        {

            transform.position = Vector3.Lerp(transform.position, newPosition, shipSpeed * Time.deltaTime / distance);
            Quaternion OriginalRot = transform.rotation;
            transform.LookAt(newPosition);
            Quaternion NewRot = transform.rotation;
            transform.rotation = OriginalRot;
            transform.rotation = Quaternion.Lerp(transform.rotation, NewRot, shipTurnSpeed * Time.deltaTime);
        }

    }
}
