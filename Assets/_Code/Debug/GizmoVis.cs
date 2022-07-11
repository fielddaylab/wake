using BeauUtil;
using UnityEngine;

namespace Aqua.Debugging {
    static public class GizmoViz {
        static public void Box(Vector3 center, Vector2 size, Color boxColor, float alpha) {
            Box(center, size, Quaternion.identity, boxColor, ColorBank.White, RectEdges.All, alpha);
        }

        static public void Box(Vector3 center, Vector2 size, Quaternion rotation, Color boxColor, Color outlineColor, RectEdges edges, float alpha) {
            Vector3 size3 = size;
            size3.z = 0.01f;
            Gizmos.color = boxColor.WithAlpha(0.4f * alpha);
            Gizmos.matrix = Matrix4x4.Rotate(rotation);
            
            Gizmos.DrawCube(center, size);

            Gizmos.color = outlineColor.WithAlpha(0.9f * alpha);

            if (edges != 0) {
                Vector3 topRight = center + size3 / 2;
                Vector3 bottomLeft = center - size3 / 2;
                Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y);
                Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y);

                topRight.z = topLeft.z = bottomLeft.z = bottomRight.z = center.z - 0.0001f;

                if ((edges & RectEdges.Left) != 0) {
                    Gizmos.DrawLine(bottomLeft, topLeft);
                }
                if ((edges & RectEdges.Right) != 0) {
                    Gizmos.DrawLine(bottomRight, topRight);
                }
                if ((edges & RectEdges.Top) != 0) {
                    Gizmos.DrawLine(topLeft, topRight);
                }
                if ((edges & RectEdges.Bottom) != 0) {
                    Gizmos.DrawLine(bottomLeft, bottomRight);
                }
            }
        }

        static public void Frustum(Camera referenceCam, Vector3 center, Quaternion rotation, Color boxColor, Color outlineColor, float distance, float heightAtDistance, float alpha) {
            Gizmos.color = boxColor.WithAlpha(0.4f * alpha);
            Gizmos.matrix = Matrix4x4.Rotate(rotation);
            float fov = 2.0f * Mathf.Atan(heightAtDistance * 0.5f / distance) * Mathf.Rad2Deg;
            Gizmos.DrawFrustum(center, fov, referenceCam.farClipPlane, referenceCam.nearClipPlane, referenceCam.aspect);
        }
    }
}