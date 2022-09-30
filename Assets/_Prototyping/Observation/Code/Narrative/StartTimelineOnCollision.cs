using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class StartTimelineOnCollision : MonoBehaviour
{
    public PlayableDirector playableDirector;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (playableDirector)
        {
            {
                playableDirector.Play();
            }
        }
        
    }



}
