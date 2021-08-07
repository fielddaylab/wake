using System;
using Aqua;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.ExperimentV2
{
    [Serializable]
    public class ActorDefinition
    {
        #region Types

        public enum SpawnTypeId : byte
        {
            Anywhere, // actor spawns anywhere
            Bottom, // actor spawns on the bottom of the tank
            Top // actor spawns on the water surface of the tank
        }

        [Serializable]
        public struct SpawnConfiguration
        {
            [AutoEnum] public SpawnTypeId SpawnType;
            public float AvoidTankSidesRadius;
            public float AvoidTankTopBottomRadius;
            public float AvoidActorRadius;
        }

        public enum MovementTypeId : byte
        {
            Stationary, // actor does not move
            Swim, // actor swims
        }

        [Serializable]
        public struct MovementConfiguration
        {
            [AutoEnum] public MovementTypeId MoveType;
            public float MovementIdleDistance;
            public float MovementIdleDistanceMax;
            public float MovementSpeed;
            public float MovementInterval;
            public float MovementIntervalRandom;
        }

        public enum EatTypeId : byte
        {
            None, // no eating
            Nibble, // several small bites
            LargeBite, // large bite
        }

        [Serializable]
        public struct EatingConfiguration
        {
            [AutoEnum] public EatTypeId EatType;
            public float SenseRadius;
            private float MovementMultiplier;
            private int NibbleCount;
            private int NibbleCountRandom;
        }

        [Serializable]
        public struct StressConfiguration
        {
            public float MovementSpeedMultiplier;
            public float MovementIntervalMultiplier;
        }

        [Serializable]
        public struct ValidEatTarget
        {
            public SerializedHash32 TargetId;
        }

        #endregion // Types

        [HideInInspector] public StringHash32 Id;
        [FilterBestiary(BestiaryDescCategory.Critter)] public BestiaryDesc Type;
        
        [Header("Prefabs")]
        public ActorInstance Prefab;
        // TODO: Microscope prefab

        [Space]
        public int SpawnAmountOverride = -1;

        [Header("Behavior")]
        public SpawnConfiguration Spawning = default(SpawnConfiguration);
        public MovementConfiguration Movement = default(MovementConfiguration);
        public EatingConfiguration Eating = default(EatingConfiguration);
        public StressConfiguration Stress = default(StressConfiguration);
        
        [Header("Derived From Facts")]

        public ActorStateTransitionSet StateEvaluator;
        public ValidEatTarget[] AliveEatTargets;
        public ValidEatTarget[] StressedEatTargets;
        public int SpawnCount;

        #region Utility

        static public Vector3 FindRandomLocationOnBottom(System.Random inRandom, in Bounds inBounds, float inBottomOffset, float inSidesOffset)
        {
            Vector3 center = inBounds.center;
            Vector3 extents = inBounds.extents;
            float bottomY = center.y - extents.y + inBottomOffset;
            float limitX = extents.x - inSidesOffset;
            return new Vector3(center.x + inRandom.NextFloat(-limitX, limitX), bottomY, center.z);
        }

        static public Vector3 FindRandomLocationOnTop(System.Random inRandom, in Bounds inBounds, float inTopOffset, float inSidesOffset)
        {
            Vector3 center = inBounds.center;
            Vector3 extents = inBounds.extents;
            float topY = center.y + extents.y - inTopOffset;
            float limitX = extents.x - inSidesOffset;
            return new Vector3(center.x + inRandom.NextFloat(-limitX, limitX), topY, center.z);
        }

        static public Vector3 FindRandomLocationInTank(System.Random inRandom, in Bounds inBounds, float inTopBottomOffset, float inSidesOffset)
        {
            Vector3 center = inBounds.center;
            Vector3 extents = inBounds.extents;
            float limitX = extents.x - inSidesOffset;
            float limitY = extents.y - inTopBottomOffset;
            return new Vector3(center.x + inRandom.NextFloat(-limitX, limitX), center.y + inRandom.NextFloat(-limitY, limitY), center.z);
        }

        static public Vector3 FindRandomSpawnLocation(System.Random inRandom, in Bounds inBounds, in SpawnConfiguration inConfiguration)
        {
            switch(inConfiguration.SpawnType)
            {
                case SpawnTypeId.Bottom:
                    return FindRandomLocationOnBottom(inRandom, inBounds, inConfiguration.AvoidTankTopBottomRadius, inConfiguration.AvoidTankSidesRadius);

                case SpawnTypeId.Top:
                    return FindRandomLocationOnTop(inRandom, inBounds, inConfiguration.AvoidTankTopBottomRadius, inConfiguration.AvoidTankSidesRadius);

                case SpawnTypeId.Anywhere:
                    return FindRandomLocationInTank(inRandom, inBounds, inConfiguration.AvoidTankTopBottomRadius, inConfiguration.AvoidTankSidesRadius);

                default:
                    Assert.Fail("Unknown spawn type {0}", inConfiguration.SpawnType);
                    return default(Vector3);
            }
        }

        static public Vector3 FindRandomTankLocationInRange(System.Random inRandom, in Bounds inBounds, Vector3 inCurrentPosition, float inDistance, float inTopBottomOffset, float inSidesOffset)
        {
            Vector3 center = inBounds.center;
            Vector3 extents = inBounds.extents;
            float limitX = extents.x - inSidesOffset;
            float limitY = extents.y - inTopBottomOffset;

            Vector3 currentOffset = inCurrentPosition;
            VectorUtil.Subtract(ref currentOffset, center);

            float left, right, top, bottom;
            left = Math.Max(currentOffset.x - inDistance, -limitX);
            right = Math.Min(currentOffset.x + inDistance, limitX);
            bottom = Math.Max(currentOffset.y - inDistance, -limitY);
            top = Math.Min(currentOffset.y + inDistance, limitY);

            Vector2 newOffset = new Vector2(inRandom.NextFloat(left, right), inRandom.NextFloat(bottom, top));
            VectorUtil.Subtract(ref newOffset, currentOffset);

            float newMagnitude = Math.Min(newOffset.magnitude, inDistance);
            newOffset.Normalize();
            VectorUtil.Multiply(ref newOffset, newMagnitude);
            VectorUtil.Add(ref inCurrentPosition, newOffset);
            return inCurrentPosition;
        }

        #endregion // Utility

        #if UNITY_EDITOR

        static internal void LoadFromBestiary(ActorDefinition inDef, BestiaryDesc inBestiary, ActorInstance inPrefab)
        {
            Assert.NotNull(inDef);
            Assert.NotNull(inBestiary);
            Assert.True(inBestiary.Category() == BestiaryDescCategory.Critter, "Provided entry is not for a critter");
            Assert.True(!inBestiary.HasFlags(BestiaryDescFlags.DoNotUseInExperimentation), "Provided entry is not usable in experimentation");
            
            inDef.Id = inBestiary.Id();
            inDef.Type = inBestiary;
            inDef.StateEvaluator = inBestiary.GetActorStateTransitions();
            if (inPrefab != null)
                inDef.Prefab = inPrefab;

            RingBuffer<ValidEatTarget> aliveEatTargets = new RingBuffer<ValidEatTarget>();
            RingBuffer<ValidEatTarget> stressedEatTargets = new RingBuffer<ValidEatTarget>();

            foreach(var eat in inBestiary.FactsOfType<BFEat>())
            {
                if (eat.Target() == inBestiary)
                    continue;

                if (eat.OnlyWhenStressed())
                {
                    CreateOrOverwriteTarget(stressedEatTargets, eat.Target());
                }
                else
                {
                    CreateOrOverwriteTarget(aliveEatTargets, eat.Target());
                    CreateTargetOnly(stressedEatTargets, eat.Target());
                }
            }

            inDef.AliveEatTargets = aliveEatTargets.ToArray();
            inDef.StressedEatTargets = stressedEatTargets.ToArray();

            if (inDef.SpawnAmountOverride > 0)
                inDef.SpawnCount = inDef.SpawnAmountOverride;
            else
                inDef.SpawnCount = GetDefaultSpawnAmount(inBestiary.Size());
        }

        static private int GetDefaultSpawnAmount(BestiaryDescSize inSize)
        {
            switch(inSize)
            {
                case BestiaryDescSize.Tiny:
                    return 10;
                case BestiaryDescSize.Small:
                    return 8;
                case BestiaryDescSize.Medium:
                    return 4;
                case BestiaryDescSize.Large:
                    return 1;
                default:
                    Assert.Fail("Invalid critter size {0}", inSize);
                    return 0;
            }
        }

        static private void CreateOrOverwriteTarget(RingBuffer<ValidEatTarget> ioTargets, BestiaryDesc inTarget)
        {
            StringHash32 id = inTarget.Id();
            for(int i = 0, length = ioTargets.Count; i < length; i++)
            {
                if (ioTargets[i].TargetId == id)
                    return;
            }

            ValidEatTarget newTarget = new ValidEatTarget();
            newTarget.TargetId = inTarget.name;
            ioTargets.PushBack(newTarget);
        }

        static private bool CreateTargetOnly(RingBuffer<ValidEatTarget> ioTargets, BestiaryDesc inTarget)
        {
            StringHash32 id = inTarget.Id();
            for(int i = 0, length = ioTargets.Count; i < length; i++)
            {
                if (ioTargets[i].TargetId == id)
                    return false;
            }

            ValidEatTarget newTarget = new ValidEatTarget();
            newTarget.TargetId = inTarget.name;
            ioTargets.PushBack(newTarget);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}