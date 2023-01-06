using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;
using UnityEngine.Playables;

namespace Aqua.Dreams {
    public class EndDreamOnCollision : MonoBehaviour {
        public float FadeOutTime = 0.1f;

        [NonSerialized] private bool m_Closing;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (m_Closing || collision.gameObject.layer != GameLayers.Player_Index) {
                return;
            }

            m_Closing = true;
            StateUtil.LoadSceneWithFader("Cabin", null, null, SceneLoadFlags.StopMusic | SceneLoadFlags.Cutscene, FadeOutTime);
        }
    }
}