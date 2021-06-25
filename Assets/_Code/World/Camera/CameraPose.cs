using BeauUtil;
using UnityEngine;

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
            
            Vector3 size;
            size.y = Height / Zoom;
            size.x = Height * main.aspect / Zoom;
            size.z = 0.01f;
            Gizmos.color = ColorBank.Teal.WithAlpha(0.25f * inAlpha);
            Gizmos.DrawCube(center, size);

            Gizmos.color = ColorBank.White.WithAlpha(0.8f * inAlpha);

            Vector3 topRight = center + size / 2;
            Vector3 bottomLeft = center - size / 2;
            Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y);
            Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y);

            topRight.z = topLeft.z = bottomLeft.z = bottomRight.z = center.z - 1;

            Gizmos.DrawLine(bottomLeft, topLeft);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(bottomLeft, bottomRight);
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}