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
        public Vector3 Offset = default;
        public bool LookFromOffset;
        [HideIfField("LookFromOffset")] public Vector3 Look = Vector3.forward;

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
                data.Offset = Offset;
                data.Look = Look;
                data.LookFromOffset = LookFromOffset;
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

        [ContextMenu("Look from Offset")]
        private void Editor_LookFromOffset()
        {
            UnityEditor.Undo.RecordObject(this, "Setting look from offset");
            Look = -Offset.normalized;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        #endif // UNITY_EDITOR
    }
}