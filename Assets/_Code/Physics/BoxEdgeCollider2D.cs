using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [ExecuteAlways]
    public class BoxEdgeCollider2D : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private BoxCollider2D[] m_Colliders = null;
        [SerializeField] private float m_EdgeThickness = 1;

        [Header("Sizing")]
        [SerializeField] private Vector2 m_Size = new Vector2(1, 1);
        [SerializeField, AutoEnum] private RectEdges m_Edges = RectEdges.All;

        #endregion // Inspector

        private void OnEnable()
        {
            RefreshColliders();
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            m_Colliders = GetComponents<BoxCollider2D>();
        }

        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () => {
                if (!this)
                    return;

                RefreshColliders();
            };
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
                collider.offset = new Vector2(-halfWidth - halfEdge, 0);
                collider.size = new Vector2(m_EdgeThickness, m_Size.y + m_EdgeThickness);
            }

            if ((m_Edges & RectEdges.Right) != 0)
            {
                collider = m_Colliders[used++];
                collider.offset = new Vector2(halfWidth + halfEdge, 0);
                collider.size = new Vector2(m_EdgeThickness, m_Size.y + m_EdgeThickness);
            }

            if ((m_Edges & RectEdges.Top) != 0)
            {
                collider = m_Colliders[used++];
                collider.offset = new Vector2(0, halfHeight + halfEdge);
                collider.size = new Vector2(m_Size.x + m_EdgeThickness, m_EdgeThickness);
            }

            if ((m_Edges & RectEdges.Bottom) != 0)
            {
                collider = m_Colliders[used++];
                collider.offset = new Vector2(0, -halfHeight - halfEdge);
                collider.size = new Vector2(m_Size.x + m_EdgeThickness, m_EdgeThickness);
            }

            for(; used < colliderCount; used++)
            {
                m_Colliders[used].enabled = false;
            }
        }
    }
}