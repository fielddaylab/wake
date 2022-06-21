using UnityEngine;

public class ShipCameraFollow : MonoBehaviour {

    public Transform target;

    public float smoothSpeed = 0.125f;

    public Vector3 camOffset;
    public Vector3 rotationOffset;

    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + camOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed*Time.deltaTime);
        transform.position = smoothedPosition;
        transform.LookAt(target);
        transform.rotation *= Quaternion.Euler(rotationOffset);
    }

}
