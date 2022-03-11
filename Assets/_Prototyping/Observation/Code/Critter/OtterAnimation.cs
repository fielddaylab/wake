using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using UnityEngine;

public class OtterAnimation : MonoBehaviour
{
    public float startDelay;
    public int idleRepeat = 1;
    public Animator animator;
    private int idleCount = 0;

    void OnEnable()
    {
        Routine.Start(this, DelayedAnimation());
    }

    // The delay coroutine
    IEnumerator DelayedAnimation()
    {
        yield return startDelay;
        animator.Play("SwimTo");
    }

    public void idlePlayed()
    {
        idleCount = idleCount + 1;
        // Debug.Log("IdleCount:" + idleCount);
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