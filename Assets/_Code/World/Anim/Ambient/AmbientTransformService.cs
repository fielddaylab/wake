using System;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using Random = System.Random;

namespace Aqua.Animation
{
    public class AmbientTransformService : ServiceBehaviour
    {
        #region Consts

        private const byte ChangeFlag_Position = 1;
        private const byte ChangeFlag_Scale = 2;
        private const byte ChangeFlag_Rotation = 4;
        private const byte ChangeFlag_PositionAndRotationMask = ChangeFlag_Position | ChangeFlag_Rotation;

        #endregion // Consts

        private readonly RingBuffer<AmbientTransform> m_Transforms = new RingBuffer<AmbientTransform>(64, RingBufferMode.Expand);
        private readonly Random m_Random = new Random(Environment.TickCount ^ 52507823);

        private float m_LastTimestamp;

        #region Register/Deregister

        public void Register(AmbientTransform inTransform)
        {
            Assert.False(m_Transforms.Contains(inTransform), "AmbientTransform was registered twice");
            m_Transforms.PushBack(inTransform);
            SetupTransform(inTransform);
        }

        public void Deregister(AmbientTransform inTransform)
        {
            Assert.True(m_Transforms.Contains(inTransform), "AmbientTransform was deregistered multiple times, or never registered");
            m_Transforms.FastRemove(inTransform);
            ResetTransform(inTransform);
        }

        private void SetupTransform(AmbientTransform inTransform)
        {
            AmbientUtils.InitVec3Wave(ref inTransform.TransformState.PositionState, ref inTransform.PositionAnimation,
                inTransform.TransformSpace == Space.World ? inTransform.Transform.position : inTransform.Transform.localPosition, m_LastTimestamp, m_Random);
            AmbientUtils.InitVec3Wave(ref inTransform.TransformState.RotationState, ref inTransform.RotationAnimation,
                inTransform.TransformSpace == Space.World ? inTransform.Transform.eulerAngles : inTransform.Transform.localEulerAngles, m_LastTimestamp, m_Random);
            AmbientUtils.InitVec3Wave(ref inTransform.TransformState.ScaleState, ref inTransform.ScaleAnimation,
                inTransform.Transform.localScale, m_LastTimestamp, m_Random);
        }

        private void ResetTransform(AmbientTransform inTransform)
        {
            if (!inTransform.Transform)
                return;
            
            if (inTransform.TransformSpace == Space.World)
            {
                inTransform.Transform.SetPositionAndRotation(inTransform.PositionAnimation.Initial, Quaternion.Euler(inTransform.RotationAnimation.Initial));
            }
            else
            {
                inTransform.Transform.localPosition = inTransform.PositionAnimation.Initial;
                inTransform.Transform.localRotation = Quaternion.Euler(inTransform.RotationAnimation.Initial);
            }

            inTransform.Transform.localScale = inTransform.ScaleAnimation.Initial;
        }

        #endregion // Register/Deregister

        private void ResetTimestamp()
        {
            m_LastTimestamp = 0;
        }

        private void LateUpdate()
        {
            m_LastTimestamp += Time.deltaTime;

            if (m_Transforms.Count > 0)
            {
                Physics2D.autoSyncTransforms = false;
                Process(m_LastTimestamp, m_Transforms, m_Random);
                Physics2D.autoSyncTransforms = true;
            }
        }

        #region Internal

        static private unsafe void Process(float inTimestamp, ListSlice<AmbientTransform> inTransforms, Random inRandom)
        {
            // TODO: Batch these in groups of 32 or something
            // otherwise we risk using too much of the stack

            int objectCount = inTransforms.Length;
            int objectIdx;

            // buffers
            Vector3* positions = stackalloc Vector3[objectCount];
            Vector3* rotations = stackalloc Vector3[objectCount];
            Vector3* scales = stackalloc Vector3[objectCount];

            AmbientVec3State* stateBuffer = stackalloc AmbientVec3State[objectCount];
            AmbientVec3PropertyConfig* configBuffer = stackalloc AmbientVec3PropertyConfig[objectCount];
            float* animScaleBuffer = stackalloc float[objectCount];
            byte* changeBuffer = stackalloc byte[objectCount];

            for(int i = 0; i < objectCount; ++i)
            {
                changeBuffer[i] = 0;
            }
            
            // process position

            LoadPositions(positions, animScaleBuffer, stateBuffer, configBuffer, inTransforms);
            AmbientUtils.ProcessVec3Additive(positions, animScaleBuffer, stateBuffer, configBuffer, changeBuffer, objectCount, inTimestamp, ChangeFlag_Position);
            AmbientUtils.ProcessVec3Waves(stateBuffer, configBuffer, objectCount, inTimestamp, inRandom);
            StorePositionState(stateBuffer, inTransforms);

            // process rotation

            LoadRotations(rotations, stateBuffer, configBuffer, inTransforms);
            AmbientUtils.ProcessVec3Additive(rotations, animScaleBuffer, stateBuffer, configBuffer, changeBuffer, objectCount, inTimestamp, ChangeFlag_Rotation);
            AmbientUtils.ProcessVec3Waves(stateBuffer, configBuffer, objectCount, inTimestamp, inRandom);
            StoreRotationState(stateBuffer, inTransforms);

            // process scale

            LoadScales(scales, stateBuffer, configBuffer, inTransforms);
            AmbientUtils.ProcessVec3Additive(scales, animScaleBuffer, stateBuffer, configBuffer, changeBuffer, objectCount, inTimestamp, ChangeFlag_Scale);
            AmbientUtils.ProcessVec3Waves(stateBuffer, configBuffer, objectCount, inTimestamp, inRandom);
            StoreScaleState(stateBuffer, inTransforms);

            // store results back in objects

            objectIdx = 0;
            foreach(var obj in inTransforms)
            {
                byte changeFlags = changeBuffer[objectIdx];
                if (changeFlags != 0)
                {
                    Vector3 targetPos = positions[objectIdx];
                    Quaternion targetRot = Quaternion.Euler(rotations[objectIdx]);
                    Vector3 targetScale = scales[objectIdx];
                    Transform targetTransform = obj.Transform;

                    if (obj.TransformSpace == Space.Self)
                    {
                        // where's my SetLocalPositionAndRotation, Unity?!

                        if ((changeFlags & ChangeFlag_Position) != 0)
                        {
                            targetTransform.localPosition = targetPos;
                        }
                        if ((changeFlags & ChangeFlag_Rotation) != 0)
                        {
                            targetTransform.localRotation = targetRot;
                        }
                    }
                    else
                    {
                        if ((changeFlags & ChangeFlag_PositionAndRotationMask) == ChangeFlag_PositionAndRotationMask)
                        {
                            targetTransform.SetPositionAndRotation(targetPos, targetRot);
                        }
                        else if ((changeFlags & ChangeFlag_Position) != 0)
                        {
                            targetTransform.position = targetPos;
                        }
                        else if ((changeFlags & ChangeFlag_Rotation) != 0)
                        {
                            targetTransform.rotation = targetRot;
                        }
                    }

                    if ((changeFlags & ChangeFlag_Scale) != 0)
                    {
                        targetTransform.localScale = targetScale;
                    }
                }
                ++objectIdx;
            }
        }

        #region Load

        static private unsafe void LoadPositions(Vector3* ioPositions, float* ioAnimScales, AmbientVec3State* ioStateBuffer, AmbientVec3PropertyConfig* ioPropertyBuffer, ListSlice<AmbientTransform> inTransforms)
        {
            int objectIndex = 0;
            foreach(var obj in inTransforms)
            {
                ioStateBuffer[objectIndex] = obj.TransformState.PositionState;
                ioPropertyBuffer[objectIndex] = obj.PositionAnimation;
                ioPositions[objectIndex] = obj.PositionAnimation.Initial;
                ioAnimScales[objectIndex] = obj.AnimationScale;
                ++objectIndex;
            }
        }

        static private unsafe void LoadScales(Vector3* ioScales, AmbientVec3State* ioStateBuffer, AmbientVec3PropertyConfig* ioPropertyBuffer, ListSlice<AmbientTransform> inTransforms)
        {
            int objectIndex = 0;
            foreach(var obj in inTransforms)
            {
                ioStateBuffer[objectIndex] = obj.TransformState.ScaleState;
                ioPropertyBuffer[objectIndex] = obj.ScaleAnimation;
                ioScales[objectIndex] = obj.ScaleAnimation.Initial;
                ++objectIndex;
            }
        }

        static private unsafe void LoadRotations(Vector3* ioRotations, AmbientVec3State* ioStateBuffer, AmbientVec3PropertyConfig* ioPropertyBuffer, ListSlice<AmbientTransform> inTransforms)
        {
            int objectIndex = 0;
            foreach(var obj in inTransforms)
            {
                ioStateBuffer[objectIndex] = obj.TransformState.RotationState;
                ioPropertyBuffer[objectIndex] = obj.RotationAnimation;
                ioRotations[objectIndex] = obj.RotationAnimation.Initial;
                ++objectIndex;
            }
        }

        #endregion // Load

        #region Store

        static private unsafe void StorePositionState(AmbientVec3State* inStateBuffer, ListSlice<AmbientTransform> inTransforms)
        {
            int objectIdx = 0;
            foreach(var obj in inTransforms)
            {
                obj.TransformState.PositionState = inStateBuffer[objectIdx];
                objectIdx++;
            }
        }

        static private unsafe void StoreRotationState(AmbientVec3State* inStateBuffer, ListSlice<AmbientTransform> inTransforms)
        {
            int objectIdx = 0;
            foreach(var obj in inTransforms)
            {
                obj.TransformState.RotationState = inStateBuffer[objectIdx];
                objectIdx++;
            }
        }

        static private unsafe void StoreScaleState(AmbientVec3State* inStateBuffer, ListSlice<AmbientTransform> inTransforms)
        {
            int objectIdx = 0;
            foreach(var obj in inTransforms)
            {
                obj.TransformState.ScaleState = inStateBuffer[objectIdx];
                objectIdx++;
            }
        }

        #endregion // Store

        #endregion // Internal

        #region Service

        private void OnSceneLoaded(SceneBinding inBinding, object inContext)
        {
            m_LastTimestamp = 0;
        }

        protected override void Initialize()
        {
            base.Initialize();

            SceneHelper.OnSceneUnload += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            SceneHelper.OnSceneLoaded -= OnSceneLoaded;
        }

        #endregion // Service
    }
}