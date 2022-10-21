using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aqua.Cameras;
using Aqua;

public class CameraShake : MonoBehaviour
{
    public Vector2 distance;
    public Vector2 period;
    public float duration;

    public void shake()
    {
        Services.Camera.AddShake(distance, period, duration);
    }

  
}
