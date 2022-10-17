using System;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [ExecuteAlways]
    public class PolygonCollider2DRenderer : MonoBehaviour {
        #region Inspector

        [SerializeField, Required] private MeshFilter m_Filter = null;
        [SerializeField, Required] private PolygonCollider2D m_Collider = null;
        [SerializeField, Required] private LineRenderer m_Outline = null;
        [SerializeField, Required] private float m_LineThickness = 1;

        #endregion // Inspector

        [NonSerialized] private Mesh m_Mesh;
        [NonSerialized] private uint m_LastHash;

        public PolygonCollider2D Collider { 
            get { return m_Collider; }
        }

        private void OnEnable() {
            ForceRebuild();
        }

        private void OnDisable() {
            m_Filter.sharedMesh = null;
            if (m_Mesh != null) {
                Mesh.DestroyImmediate(m_Mesh);
                m_Mesh = null;
            }
        }

        #if UNITY_EDITOR

        private void Update() {
            if (m_Collider != null) {
                if (!Ref.Replace(ref m_LastHash, m_Collider.GetShapeHash())) {
                    return;
                }

                ForceRebuild();
            }
        }

        #endif // UNITY_EDITOR

        public void Rebuild() {
            if (!m_Filter || !m_Collider) {
                return;
            }

            if (!Ref.Replace(ref m_LastHash, m_Collider.GetShapeHash())) {
                return;
            }

            ForceRebuild();
        }

        [ContextMenu("Rebuild Mesh")]
        public void ForceRebuild() {
            if (!m_Filter || !m_Collider) {
                return;
            }

            BuildMeshAndOutline(m_Collider, ref m_Mesh, m_Outline);
            m_Filter.transform.position = m_Collider.offset;
            m_Outline.widthMultiplier = m_LineThickness;
            m_Filter.sharedMesh = m_Mesh;
        }

        static private void BuildMeshAndOutline(PolygonCollider2D collider, ref Mesh mesh, LineRenderer inOutline) {
            using(PooledList<Vector2> points = PooledList<Vector2>.Create()) {
                int pointCount = collider.GetPath(0, points);

                if (mesh != null) {
                    Mesh.DestroyImmediate(mesh);
                }
                mesh = collider.CreateMesh(false, false);

                if (inOutline != null) {
                    inOutline.positionCount = pointCount;
                    inOutline.loop = true;

                    Vector3[] points3d = new Vector3[pointCount];

                    for(int i = 0; i < pointCount; i++) {
                        points3d[i] = points[i];
                    }

                    inOutline.SetPositions(points3d);
                }
            }
        }
    }
}