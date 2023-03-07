using UnityEngine;
using System;
using BeauUtil;

#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
#endif // UNITY_EDITOR

namespace Aqua {
    [ExecuteAlways]
    public class FogPlane : MonoBehaviour {
        public float Start;
        public float Range;
        
        [NonSerialized] private Camera m_Camera;

        private void OnEnable() {
            #if UNITY_EDITOR
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;
            #endif // UNITY_EDITOR

            TransformHelper.TryGetCamera(transform, out m_Camera);
        }

        private void LateUpdate() {
            #if UNITY_EDITOR
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;

            if (!Application.IsPlaying(this)) {
                TransformHelper.TryGetCamera(transform, out m_Camera);
            }

            if (!m_Camera) {
                return;
            }
            #endif // UNITY_EDITOR

            Plane p = new Plane(-transform.forward, transform.position);
            if (CameraHelper.TryGetDistanceToPlane(m_Camera, p, out float dist)) {
                RenderSettings.fogStartDistance = Start + dist;
                RenderSettings.fogEndDistance = Start + dist + Range;
            }
        }
    }
}