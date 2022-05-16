using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.ExperimentV2 {
    public sealed class ActorBehaviorSystem : MonoBehaviour, IBaked {
        static private bool s_Configured;
        private const float SpeedUpLifetimeThreshold = 60;

        public delegate bool HasFactDelegate(StringHash32 inFactId);

        private enum Phase {
            Spawning,
            Executing
        }

        [SerializeField] private SelectableTank m_Tank = null;
        [SerializeField, HideInInspector] private ActorAllocator m_Allocator = null;

        [Header("Reproduction Rules")]
        [SerializeField] private float m_ReproPeriod = 0f;

        [NonSerialized] private Phase m_Phase = Phase.Spawning;
        [NonSerialized] public ActorWorld World;
        [NonSerialized] private bool m_AllowTick = false;
        [NonSerialized] private bool m_AllowRepro = false;

        private readonly float[] m_ReproductionSchedule = new float[8];

        public Predicate<ActorActionId> ActionAvailable;
        public Func<bool> ReproAvailable;

        public void Initialize() {
            if (World != null)
                return;

            World = new ActorWorld(m_Allocator, m_Tank.Bounds, null, OnFree, 16, m_Tank, m_Tank.Controller);

            ConfigureStates();
        }

        public void Begin() {
            if (!m_AllowTick) {
                m_AllowTick = true;
            }
        }

        public void End() {
            if (m_AllowTick) {
                m_AllowTick = false;
                World.IsExecuting = false;
                foreach (var actor in World.Actors) {
                    actor.ActionAnimation.Stop();
                }
            }
        }

        private void LateUpdate() {
            if (!m_AllowTick || (m_Tank.CurrentState & TankState.Running) == 0 || Script.IsPaused)
                return;

            TickBehaviors(Time.deltaTime);
        }

        #region Critters

        public void Alloc(StringHash32 inId) {
            ActorWorld.AllocWithDefaultCount(World, inId);
        }

        public void Alloc(StringHash32 inId, int inCount) {
            ActorWorld.Alloc(World, inId, inCount);
        }

        public void FreeAll(StringHash32 inId) {
            ActorWorld.FreeAll(World, inId);
        }

        private void OnFree(ActorInstance inCritter, ActorWorld inWorld) {
            ActorWorld.ModifyPopulation(inWorld, inCritter.Definition.Id, -1);
            ActorInstance.ReleaseTargetsAndInteractions(inCritter, inWorld);
        }

        #endregion // Critters

        #region Env State

        public void UpdateEnvState(WaterPropertyBlockF32 inEnvironment) {
            ActorWorld.SetWaterState(World, inEnvironment);
        }

        public void ClearEnvState() {
            ActorWorld.SetWaterState(World, null);
        }

        #endregion // Env State

        #region Tick

        static public void ConfigureStates() {
            if (s_Configured) {
                return;
            }

            ActorInstance.ConfigureActionMethods(ActorActionId.Idle, OnActorIdleStart, null, null);
            ActorInstance.ConfigureActionMethods(ActorActionId.Hungry, OnActorHungryStart, null, ActorHungryAnimation);
            ActorInstance.ConfigureActionMethods(ActorActionId.Eating, OnActorEatStart, OnActorEatEnd, ActorEatAnimation);
            ActorInstance.ConfigureActionMethods(ActorActionId.BeingEaten, OnActorBeingEatenStart, OnActorBeingEatenEnd, ActorBeingEatenAnimation);
            ActorInstance.ConfigureActionMethods(ActorActionId.Dying, OnActorDyingStart, null, ActorDyingAnimation);
            ActorInstance.ConfigureActionMethods(ActorActionId.Parasiting, OnActorParasiteStart, null, ActorParasiteAnimation);
            ActorInstance.ConfigureInteractionMethods(OnInteractionAcquired, OnInteractionReleased);
            s_Configured = true;
        }

        public void TickBehaviors(float inDeltaTime) {
            if (!IsSpawningComplete()) {
                return;
            }

            World.Lifetime += inDeltaTime;
            if (m_AllowRepro) {
                int count = World.ActorCounts.Count;
                for(int i = 0; i < count; i++) {
                    ref float repro = ref m_ReproductionSchedule[i];
                    if (repro <= World.Lifetime) {
                        PerformReproduction(World, World.ActorCounts[i].Id);
                        repro += m_ReproPeriod * RNG.Instance.NextFloat(0.8f, 1.2f);
                        break;
                    }
                }
            }
        }

        private bool IsSpawningComplete() {
            if (m_Phase == Phase.Spawning) {
                foreach (var critter in World.Actors) {
                    if (critter.CurrentAction == ActorActionId.Spawning)
                        return false;
                }

                FinalizeCritterInitialization();
                m_Phase = Phase.Executing;
                World.IsExecuting = true;
                return true;
            } else {
                return true;
            }
        }

        public bool AllSpawned() {
            foreach (var critter in World.Actors) {
                if (critter.CurrentAction == ActorActionId.Spawning) {
                    return false;
                }
            }
            return true;
        }

        private void FinalizeCritterInitialization() {
            World.Lifetime = 0;
            World.EnvDeaths = 0;

            ActorInstance critter;
            for (int i = World.Actors.Count - 1; i >= 0; i--) {
                critter = World.Actors[i];
                if (critter.CurrentState == ActorStateId.Dead) {
                    ActorInstance.SetActorAction(critter, ActorActionId.Dying, World);
                } else {
                    ActorInstance.SetActorAction(critter, ActorActionId.Idle, World);
                }
            }

            ActorWorld.RegenerateActorCounts(World);

            Array.Clear(m_ReproductionSchedule, 0, m_ReproductionSchedule.Length);

            m_AllowRepro = m_ReproPeriod > 0 && (ReproAvailable == null || ReproAvailable());
            if (m_AllowRepro) {
                int count = World.ActorCounts.Count;
                for(int i = 0; i < count; i++) {
                    m_ReproductionSchedule[i] = RNG.Instance.NextFloat(0.3f, 0.7f) * m_ReproPeriod;
                }
            }
        }

        static private void PerformReproduction(ActorWorld inWorld, StringHash32 inId) {
            var definition = inWorld.Allocator.Define(inId);
            int pop = (int) ActorWorld.GetPopulation(inWorld, inId);
            if (pop <= 0) {
                return;
            }

            int aliveCount = 0, stressCount = 0;
            ActorInstance firstInstance = null;
            foreach(var actor in inWorld.Actors) {
                if (actor.Definition.Id != inId)
                    continue;

                if (!firstInstance) {
                    firstInstance = actor;
                }
                
                switch(actor.CurrentState) {
                    case ActorStateId.Alive: {
                        aliveCount++;
                        break;
                    }
                    case ActorStateId.Stressed: {
                        stressCount++;
                        break;
                    }
                }
            }

            int desired = Math.Min(definition.SpawnMax, pop + (int) Math.Ceiling(aliveCount * definition.AliveReproduceRate + stressCount * definition.StressedReproduceRate));
            int newSpawn = desired - pop;
            if (newSpawn > 0) {
                if (definition.IsDistributed) {
                    firstInstance.DistributedParticles.Emit(newSpawn * 32);
                    ActorWorld.EmitEmoji(inWorld, firstInstance, SelectableTank.Emoji_Reproduce, null, newSpawn * 32);
                } else {
                    ActorWorld.Alloc(inWorld, inId, newSpawn, ActorActionId.BeingBorn);
                }
            }
        }

        static public int GetPotentialNewObservations(ActorWorld inWorld, HasFactDelegate inDelegate, ICollection<BFBase> outFactIds) {
            Assert.NotNull(inDelegate);

            int factCount = 0;
            ActorDefinition def;
            ActorStateId state;
            ActorDefinition.ValidInteractionTarget[] possibleEats;
            HashSet<StringHash32> reevaluate = null;
            foreach (var critterCount in inWorld.ActorCounts) {
                if (critterCount.Population == 0)
                    continue;

                def = inWorld.Allocator.Define(critterCount.Id);
                state = def.StateEvaluator.Evaluate(inWorld.Water);
                possibleEats = ActorDefinition.GetEatTargets(def, state);

                foreach (var eat in possibleEats) {
                    if (inDelegate(eat.FactId) || ActorWorld.GetPopulation(inWorld, eat.TargetId) == 0)
                        continue;

                    factCount++;
                    if (outFactIds != null) {
                        outFactIds.Add(Assets.Fact(eat.FactId));
                    }
                }

                foreach(var fact in def.ParasiteTargets) {
                    if (inDelegate(fact.FactId) || ActorWorld.GetPopulation(inWorld, fact.TargetId) == 0)
                        continue;

                    factCount++;
                    if (outFactIds != null) {
                        outFactIds.Add(Assets.Fact(fact.FactId));
                    }

                    // we may need to reevaluate our set if the parasite target
                    // is alive now but can become stressed mid-experiment

                    if (reevaluate == null) {
                        reevaluate = new HashSet<StringHash32>();
                    }

                    reevaluate.Add(fact.TargetId);
                }
            }

            if (reevaluate != null) {
                foreach(var id in reevaluate) {
                    def = inWorld.Allocator.Define(id);
                    state = def.StateEvaluator.Evaluate(inWorld.Water);

                    // if we're already stressed, then the
                    // stressed versions of our facts have already been accounted for
                    if (state == ActorStateId.Stressed) {
                        continue;
                    }

                    possibleEats = ActorDefinition.GetEatTargets(def, ActorStateId.Stressed);

                    foreach (var eat in possibleEats) {
                        if (inDelegate(eat.FactId) || ActorWorld.GetPopulation(inWorld, eat.TargetId) == 0)
                            continue;

                        factCount++;
                        if (outFactIds != null) {
                            outFactIds.Add(Assets.Fact(eat.FactId));
                        }
                    }
                }
            }

            return factCount;
        }

        public bool IsFactObservable(BFBase inFact) {
            switch (inFact.Type) {
                case BFTypeId.Eat: {
                        BFEat eat = (BFEat)inFact;
                        return ActorWorld.GetPopulation(World, eat.Parent.Id()) > 0 && ActorWorld.GetPopulation(World, eat.Critter.Id()) > 0;
                    }
            }

            return false;
        }

        #endregion // Tick

        #region Actor States

        #region Dying

        static private void OnActorDyingStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev) {
            ActorInstance.ReleaseTargetsAndInteractions(inActor, inWorld);
            inActor.BreathAnimation.Stop();
            if (inActor.CurrentState == ActorStateId.Dead) {
                inWorld.EnvDeaths++;
            }
        }

        static private IEnumerator ActorDyingAnimation(ActorInstance inActor, ActorWorld inWorld) {
            yield return Tween.Color(inActor.ColorAdjust.Color, Color.black, inActor.ColorAdjust.SetColor, 1);
            ActorWorld.EmitEmoji(inWorld, inActor, SelectableTank.Emoji_Death, null, 5);
            yield return Tween.Float(1, 0, inActor.ColorAdjust.SetAlpha, 0.5f);
            ActorWorld.EmitEmoji(inWorld, inActor, SelectableTank.Emoji_Death, null, 12);
            ActorWorld.Free(inWorld, inActor);
        }

        #endregion // Dying

        #region Idle

        static private void OnActorIdleStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev) {
            ActorInstance.ReleaseInteraction(inActor, inWorld);
            ActorInstance.ReleaseTarget(inActor, inWorld);

            if (inActor.Definition.Movement.MoveType != ActorDefinition.MovementTypeId.Stationary) {
                inActor.ActionAnimation.Replace(inActor, ActorIdleAnimation(inActor, inWorld));
            } else if (ActorWorld.IsActionAvailable(ActorActionId.Hungry, inWorld) && inActor.Definition.Eating.EatType != ActorDefinition.EatTypeId.None) {
                inActor.ActionAnimation.Replace(inActor, ActorIdleStatic(inActor, inWorld, ActorActionId.Hungry));
            } else if (ActorWorld.IsActionAvailable(ActorActionId.Parasiting, inWorld) && inActor.Definition.ParasiteTargets.Length > 0) {
                inActor.ActionAnimation.Replace(inActor, ActorIdleStatic(inActor, inWorld, ActorActionId.Parasiting));
            }

            ActorInstance.StartBreathing(inActor, inWorld);
        }

        static private IEnumerator ActorIdleAnimation(ActorInstance inActor, ActorWorld inWorld) {
            ActorDefinition def = inActor.Definition;
            bool bLimitMovement = ActorWorld.IsActionAvailable(ActorActionId.Hungry, inWorld) && inActor.Definition.Eating.EatType != ActorDefinition.EatTypeId.None;

            float intervalMultiplier = ActorDefinition.GetMovementIntervalMultiplier(def, inActor.CurrentState);
            float movementSpeed = def.Movement.MovementSpeed * ActorDefinition.GetMovementSpeedMultiplier(def, inActor.CurrentState);
            int moveCount;
            if (!bLimitMovement) {
                moveCount = 0;
            } else {
                if (inWorld.Lifetime >= SpeedUpLifetimeThreshold) {
                    moveCount = RNG.Instance.Next(0, 2);
                } else {
                    moveCount = RNG.Instance.Next(1, 3);
                }
            }
            Vector3 current;
            Vector3 target;
            float duration;
            float interval = intervalMultiplier * RNG.Instance.NextFloat() * (def.Movement.MovementInterval + def.Movement.MovementIntervalRandom);
            yield return interval;

            while (!bLimitMovement || moveCount-- > 0) {
                current = inActor.CachedTransform.localPosition;
                target = ActorDefinition.FindRandomTankLocationInRange(RNG.Instance, inActor.TankBounds, inActor.CachedTransform.localPosition, def.Movement.MovementIdleDistance);
                duration = Vector3.Distance(current, target) / movementSpeed;
                interval = intervalMultiplier * (def.Movement.MovementInterval + RNG.Instance.NextFloat(def.Movement.MovementIntervalRandom));

                yield return inActor.CachedTransform.MoveTo(target, duration, Axis.XY, Space.Self).Ease(def.Movement.MovementCurve);
                yield return interval;

                if (inWorld.Lifetime >= SpeedUpLifetimeThreshold && moveCount > 1) {
                    moveCount = 1;
                }
            }

            ActorInstance.SetActorAction(inActor, ActorActionId.Hungry, inWorld);
        }

        static private IEnumerator ActorIdleStatic(ActorInstance inActor, ActorWorld inWorld, ActorActionId inAction) {
            yield return RNG.Instance.Next(3, 10);
            ActorInstance.SetActorAction(inActor, inAction, inWorld);
        }

        #endregion // Idle

        #region Hungry

        static private void OnActorHungryStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev) {
            ActorInstance eatTarget = FindGoodEatTarget(inActor, inWorld);
            if (eatTarget == null || !ActorInstance.AcquireTarget(inActor, eatTarget, inWorld)) {
                ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
            }
        }

        static private IEnumerator ActorHungryAnimation(ActorInstance inActor, ActorWorld inWorld) {
            ActorDefinition def = inActor.Definition;
            ActorHandle target = inActor.CurrentTargetActor;
            ActorInstance targetInstance = target.Get();

            if (!ActorInstance.IsValidTarget(target)) {
                ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
                yield break;
            }

            if (inActor.Definition.Eating.EatType == ActorDefinition.EatTypeId.Filter || targetInstance.Definition.IsDistributed) {
                if (ActorInstance.AcquireInteraction(inActor, targetInstance, inWorld)) {
                    ActorInstance.SetActorAction(inActor, ActorActionId.Eating, inWorld);
                    yield break;
                } else {
                    ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
                    yield break;
                }
            }

            float movementSpeed = def.Movement.MovementSpeed * def.Eating.MovementMultiplier
                * ActorDefinition.GetMovementSpeedMultiplier(def, inActor.CurrentState);

            Vector3 currentPos;
            Vector3 targetPos;
            Vector3 targetPosOffset = FindGoodEatPositionOffset(targetInstance, inActor.CachedTransform.localPosition);
            Vector3 distanceVector;
            while (ActorInstance.IsValidTarget(target)) {
                currentPos = inActor.CachedTransform.localPosition;
                targetPos = targetInstance.CachedTransform.localPosition + targetPosOffset;
                targetPos.z = currentPos.z;
                targetPos = ActorDefinition.ClampToTank(inActor.TankBounds, targetPos);

                distanceVector = targetPos - currentPos;
                if (distanceVector.sqrMagnitude < 0.05f) {
                    if (ActorInstance.AcquireInteraction(inActor, targetInstance, inWorld)) {
                        ActorInstance.SetActorAction(inActor, ActorActionId.Eating, inWorld);
                    } else {
                        ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
                    }
                    yield break;
                } else {
                    float distanceToMove = movementSpeed * Routine.DeltaTime;
                    float distanceScalar = distanceVector.magnitude;
                    if (distanceScalar < movementSpeed) {
                        distanceToMove *= 1f - 0.7f * (distanceScalar / movementSpeed);
                    }
                    currentPos = Vector3.MoveTowards(currentPos, targetPos, distanceToMove);
                    inActor.CachedTransform.SetPosition(currentPos, Axis.XY, Space.Self);
                }

                yield return null;
            }

            ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
        }

        #endregion // Hungry

        #region Eat

        static private void OnActorEatStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev) {
            ActorInstance.ResetAnimationTransform(inActor);

            if (inActor.IdleAnimation)
                inActor.IdleAnimation.AnimationScale = 0;
        }

        static private IEnumerator ActorEatAnimation(ActorInstance inActor, ActorWorld inWorld) {
            ActorHandle target = inActor.CurrentInteractionActor;
            ActorInstance targetInstance = target.Get();
            var targetDefinition = targetInstance.Definition;

            BFEat eatRule = Assets.Fact<BFEat>(ActorDefinition.GetEatTarget(inActor.Definition, targetDefinition.Id, inActor.CurrentState).FactId);

            bool bHas = Save.Bestiary.HasFact(eatRule.Id);
            using (var table = TempVarTable.Alloc()) {
                table.Set("factId", eatRule.Id);
                table.Set("newFact", !bHas);
                Services.Script.TriggerResponse(ExperimentTriggers.CaptureCircleVisible, table);
            }

            using (var capture = ObservationTank.CaptureCircle(eatRule.Id, inActor, inWorld, bHas)) {
                switch (inActor.Definition.Eating.EatType) {
                    case ActorDefinition.EatTypeId.Nibble: {
                            int nibbleCount = RNG.Instance.Next(3, 5);
                            while (nibbleCount-- > 0) {
                                yield return EatPulse(inActor, 0.2f);
                                ActorWorld.EmitEmoji(inWorld, inActor, eatRule, SelectableTank.Emoji_Eat);
                                if (nibbleCount > 0)
                                    yield return RNG.Instance.NextFloat(0.3f, 0.5f);
                            }
                            break;
                        }

                    case ActorDefinition.EatTypeId.Filter: {
                            int nibbleCount = RNG.Instance.Next(3, 5);
                            while (nibbleCount-- > 0) {
                                ActorWorld.EmitEmoji(inWorld, inActor, eatRule, SelectableTank.Emoji_Eat);
                                if (nibbleCount > 0)
                                    yield return RNG.Instance.NextFloat(0.6f, 1.5f);
                            }
                            break;
                        }

                    case ActorDefinition.EatTypeId.LargeBite:
                    default: {
                            yield return EatPulse(inActor, 0.3f);
                            ActorWorld.EmitEmoji(inWorld, inActor, eatRule, SelectableTank.Emoji_Eat);
                            yield return 2;
                            break;
                        }
                }

                if (target.Valid) {
                    if (targetDefinition.FreeOnEaten) {
                        ActorWorld.Free(inWorld, targetInstance);
                    }
                }

                yield return 1;

                if (capture.IsValid()) {
                    bHas = Save.Bestiary.HasFact(eatRule.Id);
                    using (var table = TempVarTable.Alloc()) {
                        table.Set("factId", eatRule.Id);
                        table.Set("newFact", !bHas);
                        Services.Script.TriggerResponse(ExperimentTriggers.CaptureCircleExpired, table);
                    }
                }
            }

            ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
        }

        static private IEnumerator EatPulse(ActorInstance inInstance, float inDuration) {
            yield return inInstance.CachedTransform.ScaleTo(1.05f, inDuration, Axis.XY).Yoyo(true).Ease(Curve.CubeOut).RevertOnCancel();
            Services.Audio.PostEvent("urchin_eat");
        }

        static private void OnActorEatEnd(ActorInstance inActor, ActorWorld inWorld, ActorActionId inNext) {
            if (inActor.IdleAnimation)
                inActor.IdleAnimation.AnimationScale = 1;
        }

        #endregion // Eat

        #region Parasite

        static private void OnActorParasiteStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev) {
            ActorInstance.ResetAnimationTransform(inActor);

            ActorInstance parasiteTarget = FindGoodParasiteTarget(inActor, inWorld);
            if (parasiteTarget == null || !ActorInstance.AcquireTarget(inActor, parasiteTarget, inWorld) || !ActorInstance.AcquireInteraction(inActor, parasiteTarget, inWorld)) {
                ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
            }
        }

        static private IEnumerator ActorParasiteAnimation(ActorInstance inActor, ActorWorld inWorld) {
            ActorHandle target = inActor.CurrentInteractionActor;
            ActorInstance targetInstance = target.Get();
            var targetDefinition = targetInstance.Definition;

            BFParasite parasiteRule = Assets.Fact<BFParasite>(ActorDefinition.GetParasiteTarget(inActor.Definition, targetDefinition.Id).FactId);

            bool bHas = Save.Bestiary.HasFact(parasiteRule.Id);
            using (var table = TempVarTable.Alloc()) {
                table.Set("factId", parasiteRule.Id);
                table.Set("newFact", !bHas);
                Services.Script.TriggerResponse(ExperimentTriggers.CaptureCircleVisible, table);
            }

            using (var capture = ObservationTank.CaptureCircle(parasiteRule.Id, targetInstance, inWorld, bHas)) {
                int nibbleCount = RNG.Instance.Next(3, 5);
                while (nibbleCount-- > 0 && target.Valid) {
                    Services.Audio.PostEvent("Experiment.Parasite");
                    ActorInstance.SetActorState(targetInstance, ActorStateId.Stressed, inWorld);
                    ActorWorld.EmitEmoji(inWorld, targetInstance, parasiteRule, SelectableTank.Emoji_Parasite);
                    if (nibbleCount > 0)
                        yield return RNG.Instance.NextFloat(0.6f, 1.2f);
                }

                if (targetInstance.ParasiteMarker) {
                    targetInstance.ParasiteMarker.SetActive(true);
                }

                yield return 1;

                if (capture.IsValid()) {
                    bHas = Save.Bestiary.HasFact(parasiteRule.Id);
                    using (var table = TempVarTable.Alloc()) {
                        table.Set("factId", parasiteRule.Id);
                        table.Set("newFact", !bHas);
                        Services.Script.TriggerResponse(ExperimentTriggers.CaptureCircleExpired, table);
                    }
                }
            }

            ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
        }

        #endregion // Parasite

        #region Being Eaten

        static private void OnActorBeingEatenStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev) {
            ActorInstance.ReleaseTargetsAndInteractions(inActor, inWorld);
            ActorInstance.ResetAnimationTransform(inActor);

            if (inActor.IdleAnimation)
                inActor.IdleAnimation.AnimationScale = 0;
        }

        static private IEnumerator ActorBeingEatenAnimation(ActorInstance inActor, ActorWorld inWorld) {
            if (!inActor.Definition.IsDistributed) {
                yield return inActor.CachedTransform.MoveTo(inActor.CachedTransform.localPosition.x + 0.01f, 0.15f, Axis.X, Space.Self)
                    .Wave(Wave.Function.Sin, 1).Loop().RevertOnCancel();
            }
        }

        static private void OnActorBeingEatenEnd(ActorInstance inActor, ActorWorld inWorld, ActorActionId inNext) {
            ActorInstance.ResetAnimationTransform(inActor);

            if (inActor.IdleAnimation)
                inActor.IdleAnimation.AnimationScale = 1;
        }

        #endregion // Being Eaten

        static private void OnInteractionAcquired(ActorInstance inActor, ActorWorld inWorld) {
            if (inActor.Definition.IsDistributed) {
                return;
            }

            ActorInstance.SetActorAction(inActor, ActorActionId.BeingEaten, inWorld);
        }

        static private void OnInteractionReleased(ActorInstance inActor, ActorWorld inWorld) {
            if (inActor.IncomingInteractionCount > 0 || inActor.CurrentAction != ActorActionId.BeingEaten)
                return;

            ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
        }

        #endregion // Actor States

        #region Eating

        static private RingBuffer<PriorityValue<ActorInstance>> s_TempTargetBuffer;

        static private ActorInstance FindGoodEatTarget(ActorInstance inInstance, ActorWorld inWorld) {
            RingBuffer<PriorityValue<ActorInstance>> buffer = s_TempTargetBuffer ?? (s_TempTargetBuffer = new RingBuffer<PriorityValue<ActorInstance>>(16, RingBufferMode.Expand));
            ActorDefinition.ValidInteractionTarget[] validTargets = ActorDefinition.GetEatTargets(inInstance.Definition, inInstance.CurrentState);
            buffer.Clear();
            Vector3 instancePosition = inInstance.CachedTransform.localPosition;
            Vector3 critterPosition;
            float critterDistance, priority;

            foreach (var critter in inWorld.Actors) {
                if (!IsValidTarget(validTargets, critter))
                    continue;

                critterPosition = critter.CachedTransform.localPosition;
                critterDistance = Vector3.Distance(instancePosition, critterPosition);
                priority = (critter.Definition.TargetLimit - critter.IncomingTargetCount) * (5 - critterDistance);
                buffer.PushBack(new PriorityValue<ActorInstance>(critter, priority));
            }

            if (buffer.Count == 0) {
                return null;
            } else if (buffer.Count == 1) {
                return buffer.PopFront();
            } else if (inInstance.Definition.Eating.EatType == ActorDefinition.EatTypeId.Filter) {
                ActorInstance actor = RNG.Instance.Choose(buffer);
                buffer.Clear();
                return actor;
            } else {
                buffer.Sort();
                ActorInstance actor = buffer.PopFront().Value;
                buffer.Clear();
                return actor;
            }
        }

        static private Vector3 FindGoodEatPositionOffset(ActorInstance inEatTarget, Vector3 inCurrentPosition) {
            if (inEatTarget.Definition.IsDistributed) {
                return inCurrentPosition;
            } else {
                return RNG.Instance.NextVector2(inEatTarget.Definition.EatOffsetRange);
            }
        }

        static private bool IsValidTarget(ActorDefinition.ValidInteractionTarget[] inTargets, ActorInstance inPossibleTarget) {
            if (!ActorInstance.IsValidTarget(inPossibleTarget))
                return false;

            for (int i = 0, length = inTargets.Length; i < length; i++) {
                if (inTargets[i].TargetId == inPossibleTarget.Definition.Id)
                    return true;
            }

            return false;
        }

        #endregion // Eating

        #region Parasite

        static private ActorInstance FindGoodParasiteTarget(ActorInstance inInstance, ActorWorld inWorld) {
            RingBuffer<PriorityValue<ActorInstance>> buffer = s_TempTargetBuffer ?? (s_TempTargetBuffer = new RingBuffer<PriorityValue<ActorInstance>>(16, RingBufferMode.Expand));
            ActorDefinition.ValidInteractionTarget[] validTargets = inInstance.Definition.ParasiteTargets;
            buffer.Clear();
            float priority;

            foreach (var critter in inWorld.Actors) {
                if (!IsValidTarget(validTargets, critter))
                    continue;

                priority = (critter.Definition.TargetLimit - critter.IncomingTargetCount);
                if (critter.CurrentState == ActorStateId.Alive) {
                    priority += 5;
                }
                priority += RNG.Instance.NextFloat(1, 5);
                buffer.PushBack(new PriorityValue<ActorInstance>(critter, priority));
            }

            if (buffer.Count == 0) {
                return null;
            } else if (buffer.Count == 1) {
                return buffer.PopFront();
            } else {
                buffer.Sort();
                ActorInstance actor = buffer.PopFront().Value;
                buffer.Clear();
                return actor;
            }
        }

        #endregion // Parasite

        public void ClearAll() {
            End();
            if (World != null) {
                ActorWorld.FreeAll(World);
                ActorWorld.SetWaterState(World, null);
            }
            m_Phase = Phase.Spawning;
            World.Lifetime = 0;
            World.EnvDeaths = 0;
            World.IsExecuting = false;
        }

        public void ClearActors() {
            if (World != null) {
                ActorWorld.FreeAll(World);
            }
            m_Phase = Phase.Spawning;
        }

        #region IBaked

#if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags) {
            m_Tank = GetComponentInParent<SelectableTank>();
            m_Allocator = FindObjectOfType<ActorAllocator>();
            return true;
        }

#endif // UNITY_EDITOR

        #endregion // IBaked
    }
}