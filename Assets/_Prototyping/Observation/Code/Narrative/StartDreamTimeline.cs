using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class StartDreamTimeline : MonoBehaviour
{
    public CircleCollider2D clickTriggerCollider;
    public GameObject scanIcon;
    public PlayableDirector playableDirector;
    public Animator scannableAnimator;
    private bool scanComplete = false;

    private void OnMouseDown()
    {
        scannableAnimator.SetBool("scanning", true);
    }

    private void OnMouseUp()
    {
        if (scanComplete == false)
        {
            scannableAnimator.SetBool("scanning", false);
        }
    }

    private void PlayTimeline()
    {
        playableDirector.Play();
    }

    private void CompleteScan()
    {
        scanComplete = true;
        clickTriggerCollider.enabled = false;
        scanIcon.SetActive(false);
    }
}
