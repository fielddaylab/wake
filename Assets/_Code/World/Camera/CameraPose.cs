using BeauUtil;
using UnityEngine;
using System;
using Aqua.Debugging;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif // UNITY_EDITOR

namespace Aqua.Cameras
{
    public class CameraPose : MonoBehaviour
    {
        [Serializable]
        public struct Data {
            public Vector3 Position;
            public Quaternion Rotation;
            public CameraFOVMode Mode;
            public Transform Target;
            public float Height;
            public float Zoom;
            public float FOV;
            public float AudioListenerZOffset;
            public CameraPoseProperties Properties;
        }

        #region Inspector

        public CameraFOVMode Mode;
        [HideIfField("IsFOVDirect")] public Transform Target = null;
        [HideIfField("IsFOVDirect")] public float Height = 10;
        [HideIfField("IsFOVDirect")] public float Zoom = 1;
        [HideIfField("IsFOVDirect")] public float AudioListenerZOffset = 0;
        [ShowIfField("IsFOVDirect")] public float FieldOfView = 30;

        [AutoEnum] public CameraPoseProperties Properties = CameraPoseProperties.Default;

        #endregion // Inspector

        public void ReadData(ref Data data) {
            data.Position = transform.position;
            data.Rotation = transform.rotation;
            data.Mode = Mode;
            data.Target = Target;
            data.Height = Height;
            data.Zoom = Zoom;
            data.AudioListenerZOffset = AudioListenerZOffset;
            data.Properties = Properties;
            data.FOV = FieldOfView;
        }

        public void WriteData(in Data data) {
            #if UNITY_EDITOR
            Undo.RecordObject(this, "Writing camera pose data");
            Undo.RecordObject(transform, "Writing camera pose data");
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(transform);
            #endif // UNITY_EDITOR

            transform.SetPositionAndRotation(data.Position, data.Rotation);
            Mode = data.Mode;
            Target = data.Target;
            Height = data.Height;
            Zoom = data.Zoom;
            AudioListenerZOffset = data.AudioListenerZOffset;
            Properties = data.Properties;
            FieldOfView = data.FOV;
        }

        #region Editor

        #if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (UnityEditor.Selection.Contains(this))
                return;
            
            RenderBox(0.25f);
        }

        private void OnDrawGizmosSelected()
        {
            RenderBox(1);
        }

        private void RenderBox(float inAlpha)
        {
            Camera main = Camera.main;
            if (main == null)
                return;

            CameraFOVPlane plane = main.GetComponent<CameraFOVPlane>();
            CameraRig rig = main.GetComponentInParent<CameraRig>();
            
            Vector3 center = transform.position;
            Quaternion rot = transform.rotation;
            if (Target != null)
                center.z = Target.position.z;
            
            Vector2 size;
            size.y = Height / Zoom;
            size.x = Height * main.aspect / Zoom;
            
            GizmoViz.Box(center, size, rot, ColorBank.Teal, ColorBank.White, RectEdges.All, inAlpha);
        }

        private bool IsFOVDirect()
        {
            return Mode == CameraFOVMode.Direct;
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }

    [Flags]
    public enum CameraPoseProperties : byte
    {
        Position = 0x01,
        Height = 0x02,
        Zoom = 0x04,
        Rotation = 0x8,
        FieldOfView = 0x10,

        [Hidden] PosAndZoom = Position | Zoom,
        [Hidden] HeightAndZoom = Height | Zoom,
        [Hidden] Default = Position | Height | Zoom,
        [Hidden] All = Default | Rotation | FieldOfView
    }
    
    public enum CameraFOVMode : byte
    {
        Plane,
        Direct
    }
}