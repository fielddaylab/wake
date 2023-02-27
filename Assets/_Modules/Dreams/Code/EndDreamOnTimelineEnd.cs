using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;
using UnityEngine.Playables;

namespace Aqua.Dreams {
    public class EndDreamOnTimelineEnd : MonoBehaviour {
        [Required] public PlayableDirector Director;
        public float CloseAfter = 0;

        [NonSerialized] private bool m_Closing;

        private void Awake() {
            Director.stopped += (d) => Close();
            Director.played += (d) => {
                enabled = true;
            };
            if (CloseAfter <= 0 || Director.state != PlayState.Playing) {
                enabled = false;
            }
        }

        private void LateUpdate() {
            if (Director.time <= 0) {
                return;
            }

            if (Director.state != PlayState.Playing
                || (CloseAfter > 0 && Director.time >= CloseAfter)
                || (Director.extrapolationMode == DirectorWrapMode.Hold && Director.time >= Director.duration)) {
                Close();
            }
        }

        private void Close() {
            if (!m_Closing && Services.Valid) {
                StateUtil.LoadSceneWithFader("Cabin", null, null, SceneLoadFlags.StopMusic | SceneLoadFlags.Cutscene, 0.1f);
                m_Closing = true;
                enabled = false;
            }
        }

        #if UNITY_EDITOR

        private void Reset() {
            if (!Director) {
                Director = GetComponent<PlayableDirector>();
            }
        }

        #endif // UNITY_EDITOR
    }
}