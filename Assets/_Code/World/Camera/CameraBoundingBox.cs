using System;
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
            TriggerListener2D listener = m_Region.EnsureComponent<TriggerListener2D>();
            listener.SetOccupantTracking(false);
            listener.LayerFilter = GameLayers.Player_Mask;

            listener.onTriggerEnter.AddListener(OnTargetEnter);
            listener.onTriggerExit.AddListener(OnTargetExit);
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
            Vector3 size = m_Region.size;
            Vector2 offset = m_Region.offset;
            center.x += offset.x;
            center.y += offset.y;
            
            size.z = 0.01f;
            Gizmos.color = ColorBank.Red.WithAlpha(0.25f * inAlpha);
            Gizmos.DrawCube(center, size);

            Gizmos.color = ColorBank.White.WithAlpha(0.8f * inAlpha);

            Vector3 topRight = center + size / 2;
            Vector3 bottomLeft = center - size / 2;
            Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y);
            Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y);

            topRight.z = topLeft.z = bottomLeft.z = bottomRight.z = center.z - 1;

            if (m_ConstrainLeft != EdgeType.DoNotApply)
                Gizmos.DrawLine(bottomLeft, topLeft);
            if (m_ConstrainRight != EdgeType.DoNotApply)
                Gizmos.DrawLine(bottomRight, topRight);
            if (m_ConstrainTop != EdgeType.DoNotApply)
                Gizmos.DrawLine(topLeft, topRight);
            if (m_ConstrainBottom != EdgeType.DoNotApply)
                Gizmos.DrawLine(bottomLeft, bottomRight);
        }

        #endif // UNITY_EDITOR
    }
}