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
        #region Inspector

        public Transform Target = null;
        public float Height = 10;
        public float Zoom = 1;

        [AutoEnum] public CameraPoseProperties Properties = CameraPoseProperties.All;

        #endregion // Inspector

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
            
            Vector3 center = transform.position;
            if (Target != null)
                center.z = Target.position.z;
            
            Vector2 size;
            size.y = Height / Zoom;
            size.x = Height * main.aspect / Zoom;
            GizmoViz.Box(center, size, plane.transform.rotation, ColorBank.Teal, ColorBank.White, RectEdges.All, inAlpha);
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

        [Hidden] PosAndZoom = Position | Zoom,
        [Hidden] HeightAndZoom = Height | Zoom,
        [Hidden] All = Position | Height | Zoom
    }
}