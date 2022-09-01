using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAnimationDelay : MonoBehaviour
{

    public float delayMin;
    public float delayMax;
    public Animator animator;
    public string idleExitParam;
    public string cycleOffsetParam;
    public string speedParam;
    public float speedMax;
    public float speedMin;


    private void Awake()
    {
        animator.SetFloat(cycleOffsetParam, Random.Range(0.0f, 1.0f));
        animator.SetFloat(speedParam, Random.Range(speedMin, speedMax));
        StartCoroutine(DelayAnimation());

    }


    IEnumerator DelayAnimation()
    {
        yield return new WaitForSeconds(Random.Range(delayMin, delayMax));
        animator.SetTrigger(idleExitParam);
    }


}
