using System;
using BeauUtil;
using UnityEngine;
using BeauRoutine;

namespace ProtoAqua.Observation
{
    public class CameraBoundsConstraint : MonoBehaviour
    {
        private enum EdgeType : byte
        {
            DoNotApply,
            Soft,
            Hard
        }

        #region Inspector

        [SerializeField] private BoxCollider2D m_Box = null;

        [Header("Borders")]
        [SerializeField, AutoEnum] private EdgeType m_ConstrainTop = EdgeType.Soft;
        [SerializeField, AutoEnum] private EdgeType m_ConstrainBottom = EdgeType.Soft;
        [SerializeField, AutoEnum] private EdgeType m_ConstrainLeft = EdgeType.Soft;
        [SerializeField, AutoEnum] private EdgeType m_ConstrainRight = EdgeType.Soft;

        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private TriggerListener2D m_Listener;
        [NonSerialized] private Routine m_UpdateListenerRoutine;
        [NonSerialized] private CameraConstraints.Bounds m_BoundsConstraint;

        #region Events

        private void Awake()
        {
            m_Transform = transform;
            m_Box.EnsureComponent<TriggerListener2D>(ref m_Listener);
            m_Listener.SetOccupantTracking(true);
            m_Listener.FilterByComponentInParent<CameraTargetConstraint>();
            m_Listener.LayerFilter = GameLayers.Player_Mask;
            
            m_Listener.onTriggerEnter.AddListener(OnTargetEnter);
            m_Listener.onTriggerExit.AddListener(OnTargetExit);
        }

        private void OnEnable()
        {
            m_UpdateListenerRoutine = Routine.StartLoop(this, Tick).SetPhase(RoutinePhase.ThinkUpdate);
        }

        private void OnDisable()
        {
            m_UpdateListenerRoutine.Stop();
        }

        private void OnTargetEnter(Collider2D inCollider)
        {
            if (m_BoundsConstraint != null)
                return;

            m_BoundsConstraint = ObservationServices.Camera.AddBounds(name);
            PushChanges();
        }

        private void OnTargetExit(Collider2D inCollider)
        {
            if (m_BoundsConstraint == null || !ObservationServices.Camera)
                return;
            
            ObservationServices.Camera.RemoveBounds(m_BoundsConstraint);
            m_BoundsConstraint = null;
            Debug.LogFormat("[CameraBoundsConstraint] Removed bounding region '{0}'", name);
        }

        #endregion // Events

        #region Tick

        private void Tick()
        {
            m_Listener.ProcessOccupants();
            PushChanges();
        }

        private void PushChanges()
        {
            if (m_BoundsConstraint != null)
            {
                m_BoundsConstraint.Region = Rect();
                m_BoundsConstraint.SoftEdges = SoftEdges();
                m_BoundsConstraint.HardEdges = HardEdges();
            }
        }

        #endregion // Tick

        public Rect Rect()
        {
            Rect rect = new Rect();
            rect.size = m_Box.size;
            rect.center = (Vector2) m_Transform.position + m_Box.offset;
            return rect;
        }

        public RectEdges SoftEdges()
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

        public RectEdges HardEdges()
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
            if (m_Box == null)
                return;

            if (UnityEditor.Selection.Contains(this))
                return;
            
            RenderBox(0.25f);
        }

        private void OnDrawGizmosSelected()
        {
            if (m_Box == null)
                return;
            
            RenderBox(1);
        }

        private void RenderBox(float inAlpha)
        {
            Vector3 center = transform.position;
            Vector3 size = m_Box.size;
            Vector2 offset = m_Box.offset;
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