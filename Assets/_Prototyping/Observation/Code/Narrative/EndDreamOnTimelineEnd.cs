using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;
using UnityEngine.Playables;

public class EndDreamOnTimelineEnd : MonoBehaviour
{
    [Required] public PlayableDirector Director;
    public float CloseAfter = 0;

    [NonSerialized] private bool m_Closing;

    private void Awake() {
        Director.stopped += (d) => Close();
        Director.played += (d) => {
            if (CloseAfter > 0) {
                enabled = true;
            }
        };
        if (Director.state != PlayState.Playing) {
            enabled = false;
        }
    }

    private void LateUpdate() {
        if (Director.time >= CloseAfter) {
            Close();
        }
    }

    private void Close() {
        if (!m_Closing) {
            StateUtil.LoadSceneWithFader("Cabin", null, null, SceneLoadFlags.StopMusic | SceneLoadFlags.Cutscene);
            m_Closing = true;
            enabled = false;
        }
    }
}
