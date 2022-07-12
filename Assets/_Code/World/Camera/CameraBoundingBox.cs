using System;
using Aqua.Debugging;
using Aqua.Scripting;
using BeauUtil;
using UnityEngine;

namespace Aqua.Cameras
{
    public class CameraBoundingBox : ScriptComponent
    {
        private enum EdgeType : byte
        {
            DoNotApply,
            Soft,
            Hard
        }

        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Children)] private BoxCollider2D m_Region = null;

        [Header("Borders")]
        [SerializeField, AutoEnum] private EdgeType m_ConstrainTop = EdgeType.Soft;
        [SerializeField, AutoEnum] private EdgeType m_ConstrainBottom = EdgeType.Soft;
        [SerializeField, AutoEnum] private EdgeType m_ConstrainLeft = EdgeType.Soft;
        [SerializeField, AutoEnum] private EdgeType m_ConstrainRight = EdgeType.Soft;

        #endregion // Inspector
        
        [NonSerialized] internal uint m_BoundsHandle;

        private void Awake()
        {
            WorldUtils.ListenForPlayer(m_Region, OnTargetEnter, OnTargetExit);
        }

        private void OnTargetEnter(Collider2D inCollider)
        {
            if (m_BoundsHandle != 0)
                return;

            m_BoundsHandle = Services.Camera.AddBounds(m_Region, SoftEdges(), HardEdges()).Id;
        }

        private void OnTargetExit(Collider2D inCollider)
        {
            if (m_BoundsHandle == 0 || !Services.Camera)
                return;

            Services.Camera.RemoveBounds(m_BoundsHandle);
            m_BoundsHandle = 0;
        }

        private RectEdges SoftEdges()
        {
            RectEdges edges = 0;
            if (m_ConstrainTop == EdgeType.Soft)
                edges |= RectEdges.Top;
            if (m_ConstrainBottom == EdgeType.Soft)
                edges |= RectEdges.Bottom;
            if (m_ConstrainLeft == EdgeType.Soft)
                edges |= RectEdges.Left;
            if (m_ConstrainRight == EdgeType.Soft)
                edges |= RectEdges.Right;
            return edges;
        }

        private RectEdges HardEdges()
        {
            RectEdges edges = 0;
            if (m_ConstrainTop == EdgeType.Hard)
                edges |= RectEdges.Top;
            if (m_ConstrainBottom == EdgeType.Hard)
                edges |= RectEdges.Bottom;
            if (m_ConstrainLeft == EdgeType.Hard)
                edges |= RectEdges.Left;
            if (m_ConstrainRight == EdgeType.Hard)
                edges |= RectEdges.Right;
            return edges;
        }

        #if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (m_Region == null)
                return;

            if (UnityEditor.Selection.Contains(this))
                return;
            
            RenderBox(0.25f);
        }

        private void OnDrawGizmosSelected()
        {
            if (m_Region == null)
                return;
            
            RenderBox(1);
        }

        private void RenderBox(float inAlpha)
        {
            Vector3 center = transform.position;
            Vector2 size = m_Region.size;
            Vector2 offset = m_Region.offset;
            center.x += offset.x;
            center.y += offset.y;

            GizmoViz.Box(center, size, Quaternion.identity, ColorBank.Red, ColorBank.White, SoftEdges() | HardEdges(), inAlpha);
        }

        #endif // UNITY_EDITOR
    }
}