using System;
using BeauUtil;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif // UNITY_EDITOR

namespace Aqua {
    [ExecuteAlways]
    public class BoxEdgeCollider2D : MonoBehaviour, IColliderMaterialSource
    {
        #region Inspector

        [SerializeField, PrefabModeOnly] private BoxCollider2D[] m_Colliders = null;
        [SerializeField] private float m_EdgeThickness = 1;
        [SerializeField] private bool m_IsTrigger = false;

        [Header("Sizing")]
        [SerializeField] private Vector2 m_Size = new Vector2(1, 1);
        [SerializeField, AutoEnum] private RectEdges m_Edges = RectEdges.All;

        [Header("Materials")]
        [SerializeField, AutoEnum] private ColliderMaterialId m_TopMaterial = ColliderMaterialId.Invisible;
        [SerializeField, AutoEnum] private ColliderMaterialId m_BottomMaterial = ColliderMaterialId.Invisible;
        [SerializeField, AutoEnum] private ColliderMaterialId m_LeftMaterial = ColliderMaterialId.Invisible;
        [SerializeField, AutoEnum] private ColliderMaterialId m_RightMaterial = ColliderMaterialId.Invisible;

        #endregion // Inspector

        private readonly RectEdges[] m_EdgeMapping = new RectEdges[4];

        private void OnEnable()
        {
            #if UNITY_EDITOR
            if (m_Colliders == null)
                return;
            #endif // UNITY_EDITOR
            RefreshColliders();
        }

        public ColliderMaterialId GetMaterial(Collider2D collider) {
            for(int i = 0; i < m_Colliders.Length; i++) {
                if (m_Colliders[i] == collider) {
                    switch(m_EdgeMapping[i]) {
                        case RectEdges.Left: {
                            return m_LeftMaterial;
                        }
                        case RectEdges.Right: {
                            return m_RightMaterial;
                        }
                        case RectEdges.Bottom: {
                            return m_BottomMaterial;
                        }
                        case RectEdges.Top: {
                            return m_TopMaterial;
                        }
                        default: {
                            return ColliderMaterialId.Invisible;
                        }
                    }
                }
            }

            return ColliderMaterial.DefaultMaterial;
        }

        #if UNITY_EDITOR

        private bool m_RefreshQueued;

        private void Reset()
        {
            m_Colliders = GetComponents<BoxCollider2D>();
            int collidersToAdd = 4 - m_Colliders.Length;
            if (collidersToAdd > 0)
            {
                int startIdx = m_Colliders.Length;
                Array.Resize(ref m_Colliders, 4);
                for(int i = startIdx; i < 4; i++)
                {
                    m_Colliders[i] = gameObject.AddComponent<BoxCollider2D>();
                }
            }
        }

        private void OnValidate()
        {
            if (!m_RefreshQueued)
            {
                m_RefreshQueued = true;
                UnityEditor.EditorApplication.delayCall += () => {
                    if (!this)
                        return;

                    m_RefreshQueued = false;
                    RefreshColliders();
                };
            }
        }

        [UnityEditor.CustomEditor(typeof(BoxEdgeCollider2D)), CanEditMultipleObjects]
        private class Inspector : UnityEditor.Editor
        {
            private readonly BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

            private void OnEnable()
            {
                m_BoundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
            }

            private void OnSceneGUI() {
                BoxEdgeCollider2D edgeCollider = target as BoxEdgeCollider2D;
                if (!edgeCollider) {
                    return;
                }

                Transform colliderTransform = edgeCollider.transform;

                Matrix4x4 mat = colliderTransform.localToWorldMatrix;
                mat.SetRow(0, Vector4.Scale(mat.GetRow(0), new Vector4(1f, 1f, 0f, 1f)));
                mat.SetRow(1, Vector4.Scale(mat.GetRow(1), new Vector4(1f, 1f, 0f, 1f)));
                mat.SetRow(2, new Vector4(0f, 0f, 1f, colliderTransform.position.z));

                using(new Handles.DrawingScope(mat)) {
                    m_BoundsHandle.center = Vector3.zero;
                    m_BoundsHandle.size = edgeCollider.m_Size;
                    m_BoundsHandle.SetColor(Color.green);

                    EditorGUI.BeginChangeCheck();
                    m_BoundsHandle.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(edgeCollider, "Modifying collider");
                        Undo.RecordObject(edgeCollider.transform, "Modifying collider");

                        Vector3 center = m_BoundsHandle.center;
                        edgeCollider.m_Size = m_BoundsHandle.size;
                        colliderTransform.position += m_BoundsHandle.center;

                        edgeCollider.RefreshColliders();

                        EditorUtility.SetDirty(colliderTransform);
                        EditorUtility.SetDirty(edgeCollider);
                    }
                }
            }
        }

        #endif // UNITY_EDITOR

        private void RefreshColliders()
        {
            int used = 0;
            int colliderCount = m_Colliders.Length;
            BoxCollider2D collider;

            float halfWidth = m_Size.x / 2;
            float halfHeight = m_Size.y / 2;
            float halfEdge = m_EdgeThickness / 2;

            if ((m_Edges & RectEdges.Left) != 0)
            {
                collider = m_Colliders[used++];
                collider.enabled = true;
                collider.offset = new Vector2(-halfWidth - halfEdge, 0);
                collider.size = new Vector2(m_EdgeThickness, m_Size.y + m_EdgeThickness);
                collider.isTrigger = m_IsTrigger;
                m_EdgeMapping[used - 1] = RectEdges.Left;
            }

            if ((m_Edges & RectEdges.Right) != 0)
            {
                collider = m_Colliders[used++];
                collider.enabled = true;
                collider.offset = new Vector2(halfWidth + halfEdge, 0);
                collider.size = new Vector2(m_EdgeThickness, m_Size.y + m_EdgeThickness);
                collider.isTrigger = m_IsTrigger;
                m_EdgeMapping[used - 1] = RectEdges.Right;
            }

            if ((m_Edges & RectEdges.Top) != 0)
            {
                collider = m_Colliders[used++];
                collider.enabled = true;
                collider.offset = new Vector2(0, halfHeight + halfEdge);
                collider.size = new Vector2(m_Size.x + m_EdgeThickness, m_EdgeThickness);
                collider.isTrigger = m_IsTrigger;
                m_EdgeMapping[used - 1] = RectEdges.Top;
            }

            if ((m_Edges & RectEdges.Bottom) != 0)
            {
                collider = m_Colliders[used++];
                collider.enabled = true;
                collider.offset = new Vector2(0, -halfHeight - halfEdge);
                collider.size = new Vector2(m_Size.x + m_EdgeThickness, m_EdgeThickness);
                collider.isTrigger = m_IsTrigger;
                m_EdgeMapping[used - 1] = RectEdges.Bottom;
            }

            for(; used < colliderCount; used++)
            {
                m_Colliders[used].enabled = false;
                m_EdgeMapping[used] = RectEdges.None;
            }
        }
    }
}