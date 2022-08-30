#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Collections;
using Aqua.Debugging;
using BeauRoutine;
using BeauRoutine.Splines;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aqua.Cameras
{
    public class CameraService : ServiceBehaviour, IPauseable
    {
        private const float DesiredWidth = 1024;
        private const float DesiredHeight = 660;

        #region Types

        private struct TargetState
        {
            public Vector3 Offset;
            public Vector3 Look;
            public float Zoom;
            public float Lerp;
            public float Weight;
        }

        public struct CameraState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public float Height;
            public float Zoom;
            public float FieldOfView;

            public CameraState(Vector3 inPosition, Quaternion inRotation, float inHeight, float inZoom, float inFieldOfView)
            {
                Position = inPosition;
                Rotation = inRotation;
                Height = inHeight;
                Zoom = inZoom;
                FieldOfView = inFieldOfView;
            }

            static public void Lerp(in CameraState inA, in CameraState inB, ref CameraState outState, float inLerp)
            {
                outState.Position = Vector3.LerpUnclamped(inA.Position, inB.Position, inLerp);
                outState.Rotation = Quaternion.SlerpUnclamped(inA.Rotation, inB.Rotation, inLerp);
                outState.Height = Mathf.LerpUnclamped(inA.Height, inB.Height, inLerp);
                outState.Zoom = Mathf.LerpUnclamped(inA.Zoom, inB.Zoom, inLerp);
                outState.FieldOfView = Mathf.LerpUnclamped(inA.FieldOfView, inB.FieldOfView, inLerp);
            }

            static public void Lerp<T>(in CameraState inA, in CameraState inB, ref CameraState outState, float inLerp, T inPositionSpline)
                where T : ISpline
            {
                outState.Position = inPositionSpline.GetPoint(inLerp);
                outState.Rotation = Quaternion.SlerpUnclamped(inA.Rotation, inB.Rotation, inLerp);
                outState.Height = Mathf.LerpUnclamped(inA.Height, inB.Height, inLerp);
                outState.Zoom = Mathf.LerpUnclamped(inA.Zoom, inB.Zoom, inLerp);
                outState.FieldOfView = Mathf.LerpUnclamped(inA.FieldOfView, inB.FieldOfView, inLerp);
            }
        }

        public struct PlanePositionHelper
        {
            public Camera Camera;
            public Ray CenterRay;
            public Plane GameplayPlane;
            public Matrix4x4 ViewportToWorld;
            public float GameplayDistance;

            public Vector3 CastToPlane(Transform inTransform)
            {
                Vector3 transformPos = inTransform.position;
                Vector3 from = Camera.WorldToViewportPoint(transformPos, Camera.MonoOrStereoscopicEye.Mono);

                from.z = 0;
                Vector3 near = ViewportToWorld.MultiplyPoint(from);
                
                from.z = 1;
                Vector3 far = ViewportToWorld.MultiplyPoint(from);

                Ray ray = new Ray(near, far - near);
                Vector3 planeNormal = GameplayPlane.normal;

                float gameplayDist = (-Vector3.Dot(ray.origin, planeNormal) - GameplayPlane.distance) / Vector3.Dot(ray.direction, planeNormal);
                return near + (ray.direction * gameplayDist);
            }

            public Vector3 CastToPlane(Transform inTransform, out float outDistanceRatio)
            {
                Vector3 transformPos = inTransform.position;
                Vector3 from = Camera.WorldToViewportPoint(transformPos, Camera.MonoOrStereoscopicEye.Mono);

                Plane transformPlane = new Plane(GameplayPlane.normal, transformPos);
                
                from.z = 0;
                Vector3 near = ViewportToWorld.MultiplyPoint(from);
                
                from.z = 1;
                Vector3 far = ViewportToWorld.MultiplyPoint(from);

                Ray ray = new Ray(near, far - near);
                Vector3 planeNormal = GameplayPlane.normal;

                float gameplayDist = (-Vector3.Dot(ray.origin, planeNormal) - GameplayPlane.distance) / Vector3.Dot(ray.direction, planeNormal);
                float planeDist = (-Vector3.Dot(CenterRay.origin, planeNormal) - GameplayPlane.distance) / Vector3.Dot(CenterRay.direction, planeNormal);

                // Debug.DrawRay(ray.origin, ray.direction * transformDist, Color.yellow, 0.2f);

                outDistanceRatio = planeDist / GameplayDistance;
                return near + (ray.direction * gameplayDist);
            }
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private uint m_CacheFrameSkip = 1;
        [SerializeField] private Color m_ClipOutsideColor = Color.clear;

        #endregion // Inspector

        [NonSerialized] private Camera m_Camera;
        [NonSerialized] private CameraRig m_Rig;
        [NonSerialized] private CameraFOVPlane m_FOVPlane;
        [NonSerialized] private CameraRenderScale m_RenderScale;
        [NonSerialized] private float m_LastCameraDistance;
        [NonSerialized] private Transform m_PositionRoot;
        [NonSerialized] private Axis m_Axis = Axis.XY;

        [NonSerialized] private double m_Time;
        [NonSerialized] private uint m_NextId;
        [NonSerialized] private CameraMode m_Mode = CameraMode.Scripted;
        [NonSerialized] private bool m_Paused;
        [NonSerialized] private bool m_CacheDirty;

        [NonSerialized] private Plane m_LastGameplayPlane;
        [NonSerialized] private Vector3 m_LastGameplayPlaneCenter;
        [NonSerialized] private float m_LastGameplayPlaneDistance;
        [NonSerialized] private Ray m_LastCenterRay;
        [NonSerialized] private Matrix4x4 m_LastVPMatrix;
        [NonSerialized] private Matrix4x4 m_LastVPMatrixInv;
        [NonSerialized] private Rect m_LastScreenAspectClip;

        #if DEVELOPMENT
        [NonSerialized] private CameraState m_LastAssignedState;
        #endif // DEVELOPMENT

        private RingBuffer<CameraTargetData> m_TargetStack = new RingBuffer<CameraTargetData>();
        private RingBuffer<CameraPointData> m_Hints = new RingBuffer<CameraPointData>();
        private RingBuffer<CameraBoundsData> m_Bounds = new RingBuffer<CameraBoundsData>();
        private RingBuffer<CameraDriftData> m_Drifts = new RingBuffer<CameraDriftData>();
        private RingBuffer<CameraShakeData> m_Shakes = new RingBuffer<CameraShakeData>();

        private Routine m_ScriptedAnimation;
        [NonSerialized] private CameraFOVMode m_FOVMode;

        public Camera Current { get { return m_Camera; } }
        public CameraRig Rig { get { return m_Rig; } }
        public Transform RootTransform { get { return m_PositionRoot; } }

        public Vector2 Position { get { return m_PositionRoot.localPosition; } }
        public float Zoom { get { return m_FOVPlane.Zoom; } }
        public float AspectRatio { get { return m_Camera.aspect; } }

        public Vector3 FocusPosition { get { return m_LastGameplayPlaneCenter; }}

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
            if (m_Paused || Script.IsLoading || Time.timeScale <= 0)
                return;

            float deltaTime = Time.deltaTime;
            m_Time += deltaTime;

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
                    CameraState state = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
                    
                    flags = UpdateHintedCamera(ref state, deltaTime, CameraModifierFlags.All);

                    ApplyCameraState(state, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, CameraPoseProperties.All, m_Axis);
                    break;
                }
            }

            if (!m_Rig.IsReferenceNull() && !Accessibility.ReduceCameraMovement) {
                Vector2 offset = default(Vector2);
                if ((flags & CameraModifierFlags.Drift) != 0) {
                    ApplyDrift(ref offset, m_Drifts, m_Time);
                }
                UpdateAndApplyShakes(ref offset, m_Shakes, m_Time);
                m_Rig.EffectsTransform.SetPosition(offset, Axis.XY, Space.Self);
            } else {
                m_Shakes.Clear();
                if (!m_Rig.IsReferenceNull()) {
                    m_Rig.EffectsTransform.SetPosition(default(Vector2), Axis.XY, Space.Self);
                }
            }

            UpdateCachedPlanes();
        }

        private CameraModifierFlags UpdateHintedCamera(ref CameraState ioState, float inDeltaTime, CameraModifierFlags inMask)
        {
            // with no target, smart camera does not work
            if (m_TargetStack.Count <= 0)
                return CameraModifierFlags.NoHints & inMask;

            CameraTargetData target = UpdateCameraTargetPosition();
            CameraModifierFlags flags = target.Flags & inMask;
            Vector3 targetPos = target.m_CachedPosition;

            UpdateHintedCaches(targetPos);

            TargetState targetState;
            targetState.Look = default(Vector3);
            targetState.Lerp = 0;
            targetState.Weight = 0;
            targetState.Offset = default(Vector3);
            targetState.Zoom = 0;

            if ((flags & CameraModifierFlags.Hints) != 0)
            {
                AccumulateHints(ref targetState, m_Hints, target.m_CachedPosition, target.m_CachedLook);
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

            Quaternion targetRotation = Quaternion.LookRotation(targetState.Look);
            Quaternion cameraRotation = Quaternion.Slerp(ioState.Rotation, targetRotation, lerpAmount).normalized;
            if (cameraRotation == targetRotation)
            {
                cameraRotation = targetRotation;
            }

            Vector3 cameraPos = ioState.Position;

            if ((flags & CameraModifierFlags.Bounds) != 0)
            {
                Vector2 size = FrameSize(m_Camera, m_FOVPlane, cameraZoom);
                ApplySoftConstraints(ref targetPos, size, m_Bounds);

                cameraPos = Vector3.LerpUnclamped(ioState.Position, targetPos, lerpAmount);
                ApplyHardConstraints(ref cameraPos, size, m_Bounds);
            }
            else
            {
                cameraPos = Vector3.LerpUnclamped(ioState.Position, targetPos, lerpAmount);
            }

            ioState.Position = cameraPos;
            ioState.Zoom = cameraZoom;
            ioState.Rotation = cameraRotation;

            return target.Flags;
        }

        private CameraTargetData UpdateCameraTargetPosition()
        {
            int targetIdx = m_TargetStack.Count - 1;
            if (targetIdx >= 0)
            {
                ref CameraTargetData target = ref m_TargetStack[targetIdx];
                CacheTarget(ref target, m_Axis);
                return target;
            }

            return default(CameraTargetData);
        }

        private void UpdateHintedCaches(Vector3 inTargetPosition)
        {
            int frameCount = Time.frameCount % (int) (m_CacheFrameSkip + 1);

            // update points and bounds on alternating frames if specified
            if (m_CacheFrameSkip == 0 || m_CacheDirty)
            {
                CachePoints(m_Hints, inTargetPosition, m_Axis);
                CacheBounds(m_Bounds);
                m_CacheDirty = false;
            }
            else if (frameCount == 0)
            {
                CacheBounds(m_Bounds);
            }
            else if (frameCount == 1)
            {
                CachePoints(m_Hints, inTargetPosition, m_Axis);
            }
        }

        private void ConstrainPositionToBounds()
        {
            if (m_Bounds.Count == 0)
                return;

            CacheBounds(m_Bounds);

            CameraState current = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            Vector3 pos = current.Position;
            Vector2 size = FrameSize(m_Camera, m_FOVPlane, current.Zoom);
            ApplySoftConstraints(ref pos, size, m_Bounds);
            ApplyHardConstraints(ref pos, size, m_Bounds);
            current.Position.x = pos.x;
            current.Position.y = pos.y;
            ApplyCameraState(current, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, CameraPoseProperties.Position, m_Axis);
        }

        private static readonly Matrix4x4 View2NDC = Matrix4x4.Translate(-Vector3.one) * Matrix4x4.Scale(Vector3.one * 2);
        private static readonly Vector3 CenterViewportPos = new Vector3(0.5f, 0.5f, 0);

        private void UpdateCachedPlanes()
        {
            Vector3 cameraForwardVector = m_PositionRoot.forward;

            if (m_FOVPlane)
            {
                m_LastGameplayPlane = new Plane(-cameraForwardVector, m_FOVPlane.Target.position);
            }
            else
            {
                m_LastGameplayPlane = new Plane(-cameraForwardVector, Vector3.zero);
            }

            Ray r = new Ray(m_PositionRoot.position, cameraForwardVector);
            m_LastGameplayPlane.Raycast(r, out float planeCastDist);
            m_LastGameplayPlaneCenter = r.GetPoint(planeCastDist);
            m_LastGameplayPlaneDistance = planeCastDist;

            m_LastCenterRay = r;

            Matrix4x4 projWorld = m_Camera.projectionMatrix * m_Camera.worldToCameraMatrix;
            m_LastVPMatrix = projWorld;
            m_LastVPMatrixInv = Matrix4x4.Inverse(projWorld) * View2NDC;
        }

        #endregion // Update

        #region Caching

        static private void CachePoints(RingBuffer<CameraPointData> inPoints, Vector3 inCameraTargetPosition, Axis inMask)
        {
            for(int i = 0, length = inPoints.Count; i < length; i++)
            {
                CachePoint(ref inPoints[i], inCameraTargetPosition, inMask);
            }
        }

        static private void CachePoint(ref CameraPointData ioPoint, Vector3 inCameraTargetPosition, Axis inMask)
        {
            Vector3 cachedPosition = ioPoint.Offset;
            if (ioPoint.Anchor != null)
                cachedPosition += ioPoint.Anchor.position;

            Mask(ref cachedPosition, inMask);

            ioPoint.m_CachedPosition = cachedPosition;

            float cachedWeight = ioPoint.WeightOffset;
            if (ioPoint.Weight != null)
                cachedWeight += ioPoint.Weight.Invoke(cachedPosition, inCameraTargetPosition);

            ioPoint.m_CachedWeight = cachedWeight;

            ioPoint.m_CachedLook = ioPoint.Look;
        }

        static private void CacheTarget(ref CameraTargetData ioTarget, Axis inMask)
        {
            Vector3 cachedPosition = ioTarget.Offset;
            if (ioTarget.Anchor != null)
                cachedPosition += ioTarget.Anchor.position;

            Mask(ref cachedPosition, inMask);

            if (ioTarget.LookFromOffset) {
                ioTarget.m_CachedLook = -ioTarget.Offset;
            } else {
                ioTarget.m_CachedLook = ioTarget.Look;
            }

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

        static private void AccumulateHints(ref TargetState ioState, RingBuffer<CameraPointData> inHints, Vector3 inTargetPosition, Vector3 inTargetLook)
        {
            TargetState settings = default;

            for(int i = 0, length = inHints.Count; i < length; i++)
            {
                AccumulateHint(inHints[i], inTargetPosition, inTargetLook, ref settings);
            }

            ioState = settings;
        }

        static private void AccumulateHint(in CameraPointData inHint, Vector3 inCameraTargetPosition, Vector3 inTargetLook, ref TargetState ioState)
        {
            if (Mathf.Approximately(inHint.m_CachedWeight, 0))
                return;

            Vector3 vector = inHint.m_CachedPosition;
            VectorUtil.Subtract(ref vector, inCameraTargetPosition);
            VectorUtil.Multiply(ref vector, inHint.m_CachedWeight);

            float absWeight = Math.Abs(inHint.m_CachedWeight);

            ioState.Offset += vector;
            ioState.Zoom += inHint.Zoom * absWeight;
            ioState.Lerp += inHint.Lerp * absWeight;
            ioState.Weight += absWeight;

            Vector3 look = inHint.m_CachedLook;
            if (look == default)
                look = inTargetLook;

            ioState.Look += look * absWeight;
        }

        static private void AccumulateHint(in CameraTargetData inTarget, Vector3 inCameraTargetPosition, float inWeight, ref TargetState ioState)
        {
            if (Mathf.Approximately(inWeight, 0))
                return;

            Vector3 vector = inTarget.m_CachedPosition;
            VectorUtil.Subtract(ref vector, inCameraTargetPosition);
            VectorUtil.Multiply(ref vector, inWeight);

            float absWeight = Math.Abs(inWeight);

            ioState.Offset += vector;
            ioState.Zoom += inTarget.Zoom * absWeight;
            ioState.Lerp += inTarget.Lerp * absWeight;
            ioState.Weight += absWeight;
            ioState.Look += inTarget.m_CachedLook * absWeight;
        }

        static private void ApplySoftConstraints(ref Vector3 ioOffset, Vector2 inSize, RingBuffer<CameraBoundsData> inBounds)
        {
            Vector2 offset2D = ioOffset;
            for(int i = 0, length = inBounds.Count; i < length; i++)
            {
                ref CameraBoundsData bounds = ref inBounds[i];
                Geom.Constrain(ref offset2D, inSize, bounds.m_CachedRegion, bounds.SoftEdges);
            }
            ioOffset.x = offset2D.x;
            ioOffset.y = offset2D.y;
        }

        static private void ApplyHardConstraints(ref Vector3 ioOffset, Vector2 inSize, RingBuffer<CameraBoundsData> inBounds)
        {
            Vector2 offset2D = ioOffset;
            for(int i = 0, length = inBounds.Count; i < length; i++)
            {
                ref CameraBoundsData bounds = ref inBounds[i];
                Geom.Constrain(ref offset2D, inSize, bounds.m_CachedRegion, bounds.HardEdges);
            }
            ioOffset.x = offset2D.x;
            ioOffset.y = offset2D.y;
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

        static private void UpdateAndApplyShakes(ref Vector2 ioOffset, RingBuffer<CameraShakeData> ioShakes, double inTime)
        {
            for(int i = ioShakes.Count - 1; i >= 0; i--)
            {
                ref CameraShakeData shake = ref ioShakes[i];
                double timeSinceStart = inTime - shake.m_StartTime;
                float fade = 1f - (float) (timeSinceStart / shake.Duration);
                if (fade <= 0) {
                    ioShakes.FastRemoveAt(i);
                    continue;
                }

                float x = shake.Distance.x * (float) Math.Cos(Math.PI * 2 * ((shake.Offset.x + timeSinceStart) % shake.Period.x) / shake.Period.x) * fade;
                float y = shake.Distance.y * (float) Math.Cos(Math.PI * 2 * ((shake.Offset.y + timeSinceStart) % shake.Period.y) / shake.Period.y) * fade;
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
                ioState.Look /= weight;
            }

            if (ioState.Look == default(Vector3))
            {
                ioState.Look = Vector3.forward;
            }
            else
            {
                ioState.Look.Normalize();
            }
        }

        static private CameraState GetCameraState(Transform inRoot, Camera inCamera, CameraFOVPlane inPlane)
        {
            CameraState state;
            state.Position = inRoot.localPosition;
            state.Rotation = inRoot.localRotation;
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
            state.FieldOfView = inCamera.fieldOfView;
            return state;
        }

        static private void ApplyCameraState(in CameraState inState, Transform inRoot, Camera inCamera, CameraFOVPlane inPlane, CameraFOVMode inFOVMode, CameraPoseProperties inProperties, Axis inAxis)
        {
            if ((inProperties & CameraPoseProperties.Position) != 0)
            {
                inRoot.SetPosition(inState.Position, inAxis, Space.Self);
            }

            if ((inProperties & CameraPoseProperties.Rotation) != 0)
            {
                inRoot.localRotation = inState.Rotation;
            }

            if ((inProperties & CameraPoseProperties.FieldOfView) != 0)
            {
                inCamera.fieldOfView = inState.FieldOfView;
            }

            if (!inPlane.IsReferenceNull())
            {
                if ((inProperties & CameraPoseProperties.Height) != 0)
                    inPlane.Height = inState.Height;
                if ((inProperties & CameraPoseProperties.Zoom) != 0)
                    inPlane.Zoom = inState.Zoom;

                inPlane.enabled = inFOVMode == CameraFOVMode.Plane;
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
            m_FOVMode = CameraFOVMode.Plane;
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
            if (m_Rig != null && m_Rig.ThreeDMode)
            {
                SnapToTarget(CameraPoseProperties.All);
            }
            else
            {
                SnapToTarget();
            }
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
                m_Axis = Axis.XY;
            }
            else
            {
                m_Camera = m_Rig.Camera;
                m_Mode = m_Rig.DefaultMode;
                m_PositionRoot = m_Rig.RootTransform;
                m_FOVPlane = m_Rig.FOVPlane;
                m_Axis = m_Rig.ThreeDMode ? Axis.XYZ : Axis.XY;
            }

            Assert.NotNull(m_Camera, "No main camera located for scene");

            m_RenderScale = m_Camera.EnsureComponent<CameraRenderScale>();

            m_Camera.transparencySortMode = TransparencySortMode.Orthographic;
            m_LastScreenAspectClip = default(Rect);
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
            m_Shakes.Clear();
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
        public void SnapToTarget(CameraPoseProperties inProperties = CameraPoseProperties.Default)
        {
            if (m_TargetStack.Count == 0)
                return;

            CameraTargetData target = UpdateCameraTargetPosition();
            CameraState current = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            CameraState state = new CameraState(target.m_CachedPosition, Quaternion.LookRotation(target.m_CachedLook), current.Height, target.Zoom, current.FieldOfView);
            ApplyCameraState(state, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, inProperties, m_Axis);
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
            newTarget.Look = Vector3.forward;
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
            newTarget.Look = Vector3.forward;
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

            ref CameraTargetData targetData = ref PushTarget(inTarget.TransformOverride ? inTarget.TransformOverride : inTarget.transform, inTarget.Lerp, inTarget.Zoom, inTarget.Flags);
            targetData.Offset = inTarget.Offset;
            targetData.Look = inTarget.Look;
            targetData.LookFromOffset = inTarget.LookFromOffset;
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

        /// <summary>
        /// Removes the last target from the target stack.
        /// </summary>
        public bool PopTarget()
        {
            int idx = m_TargetStack.Count - 1;
            if (idx >= 0)
            {
                ref CameraTargetData target = ref m_TargetStack[idx];
                m_TargetStack.PopBack();
                DebugService.Log(LogMask.Camera, "[CameraService] Removed camera target '{0}'", target.Id);
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
            m_CacheDirty = true;
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
            m_CacheDirty = true;
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
            newHint.Look = default(Vector3);
            newHint.Weight = null;
            newHint.WeightOffset = inWeight;
            newHint.Zoom = inZoom;
            newHint.Lerp = inLerp;
            m_Hints.PushBack(newHint);
            m_CacheDirty = true;
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
            newHint.Look = default(Vector3);
            newHint.Weight = null;
            newHint.WeightOffset = inWeight;
            newHint.Zoom = inZoom;
            newHint.Lerp = inLerp;
            m_Hints.PushBack(newHint);
            m_CacheDirty = true;
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

        #region Shakes

        /// <summary>
        /// Adds a new camera shake.
        /// </summary>
        public void AddShake(float inDistance, float inPeriod, float inDuration)
        {
            AddShake(new Vector2(RNG.Instance.NextFloat(0.9f, 1.1f) * inDistance, inDistance), new Vector2(inPeriod, RNG.Instance.NextFloat(0.9f, 1.1f) * inPeriod), inDuration);
        }

        /// <summary>
        /// Adds a new camera shake.
        /// </summary>
        public void AddShake(Vector2 inDistance, Vector2 inPeriod, float inDuration)
        {
            if (Accessibility.ReduceCameraMovement) {
                return;
            }

            CameraShakeData newShake = default(CameraShakeData);
            newShake.Distance = inDistance;
            newShake.Period = inPeriod;
            newShake.Duration = inDuration;
            newShake.Offset = new Vector2(RNG.Instance.NextFloat(inDuration), RNG.Instance.NextFloat(inDuration));
            newShake.m_StartTime = m_Time;
            m_Shakes.PushBack(newShake);
        }

        /// <summary>
        /// Stops all screen shakes.
        /// </summary>
        public void StopShaking()
        {
            m_Shakes.Clear();
        }

        #endregion // Shakes

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
            return MoveToPosition(target.m_CachedPosition, null, target.Zoom, inDuration, inCurve);
        }

        /// <summary>
        /// Snaps to a specific position.
        /// </summary>
        public void SnapToPosition(Vector2 inPosition, Quaternion? inRotation = null, float? inZoom = null)
        {
            SetAsScripted();

            CameraState currentState = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            CameraState newState = new CameraState(inPosition, inRotation.GetValueOrDefault(currentState.Rotation), currentState.Height, inZoom.GetValueOrDefault(currentState.Zoom), currentState.FieldOfView);
            RecordLatestState(newState);
            ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, CameraPoseProperties.PosAndZoom | CameraPoseProperties.Rotation, m_Axis);
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

            CameraState newState = new CameraState(inPose.transform.position, inPose.transform.rotation, inPose.Height, inPose.Zoom, m_Camera.fieldOfView);
            if (!m_FOVPlane.IsReferenceNull() && inPose.Target != null)
                m_FOVPlane.Target = inPose.Target;

            RecordLatestState(newState);
            SetFOVMode(inPose.Mode);
            
            ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, inProperties, m_Axis);
            m_ScriptedAnimation.Stop();
        }

        /// <summary>
        /// Moves the camera to a specific position.
        /// </summary>
        public IEnumerator MoveToPosition(Vector2 inPosition, Quaternion? inRotation, float? inZoom, float inDuration, Curve inCurve = Curve.Smooth, Axis inAxis = Axis.XYZ, Action inOnComplete = null)
        {
            SetAsScripted();

            CameraPoseProperties properties = CameraPoseProperties.Position;
            if (inZoom.HasValue)
                properties |= CameraPoseProperties.Zoom;
            if (inRotation.HasValue)
                properties |= CameraPoseProperties.Rotation;
            
            CameraState currentState = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            CameraState newState = new CameraState(inPosition, inRotation.GetValueOrDefault(currentState.Rotation), currentState.Height, inZoom.GetValueOrDefault(currentState.Zoom), currentState.FieldOfView);

            RecordLatestState(newState);

            if (inDuration <= 0)
            {
                ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, properties, inAxis & m_Axis);
                m_ScriptedAnimation.Stop();
                return null;
            }

            m_ScriptedAnimation.Replace(this, MoveCameraTween(currentState, newState, properties, inDuration, inCurve, inAxis & m_Axis, inOnComplete));
            return m_ScriptedAnimation.Wait();
        }

        /// <summary>
        /// Moves the camera to a specific pose.
        /// </summary>
        public IEnumerator MoveToPose(CameraPose inPose, float inDuration, Curve inCurve = Curve.Smooth, CameraPoseProperties inPropertiesMask = CameraPoseProperties.Default, Axis inAxis = Axis.XYZ, Action inOnComplete = null)
        {
            Assert.NotNull(inPose);

            SetAsScripted();

            if (!m_FOVPlane.IsReferenceNull() && inPose.Target != null) {
                m_FOVPlane.SetTargetPreserveFOV(inPose.Target);
            }

            CameraState currentState = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            CameraState newState = new CameraState(inPose.transform.position, inPose.transform.rotation, inPose.Height, inPose.Zoom, inPose.FieldOfView);

            RecordLatestState(newState);
            SetFOVMode(inPose.Mode);

            if (inDuration <= 0)
            {
                ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, inPose.Properties & inPropertiesMask, inAxis);
                m_ScriptedAnimation.Stop();
                return null;
            }

            m_ScriptedAnimation.Replace(this, MoveCameraTween(currentState, newState, inPose.Properties & inPropertiesMask, inDuration, inCurve, inAxis, inOnComplete));
            return m_ScriptedAnimation.Wait();
        }

        /// <summary>
        /// Moves the camera to a specific pose.
        /// </summary>
        public IEnumerator MoveToPose(CameraPose inPose, Vector3 inSplineControlPoint, float inDuration, Curve inCurve = Curve.Smooth, CameraPoseProperties inPropertiesMask = CameraPoseProperties.Default, Axis inAxis = Axis.XYZ, Action inOnComplete = null)
        {
            Assert.NotNull(inPose);

            SetAsScripted();

            if (!m_FOVPlane.IsReferenceNull() && inPose.Target != null)
                m_FOVPlane.SetTargetPreserveFOV(inPose.Target);

            CameraState currentState = GetCameraState(m_PositionRoot, m_Camera, m_FOVPlane);
            CameraState newState = new CameraState(inPose.transform.position, inPose.transform.rotation, inPose.Height, inPose.Zoom, currentState.FieldOfView);

            RecordLatestState(newState);
            SetFOVMode(inPose.Mode);

            if (inDuration <= 0)
            {
                ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, inPose.Properties & inPropertiesMask, inAxis);
                m_ScriptedAnimation.Stop();
                return null;
            }

            m_ScriptedAnimation.Replace(this, MoveCameraSplineTween(currentState, newState, inSplineControlPoint, inPose.Properties & inPropertiesMask, inDuration, inCurve, inAxis, inOnComplete));
            return m_ScriptedAnimation.Wait();
        }

        /// <summary>
        /// Moves the camera along a specific spline.
        /// </summary>
        public IEnumerator MoveAlongSpline(CameraSpline inSpline, float inDuration, CameraPoseProperties inPropertiesMask, Axis inAxis = Axis.XYZ, Action inOnComplete = null)
        {
            Assert.NotNull(inSpline);

            SetAsScripted();

            m_ScriptedAnimation.Replace(this, MoveCameraSplineTween(inSpline, inSpline.Properties & inPropertiesMask, inDuration, inAxis, inOnComplete));
            return m_ScriptedAnimation.Wait();
        }

        private IEnumerator MoveCameraTween(CameraState inInitialState, CameraState inTarget, CameraPoseProperties inProperties, float inDuration, Curve inCurve, Axis inAxis, Action inOnComplete = null)
        {
            return Tween.ZeroToOne((f) => {
                CameraState newState = default;
                CameraState.Lerp(inInitialState, inTarget, ref newState, f);
                ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, inProperties, inAxis & m_Axis);
            }, inDuration).Ease(inCurve).OnComplete(inOnComplete);
        }

        private IEnumerator MoveCameraSplineTween(CameraState inInitialState, CameraState inTarget, Vector3 inControlPoint, CameraPoseProperties inProperties, float inDuration, Curve inCurve, Axis inAxis, Action inOnComplete = null)
        {
            SimpleSpline spline = new SimpleSpline(inInitialState.Position, inTarget.Position, inControlPoint);
            return Tween.ZeroToOne((f) => {
                CameraState newState = default;
                CameraState.Lerp(inInitialState, inTarget, ref newState, f, spline);
                ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, inProperties, inAxis & m_Axis);
            }, inDuration).Ease(inCurve).OnComplete(inOnComplete);
        }

        private IEnumerator MoveCameraSplineTween(CameraSpline inSpline, CameraPoseProperties inProperties, float inDuration, Axis inAxis, Action inOnComplete)
        {
            return Tween.ZeroToOne((f) => {
                CameraState newState = default;
                inSpline.Interpolate(f, ref newState);
                ApplyCameraState(newState, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, inProperties, inAxis & m_Axis);
            }, inDuration).OnComplete(inOnComplete);
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
        /// Casts from a screen position to a world position on the given plane.
        /// </summary>
        public Vector3? ScreenToPlanePosition(Vector2 inScreenPos, Plane inPlane)
        {
            Vector3 screenPos = inScreenPos;
            screenPos.z = 1;

            Ray r = m_Camera.ScreenPointToRay(screenPos);

            float dist;
            if (!inPlane.Raycast(r, out dist))
                return null;

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

        public PlanePositionHelper GetPositionHelper()
        {
            return new PlanePositionHelper()
            {
                Camera = m_Camera,
                CenterRay = m_LastCenterRay,
                GameplayPlane = m_LastGameplayPlane,
                ViewportToWorld = m_LastVPMatrixInv,
                GameplayDistance = m_LastGameplayPlaneDistance
            };
        }

        #endregion // Positions

        #region FieldOfView

        /// <summary>
        /// Locks the field of view plane.
        /// </summary>
        public void SetFOVMode(CameraFOVMode inMode) {
            if (m_FOVMode != inMode) {
                m_FOVMode = inMode;
                if (m_FOVPlane != null) {
                    m_FOVPlane.enabled = inMode == CameraFOVMode.Plane;
                }
            }
        }

        #endregion // FieldOfView

        #region Render Regions

        private void UpdateCameraRenderRegion() {
            float aspect = DesiredWidth / DesiredHeight;
            float scrW = Screen.width, scrH = Screen.height;
            float w = scrH * aspect, h = scrH;

            if (w > scrW) {
                w = scrW;
                h = w / aspect;
            } else if (h > scrH) {
                h = scrH;
                w = h * aspect;
            }

            float diffX = 1 - w / scrW,
                diffY = 1 - h / scrH;

            Rect r = default;
            r.x = diffX / 2;
            r.y = diffY / 2;
            r.width = 1 - diffX;
            r.height = 1 - diffY;

            m_LastScreenAspectClip = r;
        }

        private void OnCameraPreRender(ScriptableRenderContext ctx, Camera[] cameras) {
            UpdateCameraRenderRegion();

            foreach(var camera in cameras) {
                if (camera.cameraType != CameraType.Game) {
                    return;
                }

                if (camera.targetTexture == null) {
                    camera.rect = m_LastScreenAspectClip;
                } else {
                    return;
                }
            }
            
            float left = m_LastScreenAspectClip.x, bottom = m_LastScreenAspectClip.y;
            if (left != 0 || bottom != 0) {
                Color c = m_ClipOutsideColor;
                float scrW = Screen.width, scrH = Screen.height;
                // woo boy we're getting into some low-level graphics
                GL.PushMatrix();
                GL.LoadOrtho();
                Rect r = default;
                if (left != 0) {
                    r.x = 0;
                    r.y = 0;
                    r.width = left * scrW;
                    r.height = scrH;
                    GL.Viewport(r);
                    GL.Clear(false, true, c);
                    r.x = scrW - r.width;
                    GL.Viewport(r);
                    GL.Clear(false, true, c);
                } else {
                    r.x = 0;
                    r.y = 0;
                    r.width = scrW;
                    r.height = bottom * scrH;
                    GL.Viewport(r);
                    GL.Clear(false, true, c);
                    r.y = scrH - r.height;
                    GL.Viewport(r);
                    GL.Clear(false, true, c);
                }
                GL.PopMatrix();
            }
        }

        #endregion // Render Regions

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

            RenderPipelineManager.beginFrameRendering += OnCameraPreRender;
        }

        protected override void Shutdown()
        {
            RenderPipelineManager.beginFrameRendering -= OnCameraPreRender;

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

        #endregion // Utils

        #region Leaf

        [LeafMember("CameraSnapToTarget"), UnityEngine.Scripting.Preserve]
        static private void LeafSnapToTarget()
        {
            Services.Camera.SnapToTarget();
        }

        [LeafMember("CameraRecenterOnTarget"), UnityEngine.Scripting.Preserve]
        static private IEnumerator LeafRecenterOnTarget(float inDuration, Curve inCurve = Curve.Smooth)
        {
            return Services.Camera.RecenterOnTarget(inDuration, inCurve);
        }

        [LeafMember("CameraSetMode"), UnityEngine.Scripting.Preserve]
        static private void LeafSetMode(CameraMode inMode)
        {
            switch(inMode)
            {
                case CameraMode.Hinted:
                    Services.Camera.SetAsHinted();
                    break;

                case CameraMode.Scripted:
                    Services.Camera.SetAsScripted();
                    break;
            }
        }

        [LeafMember("CameraPushTarget"), UnityEngine.Scripting.Preserve]
        static private void LeafPushTarget(ScriptObject inObject, float inLerp = 3, float inZoom = 1)
        {
            Assert.NotNull(inObject, "Cannot pass null target");
            CameraTarget target = inObject.GetComponent<CameraTarget>();
            if (target != null)
            {
                Services.Camera.PushTarget(target);
            }
            else
            {
                Services.Camera.PushTarget(target.transform, inLerp, inZoom);
            }
        }

        [LeafMember("CameraPopTarget"), UnityEngine.Scripting.Preserve]
        static private void LeafPopTarget()
        {
            Services.Camera.PopTarget();
        }

        [LeafMember("CameraMoveToPose"), UnityEngine.Scripting.Preserve]
        static private IEnumerator LeafModeToPose(ScriptObject inPose, float inDuration, Curve inCurve = Curve.Smooth)
        {
            Assert.NotNull(inPose, "Cannot pass null pose");
            CameraPose pose = inPose.GetComponent<CameraPose>();
            if (pose != null)
            {
                return Services.Camera.MoveToPose(pose, inDuration, inCurve);
            }
            else
            {
                return Services.Camera.MoveToPosition(pose.transform.position, pose.transform.rotation, null, inDuration, inCurve);
            }
        }

        [LeafMember("CameraSnapToPose"), UnityEngine.Scripting.Preserve]
        static private void LeafSnapToPose(ScriptObject inPose)
        {
            Assert.NotNull(inPose, "Cannot pass null pose");
            CameraPose pose = inPose.GetComponent<CameraPose>();
            if (pose != null)
            {
                Services.Camera.SnapToPose(pose);
            }
            else
            {
                Services.Camera.SnapToPosition(pose.transform.position);
            }
        }

        #endregion // Leaf
    
        #region Debug

        [System.Diagnostics.Conditional("DEVELOPMENT")]
        private void RecordLatestState(CameraState inState)
        {
            #if DEVELOPMENT
            m_LastAssignedState = inState;
            #endif // DEVELOPMENT
        }

        #if DEVELOPMENT

        internal void DebugResetToLastState()
        {
            switch(m_Mode)
            {
                case CameraMode.Scripted:
                    ApplyCameraState(m_LastAssignedState, m_PositionRoot, m_Camera, m_FOVPlane, m_FOVMode, CameraPoseProperties.All, m_Axis);
                    break;

                case CameraMode.Hinted:
                    SnapToTarget();
                    break;
            }
        }

        #endif // DEVELOPMENT

        #endregion // Debug
    }
}