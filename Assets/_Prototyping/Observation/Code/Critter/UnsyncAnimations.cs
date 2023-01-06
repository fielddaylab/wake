using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnsyncAnimations : MonoBehaviour
{
    public Animator animator;
    public string animationLayer = "Base Layer";
    public string animationState = "Idle";

    void Start()
    {
        float startTime = Random.Range(0.0f, 1.0f);
        if (animator)
        {
            animator.Play(animationLayer + "." + animationState, 0, startTime);
        }
    }


}