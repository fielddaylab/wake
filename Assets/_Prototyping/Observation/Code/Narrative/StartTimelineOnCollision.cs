using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Scripting;
using BeauUtil;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(Collider2D))]
public class StartTimelineOnCollision : MonoBehaviour, IScenePreloader
{
    public PlayableDirector playableDirector;

    [NonSerialized] private bool m_Triggered;

    IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext) {
        playableDirector.RebuildGraph();
        yield return null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (m_Triggered || collision.gameObject.layer != GameLayers.Player_Index) {
            return;
        }

        m_Triggered = true;
        if (playableDirector)
        {
            playableDirector.Play();
            using(var table = TempVarTable.Alloc()) {
                table.Set("timelineId", playableDirector.playableAsset.name);
                table.Set("timelineDuration", (float) playableDirector.playableAsset.duration);
                Services.Script.TriggerResponse(GameTriggers.TimelineStarted, table);
            }
        }
        
    }



}
