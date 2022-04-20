using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecterReveal : MonoBehaviour
{
    public bool scanned;
    public ParticleSystem preScannedParticles;
    public GameObject specterVisuals;

    public void Update()
    {
        if (scanned)
        {
            specterVisuals.SetActive(true);
            preScannedParticles.Stop(true);
        }
    }

}
