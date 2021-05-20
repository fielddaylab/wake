using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua.Cameras
{
    public class CameraService : ServiceBehaviour, IPauseable
    {
        #region Types

        private struct TargetState
        {
            public Vector2 Offset;
            public float Zoom;
            public float Lerp;
            public float Weight;
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private uint m_CacheFrameSkip = 1;

        #endregion // Inspector

        [NonSerialized] private Camera m_Camera;
        [NonSerialized] private CameraRig m_Rig;
        [NonSerialized] private float m_LastCameraDistance;

        [NonSerialized] private double m_Time;
        [NonSerialized] private CameraMode m_Mode;
        [NonSerialized] private bool m_Paused;

        [NonSerialized] private RingBuffer<CameraPointData> m_TargetStack = new RingBuffer<CameraPointData>();
        [NonSerialized] private RingBuffer<CameraPointData> m_Hints = new RingBuffer<CameraPointData>();
        [NonSerialized] private RingBuffer<CameraBoundsData> m_Bounds = new RingBuffer<CameraBoundsData>();
        [NonSerialized] private RingBuffer<CameraDriftData> m_Drifts = new RingBuffer<CameraDriftData>();

        public Camera Current { get { return m_Camera; } }
        public CameraRig Rig { get { return m_Rig; } }

        #region Positions

        /// <summary>
        /// Casts a transform position to a position on the gameplay plane.
        /// </summary>
        public Vector3 GameplayPlanePosition(Transform inTransform)
        {
            Vector3 position;
            m_Camera.TryCastPositionToTargetPlane(inTransform, m_Rig.FOVPlane.Target, out position);
            return position;
        }

        /// <summary>
        /// Casts a world position to a position on the gameplay plane.
        /// </summary>
        public Vector3 GameplayPlanePosition(Vector3 inWorldPos)
        {
            Vector3 position;
            m_Camera.TryCastPositionToTargetPlane(inWorldPos, m_Rig.FOVPlane.Target, out position);
            return position;
        }

        /// <summary>
        /// Casts from a screen position to a world position on the given plane.
        /// </summary>
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

            return dist / m_LastCameraDistance;
        }

        #endregion // Positions

        #region Update

        private void LateUpdate()
        {
            if (m_Paused || Services.State.IsLoadingScene() || Time.timeScale <= 0)
                return;

            float deltaTime = Time.deltaTime;
            m_Time += deltaTime;

            Transform root = m_Camera.transform;

            if (!m_Rig.IsReferenceNull())
            {
                root = m_Rig.RootTransform;

                CameraFOVPlane.CameraSettings settings;
                m_Rig.FOVPlane.GetSettings(out settings);
                m_LastCameraDistance = settings.Distance;
            }

            switch(m_Mode)
            {
                case CameraMode.Hinted:
                {
                    Vector2 position = (Vector2) root.localPosition;
                    float zoom = m_Rig.IsReferenceNull() ? 1 : m_Rig.FOVPlane.Zoom;
                    
                    UpdateHintedCamera(ref position, ref zoom);

                    root.SetPosition(position, Axis.XY, Space.Self);

                    if (!m_Rig.IsReferenceNull())
                        m_Rig.FOVPlane.Zoom = zoom;
                    break;
                }
            }

            if (!m_Rig.IsReferenceNull())
            {
                Vector2 offset = default(Vector2);
                ApplyDrift(ref offset, m_Drifts, m_Time);
                m_Rig.EffectsTransform.SetPosition(offset, Axis.XY, Space.Self);
            }
        }

        private void UpdateHintedCamera(ref Vector2 ioPosition, ref float ioZoom)
        {
            // with no target, smart camera does not work
            if (m_TargetStack.Count <= 0)
                return;

            Vector2 targetPos = ioPosition;
            float targetZoom = ioZoom;

            UpdateHintedCaches(targetPos);
        }

        private void UpdateHintedCaches(Vector2 inTargetPosition)
        {
            int targetIdx = m_TargetStack.Count - 1;
            if (targetIdx >= 0)
            {
                CachePoint(ref m_TargetStack[targetIdx], inTargetPosition);
            }

            int frameCount = Time.frameCount % (int) (m_CacheFrameSkip + 1);

            // update points and bounds on alternating frames if specified
            if (m_CacheFrameSkip == 0)
            {
                CachePoints(m_Hints, inTargetPosition);
                CacheBounds(m_Bounds);
            }
            else if (frameCount == 0)
            {
                CacheBounds(m_Bounds);
            }
            else if (frameCount == 1)
            {
                CachePoints(m_Hints, inTargetPosition);
            }
        }

        #endregion // Update

        #region Caching

        static private void CachePoints(RingBuffer<CameraPointData> inPoints, Vector2 inCameraTargetPosition)
        {
            for(int i = 0, length = inPoints.Count; i < length; i++)
            {
                CachePoint(ref inPoints[i], inCameraTargetPosition);
            }
        }

        static private void CachePoint(ref CameraPointData ioPoint, Vector2 inCameraTargetPosition)
        {
            Vector2 cachedPosition = ioPoint.Offset;
            if (ioPoint.Anchor != null)
                cachedPosition += (Vector2) ioPoint.Anchor.position;

            ioPoint.CachedPosition = cachedPosition;

            float cachedWeight = ioPoint.WeightOffset;
            if (ioPoint.Weight != null)
                cachedWeight += ioPoint.Weight.Invoke(cachedPosition, inCameraTargetPosition);

            ioPoint.CachedWeight = cachedWeight;
        }

        static private void CacheBounds(RingBuffer<CameraBoundsData> inBounds)
        {
            for(int i = 0, length = inBounds.Count; i < length; i++)
            {
                CacheBounds(ref inBounds[i]);
            }
        }

        static private void CacheBounds(ref CameraBoundsData ioBounds)
        {
            Rect cachedRegion = default(Rect);
            if (ioBounds.Anchor2D != null)
            {
                cachedRegion.size = ioBounds.Anchor2D.size;
                Vector2 center = (Vector2) ioBounds.Anchor2D.transform.position + ioBounds.Anchor2D.offset;
                cachedRegion.center = center;
            }
            else
            {
                cachedRegion = ioBounds.Region;
            }

            ioBounds.CachedRegion = cachedRegion;
        }

        #endregion // Caching

        #region Apply

        static private void AccumulateHints(ref TargetState ioState, RingBuffer<CameraPointData> inHints, Vector2 inCameraTargetPosition)
        {
            TargetState settings = default;

            for(int i = 0, length = inHints.Count; i < length; i++)
            {
                AccumulateHint(inHints[i], inCameraTargetPosition, ref settings);
            }

            float weight = ioState.Weight;

            if (ioState.Weight > 0)
            {
                settings.Offset /= weight;
                settings.Zoom /= weight;
                settings.Lerp /= weight;
            }
        }

        static private void AccumulateHint(in CameraPointData inHint, Vector2 inCameraTargetPosition, ref TargetState ioState)
        {
            if (Mathf.Approximately(inHint.CachedWeight, 0))
                return;

            Vector2 vector = inHint.CachedPosition;
            VectorUtil.Subtract(ref vector, inCameraTargetPosition);
            VectorUtil.Multiply(ref vector, inHint.CachedWeight);

            float absWeight = Math.Abs(inHint.CachedWeight);

            ioState.Offset += vector;
            ioState.Zoom += inHint.Zoom * absWeight;
            ioState.Lerp += inHint.Lerp * absWeight;
            ioState.Weight += absWeight;
        }

        static private void ApplySoftConstraints(ref Vector2 ioOffset, Vector2 inSize, RingBuffer<CameraBoundsData> inBounds)
        {
            for(int i = 0, length = inBounds.Count; i < length; i++)
            {
                ref CameraBoundsData bounds = ref inBounds[i];
                Geom.Constrain(ref ioOffset, inSize, bounds.CachedRegion, bounds.SoftEdges);
            }
        }

        static private void ApplyHardConstraints(ref Vector2 ioOffset, Vector2 inSize, RingBuffer<CameraBoundsData> inBounds)
        {
            for(int i = 0, length = inBounds.Count; i < length; i++)
            {
                ref CameraBoundsData bounds = ref inBounds[i];
                Geom.Constrain(ref ioOffset, inSize, bounds.CachedRegion, bounds.HardEdges);
            }
        }

        static private void ApplyDrift(ref Vector2 ioOffset, RingBuffer<CameraDriftData> inDrifts, double inTime)
        {
            for(int i = 0, length = inDrifts.Count; i < length; i++)
            {
                ref CameraDriftData drift = ref inDrifts[i];
                float x = drift.Distance.x * (float) Math.Sin(Math.PI * 2 * ((drift.Offset.x + inTime) % drift.Period.x) / drift.Period.x);
                float y = drift.Distance.y * (float) Math.Sin(Math.PI * 2 * ((drift.Offset.y + inTime) % drift.Period.y) / drift.Period.y);
                ioOffset.x += x;
                ioOffset.y += y;
            }
        }

        #endregion // Apply

        #region Handlers

        private void OnSceneLoaded(SceneBinding inScene, object inContext)
        {
            m_Time = 0;
        }

        #endregion // Handlers

        #region IPauseable

        bool IPauseable.IsPaused()
        {
            return m_Paused;
        }

        void IPauseable.Pause()
        {
            m_Paused = true;
            enabled = false;
        }

        void IPauseable.Resume()
        {
            m_Paused = false;
            enabled = true;
        }

        #endregion // IPauseable

        #region IService

        protected override void Initialize()
        {
            base.Initialize();

            SceneHelper.OnSceneLoaded += OnSceneLoaded;
        }

        protected override void Shutdown()
        {
            SceneHelper.OnSceneLoaded -= OnSceneLoaded;
            base.Shutdown();
        }

        #endregion // IService
    
        #region Utils

        static private void Mask(ref Vector3 ioPosition, Axis inAxis)
        {
            if ((inAxis & Axis.X) == 0)
                ioPosition.x = 0;
            if ((inAxis & Axis.Y) == 0)
                ioPosition.y = 0;
            if ((inAxis & Axis.Z) == 0)
                ioPosition.z = 0;
        }

        #endregion // Utils
    }
}