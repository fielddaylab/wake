using System;
using Aqua.Scripting;
using BeauUtil;
using UnityEngine;

namespace Aqua.Cameras
{
    public class CameraTarget : ScriptComponent
    {
        #region Inspector

        public float Lerp = 5;
        [Range(0.01f, 25)] public float Zoom = 1;
        [AutoEnum] public CameraModifierFlags Flags = CameraModifierFlags.All;

        #endregion // Inspector
        
        [NonSerialized] internal uint m_TargetHandle;

        private void OnEnable()
        {
            if (Script.IsLoading)
                return;

            Services.Camera.PushTarget(this);
        }

        private void OnDisable()
        {
            if (!Services.Valid)
                return;
            
            if (Script.IsLoading)
                return;

            Services.Camera.PopTarget(this);
        }

        public void PushChanges()
        {
            if (Script.IsLoading)
                return;
            
            if (m_TargetHandle != 0)
            {
                ref CameraTargetData data = ref Services.Camera.FindTarget(m_TargetHandle);
                data.Zoom = Zoom;
                data.Lerp = Lerp;
                data.Flags = Flags;
            }
        }
    }
}