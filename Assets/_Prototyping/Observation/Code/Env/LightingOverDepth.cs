using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingOverDepth : MonoBehaviour
{
    Camera mainCamera;
    float cameraPos;
    public Light mainLight;
    float brightestLightIntensity;
    public float darkestLightIntensity;
    public float darkeningStart = 0;
    public float darkeningEnd = -20;


    void Start()
    {
        mainCamera = Camera.main;
        if (mainLight != null)
        {
            brightestLightIntensity = mainLight.intensity;
            Debug.Log("Darkest Light Intensity:" + darkestLightIntensity);
        }
    }


    void Update()
    {
        cameraPos = mainCamera.transform.position.y;

        if (cameraPos > darkeningStart)
        {
            mainLight.intensity = brightestLightIntensity;
        }
        else if (cameraPos > darkeningEnd)
        {
            float camPercent = Mathf.InverseLerp(darkeningEnd, darkeningStart, cameraPos);
            mainLight.intensity = Mathf.Lerp(darkestLightIntensity, brightestLightIntensity, camPercent);
        }

    }
}
