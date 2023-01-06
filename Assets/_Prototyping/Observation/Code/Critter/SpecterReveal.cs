using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;

public class SpecterReveal : MonoBehaviour
{
    public bool scanned;
    public ParticleSystem preScannedParticles;
    public GameObject specterVisuals;

    [FilterBestiaryId(BestiaryDescCategory.Critter)] public StringHash32 specterId;
    public SerializedHash32 journalId;

    public void SetScanned() {
        if (!scanned) {
            scanned = true;
            Swap();
        }
    }

    private void Swap() {
        specterVisuals.SetActive(true);
        preScannedParticles.Stop(true);
        enabled = false;
    }

    private void OnDidApplyAnimationProperties() {
        #if UNITY_EDITOR
        if (!Application.isPlaying)
            return;
        #endif // UNITY_EDITOR
        if (scanned) {
            Swap();
        }
    }

}
