using System;
using System.Collections;
using Aqua.Debugging;
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

        private struct CameraState
        {
            public Vector2 Position;
            public float Height;
            public float Zoom;

            public CameraState(Vector2 inPosition, float inHeight, float inZoom)
            {
                Position = inPosition;
                Height = inHeight;
                Zoom = inZoom;
            }

            static public void Lerp(in CameraState inA, in CameraState inB, ref CameraState outState, float inLerp)
            {
                outState.Position = Vector2.LerpUnclamped(inA.Position, inB.Position, inLerp);
                outState.Height = Mathf.LerpUnclamped(inA.Height, inB.Height, inLerp);
                outState.Zoom = Mathf.LerpUnclamped(inA.Zoom, inB.Zoom, inLerp);
            }
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private uint m_CacheFrameSkip = 1;

        #endregion // Inspector

        [NonSerialized] private Camera m_Camera;
        [NonSerialized] private CameraRig m_Rig;
        [NonSerialized] private CameraFOVPlane m_FOVPlane;
        [NonSerialized] private float m_LastCameraDistance;
        [NonSerialized] private Transform m_PositionRoot;

        [NonSerialized] private double m_Time;
        [NonSerialized] private uint m_NextId;
        [NonSerialized] private CameraMode m_Mode = CameraMode.Scripted;
        [NonSerialized] private bool m_Paused;

        private RingBuffer<CameraTargetData> m_TargetStack = new RingBuffer<CameraTargetData>();
        private RingBuffer<CameraPointData> m_Hints = new RingBuffer<CameraPointData>();
        private RingBuffer<CameraBoundsData> m_Bounds = new RingBuffer<CameraBoundsData>();
        private RingBuffer<CameraDriftData> m_Drifts = new RingBuffer<CameraDriftData>();

        private Routine m_ScriptedAnimation;

        public Camera Current { get { return m_Camera; } }
        public CameraRig Rig { get { return m_Rig; } }

        public Vector2 Position { get { return m_PositionRoot.localPosition; } }
        public float Zoom { get { return m_FOVPlane.Zoom; } }
        public float AspectRatio { get { return m_Camera.aspect; } }

        public CameraMode Mode { get { return m_Mode; } }

        #region Mode

        /// <summary>
        /// Resets the camera mode to the default rig settings.
        /// </summary>
        public void ResetCameraMode()
        {
            if (m_Rig.IsReferenceNull())
            {
                m_Mode = CameraMode.Scripted;
            }
            else
            {
                m_Mode = m_Rig.DefaultMode;
            }
        }

        #endregion // Mode

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
            }

            if (!m_FOVPlane.IsReferenceNull())
            {
                CameraFOVPlane.CameraSettings settings;
                m_FOVPlane.GetSettings(out settings);
                m_LastCameraDistance = settings.Distance;
            }

            CameraModifierFlags flags = CameraModifierFlags.Drift;

            switch(m_Mode)
            {
                case CameraMode.Hinted:
                {
                    CameraState state = GetCameraState(root, m_Camera, m_FOVPlane);
                    
                    flags = UpdateHintedCamera(ref state, deltaTime, CameraModifierFlags.All);

                    ApplyCameraState(state, root, m_Camera, m_FOVPlane, CameraPoseProperties.All);
                    break;
                }
            }

            if ((flags & CameraModifierFlags.Drift) != 0 && !m_Rig.IsReferenceNull() && !Accessibility.ReduceCameraMovement)
            {
                Vector2 offset = default(Vector2);
                ApplyDrift(ref offset, m_Drifts, m_Time);
                m_Rig.EffectsTransform.SetPosition(offset, Axis.XY, Space.Self);
            }
        }

        private CameraModifierFlags UpdateHintedCamera(ref CameraState ioState, float inDeltaTime, CameraModifierFlags inMask)
        {
            // with no target, smart camera does not work
            if (m_TargetStack.Count <= 0)
                return CameraModifierFlags.NoHints & inMask;

            CameraTargetData target = UpdateCameraTargetPosition();
            CameraModifierFlags flags = target.Flags & inMask;
            Vector2 targetPos = target.m_CachedPosition;

            UpdateHintedCaches(targetPos);

            TargetState targetState;
            targetState.Lerp = 0;
            targetState.Weight = 0;
            targetState.Offset = default(Vector2);
            targetState.Zoom = 0;

            if ((flags & CameraModifierFlags.Hints) != 0)
            {
                AccumulateHints(ref targetState, m_Hints, target.m_CachedPosition);
            }

            if (targetState.Weight < 1)
            {
                float amount = (1 - targetState.Weight);
                AccumulateHint(target, target.m_CachedPosition, amount, ref targetState);
            }

            Average(ref targetState);

            targetPos += targetState.Offset;

            float lerpAmount = TweenUtil.Lerp(targetState.Lerp, 1, inDeltaTime);
            float cameraZoom = Mathf.LerpUnclamped(ioState.Zoom, targetState.Zoom, lerpAmount);

            Vector2 cameraPos = ioState.Position;

            if ((flags & CameraModifierFlags.Bounds) != 0)
            {
                Vector2 size = FrameSize(m_Camera, m_FOVPlane, cameraZoom);
                ApplySoftConstraints(ref targetPos, size, m_Bounds);

                cameraPos = Vector2.LerpUnclamped(ioState.Position, targetPos, lerpAmount);
                ApplyHardConstraints(ref cameraPos, size, m_Bounds);
            }
            else
            {
                cameraPos = Vector2.LerpUnclamped(ioState.Position, targetPos, lerpAmount);
            }

            ioState.Position = cameraPos;
            ioState.Zoom = cameraZoom;

            return target.Flags;
        }

        private CameraTargetData UpdateCameraTargetPosition()
        {
            int targetIdx = m_TargetStack.Count - 1;
            if (targetIdx >= 0)
            {
                ref CameraTargetData target = ref m_TargetStack[targetIdx];
                CacheTarget(ref target);
                return target;
            }

            return default(CameraTargetData);
        }

        private void UpdateHintedCaches(Vector2 inTargetPosition)
        {
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

        private void ConstrainPositionToBounds()
        {
            if (m_Bounds.Count == 0)
                return;

            CacheBounds(m_Bounds);

            CameraState current = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            Vector2 size = FrameSize(m_Camera, m_FOVPlane, current.Zoom);
            ApplySoftConstraints(ref current.Position, size, m_Bounds);
            ApplyHardConstraints(ref current.Position, size, m_Bounds);
            ApplyCameraState(current, m_PositionRoot, m_Camera, m_FOVPlane, CameraPoseProperties.Position);
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

            ioPoint.m_CachedPosition = cachedPosition;

            float cachedWeight = ioPoint.WeightOffset;
            if (ioPoint.Weight != null)
                cachedWeight += ioPoint.Weight.Invoke(cachedPosition, inCameraTargetPosition);

            ioPoint.m_CachedWeight = cachedWeight;
        }

        static private void CacheTarget(ref CameraTargetData ioTarget)
        {
            Vector2 cachedPosition = ioTarget.Offset;
            if (ioTarget.Anchor != null)
                cachedPosition += (Vector2) ioTarget.Anchor.position;

            ioTarget.m_CachedPosition = cachedPosition;
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

            ioBounds.m_CachedRegion = cachedRegion;
        }

        #endregion // Caching

        #region Apply

        static private void AccumulateHints(ref TargetState ioState, RingBuffer<CameraPointData> inHints, Vector2 inTargetPosition)
        {
            TargetState settings = default;

            for(int i = 0, length = inHints.Count; i < length; i++)
            {
                AccumulateHint(inHints[i], inTargetPosition, ref settings);
            }

            ioState = settings;
        }

        static private void AccumulateHint(in CameraPointData inHint, Vector2 inCameraTargetPosition, ref TargetState ioState)
        {
            if (Mathf.Approximately(inHint.m_CachedWeight, 0))
                return;

            Vector2 vector = inHint.m_CachedPosition;
            VectorUtil.Subtract(ref vector, inCameraTargetPosition);
            VectorUtil.Multiply(ref vector, inHint.m_CachedWeight);

            float absWeight = Math.Abs(inHint.m_CachedWeight);

            ioState.Offset += vector;
            ioState.Zoom += inHint.Zoom * absWeight;
            ioState.Lerp += inHint.Lerp * absWeight;
            ioState.Weight += absWeight;
        }

        static private void AccumulateHint(in CameraTargetData inTarget, Vector2 inCameraTargetPosition, float inWeight, ref TargetState ioState)
        {
            if (Mathf.Approximately(inWeight, 0))
                return;

            Vector2 vector = inTarget.m_CachedPosition;
            VectorUtil.Subtract(ref vector, inCameraTargetPosition);
            VectorUtil.Multiply(ref vector, inWeight);

            float absWeight = Math.Abs(inWeight);

            ioState.Offset += vector;
            ioState.Zoom += inTarget.Zoom * absWeight;
            ioState.Lerp += inTarget.Lerp * absWeight;
            ioState.Weight += absWeight;
        }

        static private void ApplySoftConstraints(ref Vector2 ioOffset, Vector2 inSize, RingBuffer<CameraBoundsData> inBounds)
        {
            for(int i = 0, length = inBounds.Count; i < length; i++)
            {
                ref CameraBoundsData bounds = ref inBounds[i];
                Geom.Constrain(ref ioOffset, inSize, bounds.m_CachedRegion, bounds.SoftEdges);
            }
        }

        static private void ApplyHardConstraints(ref Vector2 ioOffset, Vector2 inSize, RingBuffer<CameraBoundsData> inBounds)
        {
            for(int i = 0, length = inBounds.Count; i < length; i++)
            {
                ref CameraBoundsData bounds = ref inBounds[i];
                Geom.Constrain(ref ioOffset, inSize, bounds.m_CachedRegion, bounds.HardEdges);
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

        static private void Average(ref TargetState ioState)
        {
            float weight = ioState.Weight;

            if (weight > 0 && weight != 1)
            {
                ioState.Offset /= weight;
                ioState.Zoom /= weight;
                ioState.Lerp /= weight;
            }
        }

        static private CameraState GetCameraState(Transform inRoot, Camera inCamera, CameraFOVPlane inPlane)
        {
            CameraState state;
            state.Position = inRoot.localPosition;
            if (!inPlane.IsReferenceNull())
            {
                state.Height = inPlane.Height;
                state.Zoom = inPlane.Zoom;
            }
            else
            {
                state.Height = inCamera.orthographicSize * 2;
                state.Zoom = 1;
            }
            return state;
        }

        static private void ApplyCameraState(in CameraState inState, Transform inRoot, Camera inCamera, CameraFOVPlane inPlane, CameraPoseProperties inProperties)
        {
            if ((inProperties & CameraPoseProperties.Position) != 0)
            {
                inRoot.SetPosition(inState.Position, Axis.XY, Space.Self);
            }

            if (!inPlane.IsReferenceNull())
            {
                if ((inProperties & CameraPoseProperties.Height) != 0)
                    inPlane.Height = inState.Height;
                if ((inProperties & CameraPoseProperties.Zoom) != 0)
                    inPlane.Zoom = inState.Zoom;
            }
            else
            {
                if ((inProperties & (CameraPoseProperties.HeightAndZoom)) != 0)
                {
                    inCamera.orthographicSize = inState.Height / (2 * inState.Zoom);
                }
            }
        }

        #endregion // Apply

        #region Handlers

        private void OnSceneUnload(SceneBinding inScene, object inContext)
        {
            ResetHinted();
            ResetScripted();
        }

        private void OnSceneLoaded(SceneBinding inScene, object inContext)
        {
            m_Time = 0;
            if (!m_Rig.IsReferenceNull())
            {
                if (m_Rig.InitialTarget)
                {
                    PushTarget(m_Rig.InitialTarget);
                }
            }

            PhysicsService.PerformCollisionChecks();
            SnapToTarget();
            ConstrainPositionToBounds();
        }

        internal void LocateCameraRig()
        {
            m_Rig = FindObjectOfType<CameraRig>();

            if (m_Rig == null)
            {
                m_Camera = Camera.main;
                m_Mode = CameraMode.Scripted;
                m_PositionRoot = m_Camera.transform;
                m_FOVPlane = m_Camera.GetComponent<CameraFOVPlane>();
            }
            else
            {
                m_Camera = m_Rig.Camera;
                m_Mode = m_Rig.DefaultMode;
                m_PositionRoot = m_Rig.RootTransform;
                m_FOVPlane = m_Rig.FOVPlane;
            }

            Assert.NotNull(m_Camera, "No main camera located for scene");
        }

        #endregion // Handlers

        #region Hinted

        /// <summary>
        /// Resets the hinted camera.
        /// </summary>
        public void ResetHinted()
        {
            m_TargetStack.Clear();
            m_Hints.Clear();
            m_Bounds.Clear();
            m_Drifts.Clear();
        }

        /// <summary>
        /// Sets the camera as a hinted camera.
        /// </summary>
        public void SetAsHinted()
        {
            if (m_Mode != CameraMode.Hinted)
            {
                m_ScriptedAnimation.Stop();
                m_Mode = CameraMode.Hinted;
                DebugService.Log(LogMask.Camera, "[CameraService] Setting into Hinted mode");
            }
        }

        /// <summary>
        /// Recenters on the current target.
        /// </summary>
        public void SnapToTarget(CameraPoseProperties inProperties = CameraPoseProperties.All)
        {
            if (m_TargetStack.Count == 0)
                return;

            CameraTargetData target = UpdateCameraTargetPosition();
            CameraState current = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            CameraState state = new CameraState(target.m_CachedPosition, current.Height, target.Zoom);
            ApplyCameraState(state, m_PositionRoot, m_Camera, m_FOVPlane, inProperties);
            m_ScriptedAnimation.Stop();
        }

        private uint NextId()
        {
            if (m_NextId == uint.MaxValue)
                return (m_NextId = 1);
            else
                return ++m_NextId;
        }

        #region Targets

        /// <summary>
        /// Pushes a new target, anchored around a transform, to the target stack.
        /// </summary>
        public ref CameraTargetData PushTarget(Transform inAnchor, float inLerp, float inZoom = 1, CameraModifierFlags inFlags = CameraModifierFlags.All)
        {
            CameraTargetData newTarget = default(CameraTargetData);
            newTarget.Id = NextId();
            newTarget.Anchor = inAnchor;
            newTarget.Lerp = inLerp;
            newTarget.Zoom = inZoom;
            newTarget.Flags = inFlags;
            m_TargetStack.PushBack(newTarget);

            DebugService.Log(LogMask.Camera, "[CameraService] Pushed new camera target '{0}'", inAnchor);
            return ref m_TargetStack[m_TargetStack.Count - 1];
        }

        /// <summary>
        /// Pushes a new target, anchored around a position, to the target stack.
        /// </summary>
        public ref CameraTargetData PushTarget(Vector2 inAnchor, float inLerp, float inZoom = 1, CameraModifierFlags inFlags = CameraModifierFlags.All)
        {
            CameraTargetData newTarget = default(CameraTargetData);
            newTarget.Id = NextId();
            newTarget.Anchor = null;
            newTarget.Offset = inAnchor;
            newTarget.Lerp = inLerp;
            newTarget.Zoom = inZoom;
            newTarget.Flags = inFlags;
            m_TargetStack.PushBack(newTarget);

            DebugService.Log(LogMask.Camera, "[CameraService] Pushed new camera target '{0}'", inAnchor);
            return ref m_TargetStack[m_TargetStack.Count - 1];
        }

        /// <summary>
        /// Pushes a new target, anchored around a camera target component, to the target stack.
        /// </summary>
        public ref CameraTargetData PushTarget(CameraTarget inTarget)
        {
            Assert.NotNull(inTarget);

            ref CameraTargetData targetData = ref PushTarget(inTarget.transform, inTarget.Lerp, inTarget.Zoom, inTarget.Flags);
            inTarget.m_TargetHandle = targetData.Id;
            return ref targetData;
        }
    
        /// <summary>
        /// Locates the target with the given id.
        /// </summary>
        public ref CameraTargetData FindTarget(uint inId)
        {
            for(int i = 0, length = m_TargetStack.Count; i < length; i++)
            {
                ref CameraTargetData target = ref m_TargetStack[i];
                if (target.Id == inId)
                    return ref target;
            }

            Assert.Fail("No camera target with id '{0}' was found", inId);
            return ref m_TargetStack[0];
        }

        /// <summary>
        /// Removes a target from the target stack.
        /// </summary>
        public bool PopTarget(uint inId)
        {
            if (inId == 0)
                return false;
                
            for(int i = 0, length = m_TargetStack.Count; i < length; i++)
            {
                ref CameraTargetData target = ref m_TargetStack[i];
                if (target.Id == inId)
                {
                    m_TargetStack.RemoveAt(i);

                    DebugService.Log(LogMask.Camera, "[CameraService] Removed camera target '{0}'", inId);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes a target from the target stack.
        /// </summary>
        public bool PopTarget(CameraTarget inTarget)
        {
            Assert.NotNull(inTarget);

            if (PopTarget(inTarget.m_TargetHandle))
            {
                inTarget.m_TargetHandle = 0;
                return true;
            }

            return false;
        }

        #endregion // Targets

        #region Bounds

        /// <summary>
        /// Adds a new camera bounds.
        /// </summary>
        public ref CameraBoundsData AddBounds(BoxCollider2D inAnchor, RectEdges inSoftEdges = RectEdges.All, RectEdges inHardEdges = RectEdges.None)
        {
            CameraBoundsData newBounds = default(CameraBoundsData);
            newBounds.Id = NextId();
            newBounds.Anchor2D = inAnchor;
            newBounds.Region = default(Rect);
            newBounds.SoftEdges = inSoftEdges;
            newBounds.HardEdges = inHardEdges;
            m_Bounds.PushBack(newBounds);
            return ref m_Bounds[m_Bounds.Count - 1];
        }

        /// <summary>
        /// Adds a new camera bounds.
        /// </summary>
        public ref CameraBoundsData AddBounds(Rect inRegion, RectEdges inSoftEdges = RectEdges.All, RectEdges inHardEdges = RectEdges.None)
        {
            CameraBoundsData newBounds = default(CameraBoundsData);
            newBounds.Id = NextId();
            newBounds.Anchor2D = null;
            newBounds.Region = inRegion;
            newBounds.SoftEdges = inSoftEdges;
            newBounds.HardEdges = inHardEdges;
            m_Bounds.PushBack(newBounds);
            return ref m_Bounds[m_Bounds.Count - 1];
        }
    
        /// <summary>
        /// Locates the bounds with the given id.
        /// </summary>
        public ref CameraBoundsData FindBounds(uint inId)
        {
            for(int i = 0, length = m_Bounds.Count; i < length; i++)
            {
                ref CameraBoundsData bounds = ref m_Bounds[i];
                if (bounds.Id == inId)
                    return ref bounds;
            }

            Assert.Fail("No camera bounds with id '{0}' was found", inId);
            return ref m_Bounds[0];
        }

        /// <summary>
        /// Removes a bounds from the bounds set.
        /// </summary>
        public bool RemoveBounds(uint inId)
        {
            for(int i = 0, length = m_Bounds.Count; i < length; i++)
            {
                ref CameraBoundsData bounds = ref m_Bounds[i];
                if (bounds.Id == inId)
                {
                    m_Bounds.FastRemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        #endregion // Bounds

        #region Hint

        /// <summary>
        /// Adds a new camera hint.
        /// </summary>
        public ref CameraPointData AddHint(Transform inAnchor, float inLerp, float inWeight, float inZoom = 1)
        {
            CameraPointData newHint = default(CameraPointData);
            newHint.Id = NextId();
            newHint.Anchor = inAnchor;
            newHint.Offset = default(Vector2);
            newHint.Weight = null;
            newHint.WeightOffset = inWeight;
            newHint.Zoom = inZoom;
            newHint.Lerp = inLerp;
            m_Hints.PushBack(newHint);
            return ref m_Hints[m_Hints.Count - 1];
        }

        /// <summary>
        /// Adds a new camera hint.
        /// </summary>
        public ref CameraPointData AddHint(Vector2 inAnchor, float inLerp, float inWeight, float inZoom = 1)
        {
            CameraPointData newHint = default(CameraPointData);
            newHint.Id = NextId();
            newHint.Anchor = null;
            newHint.Offset = inAnchor;
            newHint.Weight = null;
            newHint.WeightOffset = inWeight;
            newHint.Zoom = inZoom;
            newHint.Lerp = inLerp;
            m_Hints.PushBack(newHint);
            return ref m_Hints[m_Hints.Count - 1];
        }
    
        /// <summary>
        /// Locates the hint with the given id.
        /// </summary>
        public ref CameraPointData FindHint(uint inId)
        {
            for(int i = 0, length = m_Hints.Count; i < length; i++)
            {
                ref CameraPointData hint = ref m_Hints[i];
                if (hint.Id == inId)
                    return ref hint;
            }

            Assert.Fail("No camera hint with id '{0}' was found", inId);
            return ref m_Hints[0];
        }

        /// <summary>
        /// Removes a hint from the hint set.
        /// </summary>
        public bool RemoveHint(uint inId)
        {
            for(int i = 0, length = m_Hints.Count; i < length; i++)
            {
                ref CameraPointData hint = ref m_Hints[i];
                if (hint.Id == inId)
                {
                    m_Hints.FastRemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        #endregion // Hint

        #region Drifts

        /// <summary>
        /// Adds a new camera drift.
        /// </summary>
        public ref CameraDriftData AddDrift(Vector2 inDistance, Vector2 inPeriod, Vector2 inOffset)
        {
            CameraDriftData newDrift = default(CameraDriftData);
            newDrift.Id = NextId();
            newDrift.Distance = inDistance;
            newDrift.Period = inPeriod;
            newDrift.Offset = inOffset;
            m_Drifts.PushBack(newDrift);
            return ref m_Drifts[m_Drifts.Count - 1];
        }
    
        /// <summary>
        /// Locates the drift with the given id.
        /// </summary>
        public ref CameraDriftData FindDrift(uint inId)
        {
            for(int i = 0, length = m_Drifts.Count; i < length; i++)
            {
                ref CameraDriftData drift = ref m_Drifts[i];
                if (drift.Id == inId)
                    return ref drift;
            }

            Assert.Fail("No camera drift with id '{0}' was found", inId);
            return ref m_Drifts[0];
        }

        /// <summary>
        /// Removes a drift from the drift set.
        /// </summary>
        public bool RemoveDrift(uint inId)
        {
            for(int i = 0, length = m_Drifts.Count; i < length; i++)
            {
                ref CameraDriftData drift = ref m_Drifts[i];
                if (drift.Id == inId)
                {
                    m_Drifts.FastRemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        #endregion // Drifts

        #endregion // Hinted

        #region Scripted

        /// <summary>
        /// Resets the scripted camera.
        /// </summary>
        public void ResetScripted()
        {
            m_ScriptedAnimation.Stop();
        }

        /// <summary>
        /// Sets the camera as a scripted camera.
        /// </summary>
        public void SetAsScripted()
        {
            if (m_Mode != CameraMode.Scripted)
            {
                m_Mode = CameraMode.Scripted;
                DebugService.Log(LogMask.Camera, "[CameraService] Setting into Scripted mode");
            }
        }

        /// <summary>
        /// Recenters on the current target.
        /// </summary>
        public IEnumerator RecenterOnTarget(float inDuration, Curve inCurve = Curve.Smooth)
        {
            SetAsScripted();

            if (m_TargetStack.Count == 0)
                return null;

            CameraTargetData target = UpdateCameraTargetPosition();
            return MoveToPosition(target.m_CachedPosition, target.Zoom, inDuration, inCurve);
        }

        /// <summary>
        /// Snaps to a specific position.
        /// </summary>
        public void SnapToPosition(Vector2 inPosition, float? inZoom = null)
        {
            SetAsScripted();

            CameraState currentState = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            CameraState newState = new CameraState(inPosition, currentState.Height, inZoom.GetValueOrDefault(currentState.Zoom));
            ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, CameraPoseProperties.PosAndZoom);
            m_ScriptedAnimation.Stop();
        }

        /// <summary>
        /// Snaps to a specific camera pose.
        /// </summary>
        public void SnapToPose(CameraPose inPose)
        {
            Assert.NotNull(inPose);
            SnapToPose(inPose, inPose.Properties);
        }

        /// <summary>
        /// Snaps to a specific camera pose.
        /// </summary>
        public void SnapToPose(CameraPose inPose, CameraPoseProperties inProperties)
        {
            SetAsScripted();

            Assert.NotNull(inPose);

            CameraState newState = new CameraState(inPose.transform.position, inPose.Height, inPose.Zoom);
            if (!m_FOVPlane.IsReferenceNull() && inPose.Target != null)
                m_FOVPlane.Target = inPose.Target;
            
            ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, inProperties);
            m_ScriptedAnimation.Stop();
        }

        /// <summary>
        /// Moves the camera to a specific position.
        /// </summary>
        public IEnumerator MoveToPosition(Vector2 inPosition, float? inZoom, float inDuration, Curve inCurve = Curve.Smooth)
        {
            SetAsScripted();

            CameraPoseProperties properties = CameraPoseProperties.Position;
            if (inZoom.HasValue)
                properties |= CameraPoseProperties.Zoom;
            
            CameraState currentState = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            CameraState newState = new CameraState(inPosition, currentState.Height, inZoom.GetValueOrDefault(currentState.Zoom));
            if (inDuration <= 0)
            {
                ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, properties);
                m_ScriptedAnimation.Stop();
                return null;
            }

            m_ScriptedAnimation.Replace(this, MoveCameraTween(currentState, newState, properties, inDuration, inCurve));
            return m_ScriptedAnimation.Wait();
        }

        /// <summary>
        /// Moves the camera to a specific pose.
        /// </summary>
        public IEnumerator MoveToPose(CameraPose inPose, float inDuration, Curve inCurve = Curve.Smooth)
        {
            Assert.NotNull(inPose);

            SetAsScripted();

            if (!m_FOVPlane.IsReferenceNull() && inPose.Target != null)
                SetTargetSeamless(m_Camera, m_FOVPlane, inPose.Target);

            CameraState currentState = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            CameraState newState = new CameraState(inPose.transform.position, inPose.Height, inPose.Zoom);

            if (inDuration <= 0)
            {
                ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, inPose.Properties);
                m_ScriptedAnimation.Stop();
                return null;
            }

            m_ScriptedAnimation.Replace(this, MoveCameraTween(currentState, newState, inPose.Properties, inDuration, inCurve));
            return m_ScriptedAnimation.Wait();
        }

        private IEnumerator MoveCameraTween(CameraState inInitialState, CameraState inTarget, CameraPoseProperties inProperties, float inDuration, Curve inCurve)
        {
            return Tween.ZeroToOne((f) => {
                CameraState newState = default;
                CameraState.Lerp(inInitialState, inTarget, ref newState, f);
                ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, inProperties);
            }, inDuration).Ease(inCurve);
        }

        #endregion // Scripted

        #region Positions

        /// <summary>
        /// Casts a transform position to a position on the gameplay plane.
        /// </summary>
        public Vector3 GameplayPlanePosition(Transform inTransform)
        {
            Vector3 position;
            m_Camera.TryCastPositionToTargetPlane(inTransform, m_FOVPlane.Target, out position);
            return position;
        }

        /// <summary>
        /// Casts a world position to a position on the gameplay plane.
        /// </summary>
        public Vector3 GameplayPlanePosition(Vector3 inWorldPos)
        {
            Vector3 position;
            m_Camera.TryCastPositionToTargetPlane(inWorldPos, m_FOVPlane.Target, out position);
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
        /// Casts from a screen position to a world position on the current camera plane.
        /// </summary>
        public Vector3 ScreenToGameplayPosition(Vector2 inScreenPos)
        {
            Vector3 screenPos = inScreenPos;
            screenPos.z = 1;

            Plane p = new Plane(-m_Camera.transform.forward, m_FOVPlane.Target.position);
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
            SceneHelper.OnSceneUnload += OnSceneUnload;
        }

        protected override void Shutdown()
        {
            SceneHelper.OnSceneLoaded -= OnSceneLoaded;
            SceneHelper.OnSceneUnload -= OnSceneUnload;
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

        static private Vector2 FrameSize(Camera inCamera, CameraFOVPlane inPlane, float inZoom)
        {
            Vector2 size;
            size.y = inPlane.ZoomedHeight(inZoom);
            size.x = inCamera.aspect * size.y;
            return size;
        }

        static private void SetTargetSeamless(Camera inCamera, CameraFOVPlane ioPlane, Transform inTarget)
        {
            if (inTarget == null)
            {
                ioPlane.Target = null;
                return;
            }

            CameraFOVPlane.CameraSettings current;
            ioPlane.GetSettings(out current);

            float newDist;
            inCamera.TryGetDistanceToObjectPlane(inTarget, out newDist);

            float newZoom = current.Zoom * current.Distance / newDist;

            ioPlane.Target = inTarget;
            ioPlane.Zoom = newZoom;
        }

        #endregion // Utils
    }
}