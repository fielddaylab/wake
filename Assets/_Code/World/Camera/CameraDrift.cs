using System;
using Aqua.Scripting;
using BeauUtil;
using UnityEngine;

namespace Aqua.Cameras
{
    public class CameraDrift : ScriptComponent
    {
        #region Inspector

        public Vector2 Distance = new Vector2(5, 5);
        public Vector2 Period = new Vector2(1, 1.5f);
        public float Scale = 1.0f;

        #endregion // Inspector
        
        [NonSerialized] internal uint m_TargetHandle;

        private void OnEnable()
        {
            Script.OnSceneLoad(EnableTarget);
        }

        private void EnableTarget() {
            if (m_TargetHandle == 0) {
                m_TargetHandle = Services.Camera.AddDrift(Distance * Scale, Period, RNG.Instance.NextVector2()).Id;
            }
        }

        private void OnDisable()
        {
            if (!Services.Valid)
                return;
            
            if (Script.IsLoading)
                return;

            Services.Camera.RemoveDrift(m_TargetHandle);
            m_TargetHandle = 0;
        }

        public void PushChanges()
        {
            if (Script.IsLoading)
                return;
            
            if (m_TargetHandle != 0)
            {
                ref CameraDriftData data = ref Services.Camera.FindDrift(m_TargetHandle);
                data.Distance = Distance * Scale;
                data.Period = Period;
            }
        }

        #if UNITY_EDITOR

        private void OnValidate()
        {
            if (Application.IsPlaying(this) && m_TargetHandle != 0 && UnityEditor.Selection.Contains(gameObject))
            {
                PushChanges();
            }
        }

        #endif // UNITY_EDITOR
    }
}