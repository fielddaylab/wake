using System;
using BeauRoutine;
using BeauUtil;
using UnityEditor;
using UnityEngine;

namespace Aqua
{
    [ExecuteAlways]
    public class BoxEdgeCollider2D : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private BoxCollider2D[] m_Colliders = null;
        [SerializeField] private float m_EdgeThickness = 1;
        [SerializeField] private bool m_IsTrigger = false;

        [Header("Sizing")]
        [SerializeField] private Vector2 m_Size = new Vector2(1, 1);
        [SerializeField, AutoEnum] private RectEdges m_Edges = RectEdges.All;

        #endregion // Inspector

        private void OnEnable()
        {
            #if UNITY_EDITOR
            if (m_Colliders == null)
                return;
            #endif // UNITY_EDITOR
            RefreshColliders();
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
            }

            if ((m_Edges & RectEdges.Right) != 0)
            {
                collider = m_Colliders[used++];
                collider.enabled = true;
                collider.offset = new Vector2(halfWidth + halfEdge, 0);
                collider.size = new Vector2(m_EdgeThickness, m_Size.y + m_EdgeThickness);
                collider.isTrigger = m_IsTrigger;
            }

            if ((m_Edges & RectEdges.Top) != 0)
            {
                collider = m_Colliders[used++];
                collider.enabled = true;
                collider.offset = new Vector2(0, halfHeight + halfEdge);
                collider.size = new Vector2(m_Size.x + m_EdgeThickness, m_EdgeThickness);
                collider.isTrigger = m_IsTrigger;
            }

            if ((m_Edges & RectEdges.Bottom) != 0)
            {
                collider = m_Colliders[used++];
                collider.enabled = true;
                collider.offset = new Vector2(0, -halfHeight - halfEdge);
                collider.size = new Vector2(m_Size.x + m_EdgeThickness, m_EdgeThickness);
                collider.isTrigger = m_IsTrigger;
            }

            for(; used < colliderCount; used++)
            {
                m_Colliders[used].enabled = false;
            }
        }
    }
}