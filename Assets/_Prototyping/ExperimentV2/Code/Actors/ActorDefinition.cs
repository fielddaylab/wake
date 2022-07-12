using System;
using Aqua;
using BeauData;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.ExperimentV2 {
    [Serializable]
    public class ActorDefinition
        #if UNITY_EDITOR
        : ISerializedObject
        #endif // UNITY_EDITOR
    {
        #region Types

        public enum SpawnPositionId : byte {
            Anywhere, // actor spawns anywhere
            Bottom, // actor spawns on the bottom of the tank
            Top, // actor spawns on the water surface of the tank
            Center // actor spawns in the center of the tank
        }

        public enum SpawnAnimationId : byte {
            Drop,
            Sprout,
            Expand
        }

        [Serializable]
        public struct SpawnConfiguration {
            [Tooltip("Where the actor's spawn target will be located")]
            [AutoEnum] public SpawnPositionId SpawnLocation;

            [Tooltip("How the actor will animate to its spawn target")]
            [AutoEnum] public SpawnAnimationId SpawnAnimation;

            [Tooltip("Distance to offset the actor from the left and right sides of the tank")]
            public float AvoidTankSidesRadius;

            [Tooltip("Distance to offset the actor from the top and bottom of the tank")]
            public float AvoidTankTopBottomRadius;

            static internal readonly SpawnConfiguration Default = new SpawnConfiguration() {
                SpawnLocation = SpawnPositionId.Anywhere,
                SpawnAnimation = SpawnAnimationId.Drop,
                AvoidTankSidesRadius = 0.2f,
                AvoidTankTopBottomRadius = 0
            };

            static internal void ReplaceDefaults(ref SpawnConfiguration config) {
                if (config.Equals(default)) {
                    config = Default;
                }
            }

            private bool Equals(SpawnConfiguration other) {
                return SpawnLocation == other.SpawnLocation
                    && SpawnAnimation == other.SpawnAnimation
                    && AvoidTankSidesRadius == other.AvoidTankSidesRadius
                    && AvoidTankTopBottomRadius == other.AvoidTankTopBottomRadius;
            }
        }

        public enum MovementTypeId : byte {
            Stationary, // actor does not move
            Swim, // actor swims
        }

        [Serializable]
        public struct MovementConfiguration {
            [Tooltip("How the organism moves")]
            [AutoEnum] public MovementTypeId MoveType;

            [Tooltip("Distance the actor will try to move during an idle movement")]
            public float MovementIdleDistance;

            [Tooltip("Speed at which actor will move for an idle movement")]
            public float MovementSpeed;

            [Tooltip("Easing function to use for idle movement")]
            public Curve MovementCurve;

            [Tooltip("Minimum seconds between idle movements")]
            public float MovementInterval;

            [Tooltip("Maximum amount of random additional seconds between idle movements")]
            public float MovementIntervalRandom;

            static internal readonly MovementConfiguration Default = new MovementConfiguration() {
                MoveType = MovementTypeId.Swim,
                MovementIdleDistance = 1,
                MovementSpeed = 1,
                MovementCurve = Curve.Smooth,
                MovementInterval = 1.5f,
                MovementIntervalRandom = 1
            };

            static internal void ReplaceDefaults(ref MovementConfiguration config) {
                if (config.Equals(default)) {
                    config = Default;
                }
            }

            private bool Equals(MovementConfiguration other) {
                return MoveType == other.MoveType
                    && MovementIdleDistance == other.MovementIdleDistance
                    && MovementSpeed == other.MovementSpeed
                    && MovementCurve == other.MovementCurve
                    && MovementInterval == other.MovementInterval
                    && MovementIntervalRandom == other.MovementIntervalRandom;
            }
        }

        public enum EatTypeId : byte {
            None, // no eating
            Nibble, // several small bites
            LargeBite, // large bite,
            Filter, // filters
        }

        [Serializable]
        public struct EatingConfiguration {
            [Tooltip("How the organism eats")]
            [AutoEnum] public EatTypeId EatType;

            [Tooltip("Multiplier on movement speed when moving towards an eat target")]
            public float MovementMultiplier;

            static internal readonly EatingConfiguration Default = new EatingConfiguration() {
                EatType = EatTypeId.Nibble,
                MovementMultiplier = 1
            };

            static internal void ReplaceDefaults(ref EatingConfiguration config) {
                if (config.Equals(default)) {
                    config = Default;
                }

                if (config.MovementMultiplier == 0) {
                    config.MovementMultiplier = 1;
                }
            }

            private bool Equals(EatingConfiguration other) {
                return EatType == other.EatType
                    && MovementMultiplier == other.MovementMultiplier;
            }
        }

        [Serializable]
        public struct StressConfiguration {
            [Tooltip("Multiplier on movement speed when stressed")]
            public float MovementSpeedMultiplier;

            [Tooltip("Multiplier on idle movement interval when stressed")]
            public float MovementIntervalMultiplier;

            [Tooltip("Multiplier on ambient animation speed when stressed")]
            public float AmbientAnimationSpeedMultiplier;

            static internal readonly StressConfiguration Default = new StressConfiguration() {
                MovementSpeedMultiplier = 0.8f,
                MovementIntervalMultiplier = 0.9f,
                AmbientAnimationSpeedMultiplier = 1
            };

            static internal void ReplaceDefaults(ref StressConfiguration config) {
                if (config.Equals(default)) {
                    config = Default;
                }

                if (config.MovementSpeedMultiplier == 0) {
                    config.MovementSpeedMultiplier = Default.MovementSpeedMultiplier;
                }

                if (config.MovementIntervalMultiplier == 0) {
                    config.MovementIntervalMultiplier = Default.MovementIntervalMultiplier;
                }
            }

            private bool Equals(StressConfiguration other) {
                return MovementSpeedMultiplier == other.MovementSpeedMultiplier
                    && MovementIntervalMultiplier == other.MovementIntervalMultiplier
                    && AmbientAnimationSpeedMultiplier == other.AmbientAnimationSpeedMultiplier;
            }
        }

        [Serializable]
        public struct ValidInteractionTarget {
            [FilterBestiaryId(BestiaryDescCategory.Critter)] public StringHash32 TargetId;
            [FactId] public StringHash32 FactId;
        }

        #endregion // Types

        [HideInInspector] public StringHash32 Id;
        [FilterBestiary(BestiaryDescCategory.Critter)] public BestiaryDesc Type;
        public string PrefabName;

        [Tooltip("If greater than 0, this sets the number of actors to spawn directly\nActor spawn counts are normally determined by the organism type's size property")]
        public int SpawnAmountOverride = -1;
        
        [Header("Behavior")]

        public SpawnConfiguration Spawning = SpawnConfiguration.Default;
        public MovementConfiguration Movement = MovementConfiguration.Default;
        public EatingConfiguration Eating = EatingConfiguration.Default;
        public StressConfiguration Stress = StressConfiguration.Default;

        [Header("Derived From Facts")]

        public ActorStateTransitionSet StateEvaluator;
        public ValidInteractionTarget[] AliveEatTargets;
        public ValidInteractionTarget[] StressedEatTargets;
        public ValidInteractionTarget[] ParasiteTargets;
        public int SpawnCount;
        public int SpawnMax;
        public float AliveReproduceRate;
        public float StressedReproduceRate;
        public bool IsPlant;
        public bool IsLivingOrganism;
        public bool IsDistributed;
        public int TargetLimit;
        public bool FreeOnEaten;
        public Rect EatOffsetRange;
        public Rect LocalBoundsRect;

        [NonSerialized] public ActorInstance Prefab;

        #region Utility

        static public Vector3 FindRandomLocationOnBottom(System.Random inRandom, in Bounds inBounds) {
            Vector3 center = inBounds.center;
            Vector3 extents = inBounds.extents;
            float bottomY = center.y - extents.y;
            float limitX = extents.x;
            return new Vector3(center.x + inRandom.NextFloat(-limitX, limitX), bottomY, center.z);
        }

        static public Vector3 FindRandomLocationOnTop(System.Random inRandom, in Bounds inBounds) {
            Vector3 center = inBounds.center;
            Vector3 extents = inBounds.extents;
            float topY = center.y + extents.y;
            float limitX = extents.x;
            return new Vector3(center.x + inRandom.NextFloat(-limitX, limitX), topY, center.z);
        }

        static public Vector3 FindRandomLocationInTank(System.Random inRandom, in Bounds inBounds) {
            Vector3 center = inBounds.center;
            Vector3 extents = inBounds.extents;
            float limitX = extents.x;
            float limitY = extents.y;
            return new Vector3(center.x + inRandom.NextFloat(-limitX, limitX), center.y + inRandom.NextFloat(-limitY, limitY), center.z);
        }

        static public Vector3 FindRandomSpawnLocation(System.Random inRandom, in Bounds inBounds, in SpawnConfiguration inConfiguration) {
            switch (inConfiguration.SpawnLocation) {
                case SpawnPositionId.Bottom:
                    return FindRandomLocationOnBottom(inRandom, inBounds);

                case SpawnPositionId.Top:
                    return FindRandomLocationOnTop(inRandom, inBounds);

                case SpawnPositionId.Anywhere:
                    return FindRandomLocationInTank(inRandom, inBounds);

                case SpawnPositionId.Center:
                    return inBounds.center;

                default:
                    Assert.Fail("Unknown spawn type {0}", inConfiguration.SpawnLocation);
                    return default(Vector3);
            }
        }

        static public Vector3 FindRandomTankLocationInRange(System.Random inRandom, in Bounds inBounds, Vector3 inCurrentPosition, float inDistance) {
            Vector3 center = inBounds.center;
            Vector3 extents = inBounds.extents;
            float limitX = extents.x;
            float limitY = extents.y;

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

        static public Bounds GenerateTankBounds(Bounds inTankBounds, Rect inColliderBounds, float inTopBottomOffset, float inSidesOffset) {
            Vector3 center = inTankBounds.center;
            Vector3 extents = inTankBounds.extents;
            center.x -= inColliderBounds.center.x;
            center.y -= inColliderBounds.center.y;
            float left = center.x - extents.x + inSidesOffset + (inColliderBounds.width / 2);
            float right = center.x + extents.x - inSidesOffset - (inColliderBounds.width / 2);
            float top = center.y + extents.y - inTopBottomOffset - (inColliderBounds.height / 2);
            float bottom = center.y - extents.y + inTopBottomOffset + (inColliderBounds.height / 2);

            center.x = (left + right) / 2;
            center.y = (top + bottom) / 2;
            extents.x = Math.Max(0, right - left) / 2;
            extents.y = Math.Max(0, top - bottom) / 2;
            return new Bounds(center, extents * 2);
        }

        static public Vector3 ClampToTank(in Bounds inBounds, Vector3 inCurrentPosition) {
            Vector3 center = inBounds.center;
            Vector3 extents = inBounds.extents;
            float limitX = extents.x;
            float limitY = extents.y;

            Vector3 currentOffset = inCurrentPosition;
            currentOffset.x = Mathf.Clamp(currentOffset.x, center.x - limitX, center.x + limitY);
            currentOffset.y = Mathf.Clamp(currentOffset.y, center.y - limitY, center.y + limitY);
            return currentOffset;
        }

        static public ValidInteractionTarget[] GetEatTargets(ActorDefinition inDefinition, ActorStateId inStateId) {
            switch (inStateId) {
                case ActorStateId.Dead:
                default:
                    return Array.Empty<ValidInteractionTarget>();
                case ActorStateId.Alive:
                    return inDefinition.AliveEatTargets;
                case ActorStateId.Stressed:
                    return inDefinition.StressedEatTargets;
            }
        }

        static public ValidInteractionTarget GetEatTarget(ActorDefinition inDefinition, StringHash32 inTargetId, ActorStateId inStateId) {
            var targets = GetEatTargets(inDefinition, inStateId);
            for (int i = 0; i < targets.Length; i++) {
                if (targets[i].TargetId == inTargetId)
                    return targets[i];
            }

            return default(ValidInteractionTarget);
        }

        static public ValidInteractionTarget GetParasiteTarget(ActorDefinition inDefinition, StringHash32 inTargetId) {
            var targets = inDefinition.ParasiteTargets;
            for (int i = 0; i < targets.Length; i++) {
                if (targets[i].TargetId == inTargetId)
                    return targets[i];
            }

            return default(ValidInteractionTarget);
        }

        static public float GetMovementSpeedMultiplier(ActorDefinition inDefinition, ActorStateId inStateId) {
            switch (inStateId) {
                case ActorStateId.Alive:
                    return 1;
                case ActorStateId.Stressed:
                    return inDefinition.Stress.MovementSpeedMultiplier;
                case ActorStateId.Dead:
                default:
                    return 0;
            }
        }

        static public float GetMovementIntervalMultiplier(ActorDefinition inDefinition, ActorStateId inStateId) {
            switch (inStateId) {
                case ActorStateId.Alive:
                    return 1;
                case ActorStateId.Stressed:
                    return inDefinition.Stress.MovementIntervalMultiplier;
                case ActorStateId.Dead:
                default:
                    return 0;
            }
        }

        static public float GetReproductionRate(ActorDefinition inDefinition, ActorStateId inStateId) {
            switch (inStateId) {
                case ActorStateId.Alive:
                    return inDefinition.AliveReproduceRate;
                case ActorStateId.Stressed:
                    return inDefinition.StressedReproduceRate;
                case ActorStateId.Dead:
                default:
                    return 0;
            }
        }

        #endregion // Utility

        #region Editor

        #if UNITY_EDITOR

        static internal void OverwriteFromSerialized(ActorDefinition inSource, ActorDefinition inTarget) {
            inTarget.SpawnAmountOverride = inSource.SpawnAmountOverride;

            inTarget.Spawning.SpawnLocation = inSource.Spawning.SpawnLocation;
            inTarget.Spawning.SpawnAnimation = inSource.Spawning.SpawnAnimation;
            inTarget.Spawning.AvoidTankSidesRadius = inSource.Spawning.AvoidTankSidesRadius;
            inTarget.Spawning.AvoidTankTopBottomRadius = inSource.Spawning.AvoidTankTopBottomRadius;

            inTarget.Movement.MoveType = inSource.Movement.MoveType;
            inTarget.Movement.MovementIdleDistance = inSource.Movement.MovementIdleDistance;
            inTarget.Movement.MovementSpeed = inSource.Movement.MovementSpeed;
            inTarget.Movement.MovementCurve = inSource.Movement.MovementCurve;
            inTarget.Movement.MovementInterval = inSource.Movement.MovementInterval;
            inTarget.Movement.MovementIntervalRandom = inSource.Movement.MovementIntervalRandom;

            inTarget.Eating.EatType = inSource.Eating.EatType;
            inTarget.Eating.MovementMultiplier = inSource.Eating.MovementMultiplier;

            inTarget.Stress.MovementSpeedMultiplier = inSource.Stress.MovementSpeedMultiplier;
            inTarget.Stress.MovementIntervalMultiplier = inSource.Stress.MovementIntervalMultiplier;
            inTarget.Stress.AmbientAnimationSpeedMultiplier = inSource.Stress.AmbientAnimationSpeedMultiplier;
        }

        static internal void LoadFromBestiary(ActorDefinition inDef, BestiaryDesc inBestiary, ActorInstance inPrefab) {
            Assert.NotNull(inDef);
            Assert.NotNull(inBestiary);
            Assert.True(inBestiary.Category() == BestiaryDescCategory.Critter, "Provided entry is not for a critter");
            Assert.True(!inBestiary.HasFlags(BestiaryDescFlags.DoNotUseInExperimentation), "Provided entry is not usable in experimentation");

            inDef.Id = inBestiary.Id();
            inDef.Type = inBestiary;
            inDef.StateEvaluator = inBestiary.GetActorStateTransitions();
            inDef.IsPlant = inBestiary.HasFlags(BestiaryDescFlags.TreatAsPlant);
            inDef.IsLivingOrganism = !inBestiary.HasFlags(BestiaryDescFlags.IsNotLiving);
            inDef.IsDistributed = inPrefab && inPrefab.DistributedParticles;
            inDef.FreeOnEaten = !inDef.IsDistributed && !inDef.IsPlant && !inBestiary.HasFlags(BestiaryDescFlags.TreatAsHerd);
            inDef.TargetLimit = inDef.IsDistributed ? 8 : (inDef.IsPlant ? 4 : 1);

            if (inDef.IsDistributed) {
                inDef.SpawnCount = 1;
            } else if (inDef.SpawnAmountOverride > 0) {
                inDef.SpawnCount = inDef.SpawnAmountOverride;
            } else {
                inDef.SpawnCount = GetDefaultSpawnAmount(inBestiary.Size());
            }

            inDef.SpawnMax = Math.Min(inDef.SpawnCount * 3, GetDefaultSpawnMax(inBestiary.Size()));

            RingBuffer<ValidInteractionTarget> aliveEatTargets = new RingBuffer<ValidInteractionTarget>();
            RingBuffer<ValidInteractionTarget> stressedEatTargets = new RingBuffer<ValidInteractionTarget>();
            RingBuffer<ValidInteractionTarget> parasiteTargets = new RingBuffer<ValidInteractionTarget>();

            foreach (var eat in inBestiary.FactsOfType<BFEat>()) {
                if (eat.Critter == inBestiary)
                    continue;

                if (eat.OnlyWhenStressed) {
                    CreateOrOverwriteTarget(stressedEatTargets, eat.Critter, eat);
                } else {
                    CreateOrOverwriteTarget(aliveEatTargets, eat.Critter, eat);
                    CreateTargetOnly(stressedEatTargets, eat.Critter, eat);
                }
            }

            foreach(var parasite in inBestiary.FactsOfType<BFParasite>()) {
                if (parasite.Critter == inBestiary) {
                    continue;
                }

                parasiteTargets.PushBack(new ValidInteractionTarget() {
                    TargetId = parasite.Critter.Id(),
                    FactId = parasite.Id
                });
            }

            inDef.AliveEatTargets = aliveEatTargets.ToArray();
            inDef.StressedEatTargets = stressedEatTargets.ToArray();
            inDef.ParasiteTargets = parasiteTargets.ToArray();

            bool hasEat = (aliveEatTargets.Count + stressedEatTargets.Count) > 0;
            bool hasParasite = parasiteTargets.Count > 0;

            if (hasParasite && !inDef.IsDistributed) {
                Log.Error("[ActorDefinition] Actor '{0}' has parasite interactions but is not a distributed organism. Parasite interactions should only be present for distributed/microscopic");
                inDef.ParasiteTargets = Array.Empty<ValidInteractionTarget>();
                hasParasite = false;
            }

            BFBody body = inBestiary.FactOfType<BFBody>();

            float aliveRepro = 0,
                stressedRepro = 0;

            BFReproduce repro = BestiaryUtils.FindReproduceRule(inBestiary, ActorStateId.Alive);
            BFGrow grow = BestiaryUtils.FindGrowRule(inBestiary, ActorStateId.Alive);

            if (repro) {
                aliveRepro += repro.Amount;
            }
            if (grow) {
                aliveRepro += (float)grow.Amount / body.MassPerPopulation;
            }

            BFReproduce reproStress = BestiaryUtils.FindReproduceRule(inBestiary, ActorStateId.Stressed);
            BFGrow growStress = BestiaryUtils.FindGrowRule(inBestiary, ActorStateId.Stressed);

            if (reproStress) {
                stressedRepro += reproStress.Amount;
            }
            if (growStress) {
                stressedRepro += (float) growStress.Amount / body.MassPerPopulation;
            }

            if (!reproStress && !growStress) {
                stressedRepro += aliveRepro;
            }

            inDef.AliveReproduceRate = aliveRepro;
            inDef.StressedReproduceRate = stressedRepro;

            SpawnConfiguration.ReplaceDefaults(ref inDef.Spawning);
            MovementConfiguration.ReplaceDefaults(ref inDef.Movement);
            EatingConfiguration.ReplaceDefaults(ref inDef.Eating);
            StressConfiguration.ReplaceDefaults(ref inDef.Stress);

            if (inDef.IsPlant || !inDef.IsLivingOrganism) {
                inDef.Movement.MoveType = MovementTypeId.Stationary;
            }

            if (inDef.IsDistributed) {
                inDef.Movement.MoveType = MovementTypeId.Stationary;
                inDef.Spawning.SpawnLocation = SpawnPositionId.Center;
                inDef.Spawning.SpawnAnimation = SpawnAnimationId.Expand;
            }

            if (!hasEat || !inDef.IsLivingOrganism) {
                inDef.Eating.EatType = EatTypeId.None;
            }

            if (inDef.Movement.MoveType == MovementTypeId.Stationary && hasEat) {
                inDef.Eating.EatType = EatTypeId.Filter;
            }

            if (inPrefab != null) {
                ProcessPrefab(inDef, inBestiary, inPrefab);
            } else if (string.IsNullOrEmpty(inDef.PrefabName)) {
                Log.Warn("[ActorDefinition] Experiment-able organism '{0}' lacks a prefab", inDef.Id);
            }
        }

        static private void ProcessPrefab(ActorDefinition inDef, BestiaryDesc inBestiary, ActorInstance inPrefab) {
            inDef.PrefabName = inPrefab.name;
            inDef.Prefab = inPrefab;

            Bounds bounds = PhysicsUtils.GetLocalBounds(inPrefab.CachedCollider);
            bounds.center += inPrefab.CachedCollider.transform.position;
            inDef.LocalBoundsRect = Geom.BoundsToRect(bounds);

            if (!inPrefab.EatCollider) {
                inPrefab.EatCollider = inPrefab.CachedCollider;
                UnityEditor.EditorUtility.SetDirty(inPrefab);
            }

            bounds = PhysicsUtils.GetLocalBounds(inPrefab.EatCollider); 
            bounds.center += inPrefab.EatCollider.transform.position;

            if (inPrefab.EatCollider == inPrefab.CachedCollider && !inDef.IsDistributed) {
                bounds.extents *= 0.9f;
            }
            inDef.EatOffsetRange = Geom.BoundsToRect(bounds);
        }

        static private int GetDefaultSpawnAmount(BestiaryDescSize inSize) {
            switch (inSize) {
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

        static private int GetDefaultSpawnMax(BestiaryDescSize inSize) {
            switch (inSize) {
                case BestiaryDescSize.Tiny:
                    return 40;
                case BestiaryDescSize.Small:
                    return 30;
                case BestiaryDescSize.Medium:
                    return 20;
                case BestiaryDescSize.Large:
                    return 10;
                default:
                    Assert.Fail("Invalid critter size {0}", inSize);
                    return 0;
            }
        }

        static private void CreateOrOverwriteTarget(RingBuffer<ValidInteractionTarget> ioTargets, BestiaryDesc inTarget, BFEat inRule) {
            StringHash32 id = inTarget.Id();
            for (int i = 0, length = ioTargets.Count; i < length; i++) {
                if (ioTargets[i].TargetId == id) {
                    ref ValidInteractionTarget target = ref ioTargets[i];
                    target.FactId = inRule.name;
                    return;
                }
            }

            ValidInteractionTarget newTarget = new ValidInteractionTarget();
            newTarget.TargetId = inTarget.name;
            newTarget.FactId = inRule.name;
            ioTargets.PushBack(newTarget);
        }

        static private bool CreateTargetOnly(RingBuffer<ValidInteractionTarget> ioTargets, BestiaryDesc inTarget, BFEat inRule) {
            StringHash32 id = inTarget.Id();
            for (int i = 0, length = ioTargets.Count; i < length; i++) {
                if (ioTargets[i].TargetId == id)
                    return false;
            }

            ValidInteractionTarget newTarget = new ValidInteractionTarget();
            newTarget.TargetId = inTarget.name;
            newTarget.FactId = inRule.name;
            ioTargets.PushBack(newTarget);
            return true;
        }

        #region ISerializedObject

        public void Serialize(Serializer ioSerializer) {
            ioSerializer.Serialize("spawnAmountOverride", ref SpawnAmountOverride, -1);

            ioSerializer.Enum("spawnLocation", ref Spawning.SpawnLocation, SpawnPositionId.Anywhere);
            ioSerializer.Enum("spawnAnimation", ref Spawning.SpawnAnimation, SpawnAnimationId.Drop);
            ioSerializer.Serialize("sidesRadius", ref Spawning.AvoidTankSidesRadius, 0.5f);
            ioSerializer.Serialize("topBottomRadius", ref Spawning.AvoidTankTopBottomRadius, 0.5f);

            ioSerializer.Enum("moveType", ref Movement.MoveType, MovementTypeId.Swim);
            if (Movement.MoveType != MovementTypeId.Stationary) {
                ioSerializer.Serialize("moveDistance", ref Movement.MovementIdleDistance, 1);
                ioSerializer.Serialize("moveSpeed", ref Movement.MovementSpeed, 1);
                ioSerializer.Enum("moveCurve", ref Movement.MovementCurve, Curve.Smooth);
                ioSerializer.Serialize("moveInterval", ref Movement.MovementInterval, 1.5f);
                ioSerializer.Serialize("moveIntervalRandom", ref Movement.MovementIntervalRandom, 1);
            }

            ioSerializer.Enum("eatType", ref Eating.EatType, EatTypeId.Nibble);
            if (Eating.EatType != EatTypeId.None) {
                ioSerializer.Serialize("eatSpeedMultiplier", ref Eating.MovementMultiplier, 1.5f);
            }

            if (Movement.MoveType != MovementTypeId.Stationary) {
                ioSerializer.Serialize("stressSpeedMultiplier", ref Stress.MovementSpeedMultiplier, 0.8f);
                ioSerializer.Serialize("stressMoveIntervalMultiplier", ref Stress.MovementIntervalMultiplier, 0.9f);
            }

            ioSerializer.Serialize("ambientAnimationMultiplier", ref Stress.AmbientAnimationSpeedMultiplier, 1);
        }

        #endregion // ISerializedObject

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}