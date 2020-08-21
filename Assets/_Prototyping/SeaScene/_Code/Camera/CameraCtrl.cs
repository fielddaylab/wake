using System;
using System.Collections.Generic;
using BeauData;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation
{
    public class CameraCtrl : ServiceBehaviour
    {        
        #region Inspector

        [SerializeField] private Camera m_Camera = null;
        [SerializeField] private CameraFOVPlane m_CameraPlane = null;

        [SerializeField] private Transform m_RootTransform = null;
        [SerializeField] private Transform m_EffectsTransform = null;

        [SerializeField] private float m_DefaultLerp = 5;
        [SerializeField] private float m_TargetWeight = 0.5f;

        #endregion // Inspector

        [NonSerialized] private List<CameraConstraints.Bounds> m_BoundsConstraints = new List<CameraConstraints.Bounds>();
        [NonSerialized] private List<CameraConstraints.Hint> m_HintConstraints = new List<CameraConstraints.Hint>();
        [NonSerialized] private CameraConstraints.Target m_TargetConstraint;

        #region Events

        private void Awake()
        {
            
        }

        private void LateUpdate()
        {
            DriveCamera();
        }

        #endregion // Events

        public Vector3 ScreenToWorldOnPlane(Vector2 inScreenPos, Transform inWorldRef)
        {
            Vector3 screenPos = inScreenPos;
            screenPos.z = 1;

            Plane p = new Plane(-m_Camera.transform.forward, inWorldRef.position);
            Ray r = m_Camera.ScreenPointToRay(screenPos);

            float dist;
            p.Raycast(r, out dist);

            return r.GetPoint(dist);
        }

        #region Camera Update

        private void DriveCamera()
        {
            Vector2 currentPos = m_RootTransform.localPosition;
            float zoom = m_CameraPlane.Zoom;

            UpdateSmartCamera(ref currentPos, ref zoom);

            m_CameraPlane.Zoom = zoom;
            m_RootTransform.SetPosition(currentPos, Axis.XY, Space.Self);

            #if UNITY_EDITOR

            #endif // UNITY_EDITOR
        }

        private void UpdateSmartCamera(ref Vector2 ioPos, ref float ioZoom)
        {
            Vector2 targetPos = ioPos;
            float targetZoom = ioZoom;
            float lerp = m_DefaultLerp;

            CameraConstraints.HintAccumulator accum = new CameraConstraints.HintAccumulator();

            if (m_TargetConstraint != null)
            {
                targetPos = m_TargetConstraint.Position();
                float weight = Math.Abs(m_TargetWeight);
                accum.Lerp += m_TargetConstraint.LerpFactor * weight;
                accum.Zoom += m_TargetConstraint.Zoom * weight;
                accum.TotalWeight += weight;
            }

            for(int i = m_HintConstraints.Count - 1; i >= 0; --i)
                m_HintConstraints[i].Contribute(targetPos, ref accum);

            if (accum.TotalWeight > 0)
            {
                accum.Calculate();

                targetPos += accum.Offset;
                targetZoom = accum.Zoom;
                lerp = accum.Lerp;
            }

            lerp = TweenUtil.Lerp(lerp, 1, Routine.DeltaTime);
            ioPos = Vector2.LerpUnclamped(ioPos, targetPos, lerp);
            ioZoom = Mathf.LerpUnclamped(ioZoom, targetZoom, lerp);

            ConstrainPosition(ref ioPos, ioZoom);
        }

        private void ConstrainPosition(ref Vector2 ioPosition, float inZoom)
        {
            Vector2 size;
            size.y = m_CameraPlane.ZoomedHeight(inZoom);
            size.x = size.y * m_Camera.aspect;

            for(int i = m_BoundsConstraints.Count - 1; i >= 0; --i)
                m_BoundsConstraints[i].Constrain(ref ioPosition, size);
        }

        #if UNITY_EDITOR

        private void UpdateDebug()
        {
            // TODO: Visualize camera state
        }

        #endif // UNITY_EDITOR

        #endregion // Camera Update

        #region Constraints

        public CameraConstraints.Bounds AddBounds()
        {
            CameraConstraints.Bounds bounds = new CameraConstraints.Bounds();
            m_BoundsConstraints.Add(bounds);
            return bounds;
        }

        public void RemoveBounds(CameraConstraints.Bounds inBounds)
        {
            m_BoundsConstraints.FastRemove(inBounds);
        }

        public CameraConstraints.Hint AddHint()
        {
            CameraConstraints.Hint hint = new CameraConstraints.Hint();
            m_HintConstraints.Add(hint);
            return hint;
        }

        public void RemoveHint(CameraConstraints.Hint inHint)
        {
            m_HintConstraints.FastRemove(inHint);
        }

        public void SetTarget(Transform inTransform)
        {
            if (inTransform == null)
            {
                ClearTarget();
                return;
            }

            m_TargetConstraint = new CameraConstraints.Target();
            m_TargetConstraint.PositionAt(inTransform);
            m_TargetConstraint.Zoom = 1;
            m_TargetConstraint.LerpFactor = m_DefaultLerp;
        }

        public void ClearTarget()
        {
            m_TargetConstraint = null;
        }

        #endregion // Constraints

        #region IService

        public override FourCC ServiceId()
        {
            return ServiceIds.Camera;
        }

        #endregion // IService
    }
}