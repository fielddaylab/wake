using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauUtil.Debugger;
using UnityEngine;

public class LightingOverDepth : MonoBehaviour {
    public Light mainLight;
    float brightestLightIntensity;
    public float darkestLightIntensity;
    public float darkeningStart = 0;
    public float darkeningEnd = -20;


    void Start() {
        if (mainLight != null) {
            brightestLightIntensity = mainLight.intensity;
            Debug.Log("Darkest Light Intensity:" + darkestLightIntensity);
        }
    }


    void Update() {
        if (Script.IsLoading) {
            return;
        }
        
        float cameraPos = Services.Camera.Position.y;

        if (cameraPos > darkeningStart) {
            mainLight.intensity = brightestLightIntensity;
        } else if (cameraPos > darkeningEnd) {
            float camPercent = Mathf.InverseLerp(darkeningEnd, darkeningStart, cameraPos);
            mainLight.intensity = Mathf.Lerp(darkestLightIntensity, brightestLightIntensity, camPercent);
        } else {
            mainLight.intensity = darkestLightIntensity;
        }
    }
}
