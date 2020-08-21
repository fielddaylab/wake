using System;
using BeauUtil;
using UnityEngine;
using BeauRoutine;

namespace ProtoAqua.Observation
{
    public class CameraBoundsConstraint : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private BoxCollider2D m_Box = null;

        [Header("Borders")]
        [SerializeField] private bool m_ConstrainTop = true;
        [SerializeField] private bool m_ConstrainBottom = true;
        [SerializeField] private bool m_ConstrainLeft = true;
        [SerializeField] private bool m_ConstrainRight = true;

        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private TriggerListener2D m_Listener;
        [NonSerialized] private Routine m_UpdateListenerRoutine;
        [NonSerialized] private CameraConstraints.Bounds m_BoundsConstraint;

        #region Events

        private void Awake()
        {
            m_Transform = transform;
            this.EnsureComponent<TriggerListener2D>(ref m_Listener);
            m_Listener.SetOccupantTracking(true);
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

            if (inCollider.GetComponentInParent<CameraTargetConstraint>())
            {
                m_BoundsConstraint = ObservationServices.Camera.AddBounds();
                PushChanges();
                Debug.LogFormat("[CameraBoundsConstraint] Added bounding region '{0}'", name);
            }
        }

        private void OnTargetExit(Collider2D inCollider)
        {
            if (m_BoundsConstraint == null)
                return;
            
            if (inCollider.GetComponentInParent<CameraTargetConstraint>())
            {
                ObservationServices.Camera.RemoveBounds(m_BoundsConstraint);
                m_BoundsConstraint = null;
                Debug.LogFormat("[CameraBoundsConstraint] Removed bounding region '{0}'", name);
            }
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
                m_BoundsConstraint.Edges = Edges();
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

        public RectEdges Edges()
        {
            RectEdges edges = 0;
            if (m_ConstrainTop)
                edges |= RectEdges.Top;
            if (m_ConstrainBottom)
                edges |= RectEdges.Bottom;
            if (m_ConstrainLeft)
                edges |= RectEdges.Left;
            if (m_ConstrainRight)
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

            if (m_ConstrainLeft)
                Gizmos.DrawLine(bottomLeft, topLeft);
            if (m_ConstrainRight)
                Gizmos.DrawLine(bottomRight, topRight);
            if (m_ConstrainTop)
                Gizmos.DrawLine(topLeft, topRight);
            if (m_ConstrainBottom)
                Gizmos.DrawLine(bottomLeft, bottomRight);
        }

        #endif // UNITY_EDITOR
    }
}