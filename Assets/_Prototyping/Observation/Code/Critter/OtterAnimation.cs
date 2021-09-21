using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtterAnimation : MonoBehaviour
{
    public float startDelay;
    public int idleRepeat = 1;
    public Animator animator;
    private int idleCount = 0;

    void OnEnable()
    {
        //animator = GetComponent<Animator>();
        StartCoroutine(DelayedAnimation());
    }

    // The delay coroutine
    IEnumerator DelayedAnimation()
    {
        yield return new WaitForSeconds(startDelay);
        animator.Play("SwimTo");
    }

    public void idlePlayed()
    {
        idleCount = idleCount + 1;
        Debug.Log("IdleCount:" + idleCount);
    }

    private void Update()
    {
        if (idleRepeat <= idleCount)
        {
            animator.SetTrigger("swimAway");
            idleCount = 0;
        }
    }
}