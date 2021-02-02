using System;
using System.Collections.Generic;
using Aqua;
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
        [SerializeField] private double m_Time = 0;

        #endregion // Inspector

        [NonSerialized] private List<CameraConstraints.Bounds> m_BoundsConstraints = new List<CameraConstraints.Bounds>(8);
        [NonSerialized] private List<CameraConstraints.Hint> m_HintConstraints = new List<CameraConstraints.Hint>(8);
        [NonSerialized] private CameraConstraints.Target m_TargetConstraint;
        [NonSerialized] private List<CameraConstraints.Drift> m_DriftConstraints = new List<CameraConstraints.Drift>(8);

        #region Events

        private void Awake()
        {
            
        }

        private void LateUpdate()
        {
            m_Time += Time.deltaTime;
            DriveCamera();
        }

        #endregion // Events

        public Vector3 GameplayPlanePosition(Transform inTransform)
        {
            Vector3 pos;
            m_Camera.TryCastPositionToTargetPlane(inTransform, m_CameraPlane.Target, out pos);
            return pos;
        }

        public Vector3 GameplayPlanePosition(Vector3 inWorldPos)
        {
            Vector3 pos;
            m_Camera.TryCastPositionToTargetPlane(inWorldPos, m_CameraPlane.Target, out pos);
            return pos;
        }

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

        /// <summary>
        /// Returns the scale needed to have a consistent scale for the gameplay axis.
        /// </summary>
        public float GameplayPlaneScaleFactor(Transform inTransform)
        {
            Vector2 viewpointPos = new Vector2(0.5f, 0.5f);

            Plane p = new Plane(-m_Camera.transform.forward, inTransform.position);
            Ray r = m_Camera.ViewportPointToRay(viewpointPos);

            float dist;
            p.Raycast(r, out dist);

            CameraFOVPlane.CameraSettings currentSettings;
            m_CameraPlane.GetSettings(out currentSettings);

            return dist / currentSettings.Distance;
        }

        #region Camera Update

        private void DriveCamera()
        {
            Vector2 currentPos = m_RootTransform.localPosition;
            float zoom = m_CameraPlane.Zoom;

            UpdateSmartCamera(ref currentPos, ref zoom);

            m_CameraPlane.Zoom = zoom;
            m_RootTransform.SetPosition(currentPos, Axis.XY, Space.Self);

            Vector2 offset = default(Vector2);
            ApplyDrift(ref offset);

            m_EffectsTransform.SetPosition(offset, Axis.XY, Space.Self);
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

            ConstrainPositionSoft(ref targetPos, ioZoom);

            #if UNITY_EDITOR
            RenderInlineDebug(targetPos);
            #endif // UNITY_EDITOR

            lerp = TweenUtil.Lerp(lerp, 1, Routine.DeltaTime);
            ioPos = Vector2.LerpUnclamped(ioPos, targetPos, lerp);
            ioZoom = Mathf.LerpUnclamped(ioZoom, targetZoom, lerp);

            ConstrainPositionHard(ref ioPos, ioZoom);
        }

        private void ConstrainPositionSoft(ref Vector2 ioPosition, float inZoom)
        {
            Vector2 size;
            size.y = m_CameraPlane.ZoomedHeight(inZoom);
            size.x = size.y * m_Camera.aspect;

            for(int i = m_BoundsConstraints.Count - 1; i >= 0; --i)
                m_BoundsConstraints[i].ConstrainSoft(ref ioPosition, size);
        }

        private void ConstrainPositionHard(ref Vector2 ioPosition, float inZoom)
        {
            Vector2 size;
            size.y = m_CameraPlane.ZoomedHeight(inZoom);
            size.x = size.y * m_Camera.aspect;

            for(int i = m_BoundsConstraints.Count - 1; i >= 0; --i)
                m_BoundsConstraints[i].ConstrainHard(ref ioPosition, size);
        }

        private void ApplyDrift(ref Vector2 ioOffset)
        {
            for(int i = m_DriftConstraints.Count - 1; i >= 0; --i)
            {
                CameraConstraints.Drift drift = m_DriftConstraints[i];
                float x = drift.Distance.x * (float) Math.Sin(Math.PI * 2 * ((drift.Offset.x + m_Time) % drift.Period.x) / drift.Period.x);
                float y = drift.Distance.y * (float) Math.Cos(Math.PI * 2 * ((drift.Offset.y + m_Time) % drift.Period.y) / drift.Period.y); 
                ioOffset.x += x;
                ioOffset.y += y;
            }
        }

        #if UNITY_EDITOR

        private void UpdateDebug()
        {
            // TODO: Visualize camera state
        }

        private void RenderInlineDebug(Vector3 inTarget)
        {
            Vector3 selfPos = m_RootTransform.position;
            Vector3 targetPos = inTarget;
            targetPos.z = selfPos.z;
            Vector3 dir = targetPos - selfPos;
            if (dir.sqrMagnitude > 0.1f)
            {
                Debug.DrawRay(m_RootTransform.position, dir, Color.yellow, 0);
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Camera Update

        #region Constraints

        public CameraConstraints.Bounds AddBounds(string inName)
        {
            CameraConstraints.Bounds bounds = new CameraConstraints.Bounds();
            bounds.Name = inName;
            m_BoundsConstraints.Add(bounds);

            Debug.LogFormat("[CameraCtrl] Added bounding region '{0}'", inName);
            
            return bounds;
        }

        public void RemoveBounds(CameraConstraints.Bounds inBounds)
        {
            m_BoundsConstraints.FastRemove(inBounds);
            Debug.LogFormat("[CameraCtrl] Removed bounding region '{0}'", inBounds.Name);
        }

        public CameraConstraints.Hint AddHint(string inName)
        {
            CameraConstraints.Hint hint = new CameraConstraints.Hint();
            hint.Name = inName;
            m_HintConstraints.Add(hint);

            Debug.LogFormat("[CameraCtrl] Added camera hint '{0}'", inName);
            return hint;
        }

        public void RemoveHint(CameraConstraints.Hint inHint)
        {
            m_HintConstraints.FastRemove(inHint);
            Debug.LogFormat("[CameraCtrl] Removed camera hint '{0}'", inHint.Name);
        }

        public CameraConstraints.Drift AddDrift(string inName)
        {
            CameraConstraints.Drift drift = new CameraConstraints.Drift();
            drift.Name = inName;
            m_DriftConstraints.Add(drift);

            Debug.LogFormat("[CameraCtrl] Added camera drift '{0}'", inName);
            return drift;
        }

        public void RemoveDrift(CameraConstraints.Drift inDrift)
        {
            m_DriftConstraints.FastRemove(inDrift);
            Debug.LogFormat("[CameraCtrl] Removed camera drift '{0}'", inDrift.Name);
        }

        public void SetTarget(Transform inTransform)
        {
            if (inTransform == null)
            {
                ClearTarget();
                return;
            }

            m_TargetConstraint = new CameraConstraints.Target();
            m_TargetConstraint.Name = inTransform.name;
            m_TargetConstraint.PositionAt(inTransform);
            m_TargetConstraint.Zoom = 1;
            m_TargetConstraint.LerpFactor = m_DefaultLerp;

            Debug.LogFormat("[CameraCtrl] Started tracking target '{0}'", m_TargetConstraint.Name);
        }

        public void ClearTarget()
        {
            if (m_TargetConstraint != null)
            {
                Debug.LogFormat("[CameraCtrl] Stopped tracking target '{0}'", m_TargetConstraint.Name);
                m_TargetConstraint = null;
            }
        }

        #endregion // Constraints
    }
}