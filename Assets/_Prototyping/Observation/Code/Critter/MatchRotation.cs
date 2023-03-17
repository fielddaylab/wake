
using UnityEngine;

[DefaultExecutionOrder(600)]
public class MatchRotation : MonoBehaviour
{
    public Transform Source;

    private void LateUpdate() {
        transform.rotation = Source.rotation;
    }
}