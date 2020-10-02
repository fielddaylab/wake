using System;
using BeauUtil;
using UnityEngine;
using BeauRoutine;
using BeauUtil.Variants;

namespace ProtoAqua.Observation
{
    public class CameraHintConstraint : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private CircleCollider2D m_Circle = null;
        [SerializeField] private float m_InnerRadius = 0;
        [SerializeField] private string m_RegionName = null;

        [Header("Camera Parameters")]
        [SerializeField] private float m_Zoom = 1;
        [SerializeField] private float m_Lerp = 1;

        [Header("Weight")]
        [SerializeField] private float m_InitialWeight = 0;
        [SerializeField, Range(-50, 50)] private float m_MaxWeight = 5;
        [SerializeField] private Curve m_Curve = Curve.Linear;

        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private TriggerListener2D m_Listener;
        [NonSerialized] private Routine m_UpdateRoutine;
        [NonSerialized] private CameraConstraints.Hint m_HintConstraint;

        public StringHash32 RegionName() { return m_RegionName; }

        #region Events

        private void Awake()
        {
            m_Transform = transform;
            m_Circle.EnsureComponent<TriggerListener2D>(ref m_Listener);
            m_Listener.FilterByComponent<CameraTargetConstraint>(ComponentLookupDirection.Parent);
            m_Listener.SetOccupantTracking(true);
            m_Listener.onTriggerEnter.AddListener(OnTargetEnter);
            m_Listener.onTriggerExit.AddListener(OnTargetExit);
        }

        private void OnEnable()
        {
            m_UpdateRoutine = Routine.StartLoop(this, Tick).SetPhase(RoutinePhase.ThinkUpdate);
        }

        private void OnDisable()
        {
            m_UpdateRoutine.Stop();
        }

        private void OnTargetEnter(Collider2D inCollider)
        {
            if (m_HintConstraint != null)
                return;

            m_HintConstraint = ObservationServices.Camera.AddHint(name);
            m_HintConstraint.SetWeight(CalculateWeight, m_InitialWeight);
            PushChanges();

            StringHash32 regionName = RegionName();
            if (!regionName.IsEmpty)
                Services.Data.SetVariable(GameVars.CameraRegion, regionName);
        }

        private void OnTargetExit(Collider2D inCollider)
        {
            if (m_HintConstraint == null || !ObservationServices.Camera)
                return;
            
            ObservationServices.Camera.RemoveHint(m_HintConstraint);
            m_HintConstraint = null;

            if (Services.Data)
            {
                StringHash32 regionName = RegionName();
                if (!regionName.IsEmpty && Services.Data.GetVariable(GameVars.CameraRegion) == regionName)
                    Services.Data.SetVariable(GameVars.CameraRegion, Variant.Null);
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
            if (m_HintConstraint != null)
            {
                m_HintConstraint.PositionAt(transform, m_Circle.offset);
                m_HintConstraint.Zoom = m_Zoom;
                m_HintConstraint.LerpFactor = m_Lerp;
            }
        }

        private float CalculateWeight(CameraConstraints.Hint inHint, Vector2 inPos)
        {
            Vector2 selfPos = inHint.Position();
            float dist = Vector2.Distance(inPos, selfPos);

            return CameraConstraints.Hint.CalculateWeightForDistance(dist, m_Circle.radius, m_InnerRadius, m_MaxWeight, m_Curve);
        }

        #endregion // Tick

        #if UNITY_EDITOR

        // private void OnDrawGizmos()
        // {
        //     if (m_Box == null)
        //         return;

        //     if (UnityEditor.Selection.Contains(this))
        //         return;
            
        //     RenderBox(0.25f);
        // }

        // private void OnDrawGizmosSelected()
        // {
        //     if (m_Box == null)
        //         return;
            
        //     RenderBox(1);
        // }

        // private void RenderBox(float inAlpha)
        // {
        //     Vector3 center = transform.position;
        //     Vector3 size = m_Box.size;
        //     Vector2 offset = m_Box.offset;
        //     center.x += offset.x;
        //     center.y += offset.y;
            
        //     size.z = 0.01f;
        //     Gizmos.color = ColorBank.Red.WithAlpha(0.25f * inAlpha);
        //     Gizmos.DrawCube(center, size);

        //     Gizmos.color = ColorBank.White.WithAlpha(0.8f * inAlpha);

        //     Vector3 topRight = center + size / 2;
        //     Vector3 bottomLeft = center - size / 2;
        //     Vector3 topLeft = new Vector3(bottomLeft.x, topRight.y);
        //     Vector3 bottomRight = new Vector3(topRight.x, bottomLeft.y);

        //     topRight.z = topLeft.z = bottomLeft.z = bottomRight.z = center.z - 1;

        //     if (m_ConstrainLeft)
        //         Gizmos.DrawLine(bottomLeft, topLeft);
        //     if (m_ConstrainRight)
        //         Gizmos.DrawLine(bottomRight, topRight);
        //     if (m_ConstrainTop)
        //         Gizmos.DrawLine(topLeft, topRight);
        //     if (m_ConstrainBottom)
        //         Gizmos.DrawLine(bottomLeft, bottomRight);
        // }

        #endif // UNITY_EDITOR
    }
}