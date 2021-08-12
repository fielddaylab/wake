using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class ObservationBehaviorSystem : MonoBehaviour, ISceneOptimizable
    {
        private enum Phase
        {
            Spawning,
            Executing
        }

        [SerializeField] private SelectableTank m_Tank = null;
        [SerializeField] private ActorAllocator m_Allocator = null;

        [NonSerialized] private Phase m_Phase = Phase.Spawning;
        [NonSerialized] private ActorWorld m_World;

        public ActorWorld World()
        {
            return m_World ?? (m_World = new ActorWorld(m_Allocator, m_Tank.Bounds, null, OnFree, 16));
        }

        #region Critters

        public void Alloc(StringHash32 inId)
        {
            ActorWorld.AllocWithDefaultCount(World(), inId);
        }

        public void FreeAll(StringHash32 inId)
        {
            ActorWorld.FreeAll(m_World, inId);
        }

        private void OnFree(ActorInstance inCritter, ActorWorld inWorld)
        {
            ActorInstance.ReleaseTargetsAndInteractions(inCritter, inWorld);
        }

        #endregion // Critters

        #region Env State

        public void UpdateEnvState(WaterPropertyBlockF32 inEnvironment)
        {
            ActorWorld.SetWaterState(m_World, inEnvironment);
        }

        public void ClearEnvState()
        {
            ActorWorld.SetWaterState(m_World, null);
        }

        #endregion // Env State

        #region Tick

        static public void ConfigureStates()
        {
            ActorInstance.ConfigureActionMethods(ActorActionId.Idle, OnActorIdleStart, null, null);
            ActorInstance.ConfigureActionMethods(ActorActionId.Hungry, OnActorHungryStart, null, ActorHungryAnimation);
            ActorInstance.ConfigureActionMethods(ActorActionId.Eating, OnActorEatStart, null, ActorEatAnimation);
            ActorInstance.ConfigureActionMethods(ActorActionId.BeingEaten, OnActorBeingEatenStart, OnActorBeingEatenEnd, ActorBeingEatenAnimation);
            ActorInstance.ConfigureActionMethods(ActorActionId.Dying, OnActorDyingStart, null, ActorDyingAnimation);

            ActorInstance.ConfigureInteractionMethods(OnInteractionAcquired, OnInteractionReleased);
        }

        public void TickBehaviors(float inDeltaTime)
        {
            if (!IsSpawningComplete())
                return;

            
        }

        private bool IsSpawningComplete()
        {
            if (m_Phase == Phase.Spawning)
            {
                foreach(var critter in m_World.Actors)
                {
                    if (critter.CurrentAction == ActorActionId.Spawning)
                        return false;
                }

                FinalizeCritterInitialization();
                m_Phase = Phase.Executing;
                return true;
            }
            else
            {
                return true;
            }
        }

        private void FinalizeCritterInitialization()
        {
            ActorInstance critter;
            for(int i = m_World.Actors.Count - 1; i >= 0; i--)
            {
                critter = m_World.Actors[i];
                if (critter.CurrentState == ActorStateId.Dead)
                {
                    ActorInstance.SetActorAction(critter, ActorActionId.Dying, m_World);
                }
                else
                {
                    ActorInstance.SetActorAction(critter, ActorActionId.Idle, m_World);
                }
            }
        }

        #endregion // Tick

        #region Actor States

        #region Dying

        static private void OnActorDyingStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev)
        {
            ActorInstance.ReleaseTargetsAndInteractions(inActor, inWorld);
        }

        static private IEnumerator ActorDyingAnimation(ActorInstance inActor, ActorWorld inWorld)
        {
            yield return Tween.Color(inActor.ColorAdjust.Color, Color.black, inActor.ColorAdjust.SetColor, 1);
            yield return Tween.Float(1, 0, inActor.ColorAdjust.SetAlpha, 0.5f);
            ActorWorld.Free(inWorld, inActor);
        }

        #endregion // Dying

        #region Idle

        static private void OnActorIdleStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev)
        {
            ActorInstance.ReleaseInteraction(inActor, inWorld);
            ActorInstance.ReleaseTarget(inActor, inWorld);

            if (!inActor.Definition.IsPlant && inActor.Definition.Movement.MoveType != ActorDefinition.MovementTypeId.Stationary)
                inActor.ActionAnimation.Replace(inActor, ActorIdleAnimation(inActor, inWorld));
        }

        static private IEnumerator ActorIdleAnimation(ActorInstance inActor, ActorWorld inWorld)
        {
            ActorDefinition def = inActor.Definition;
            bool bLimitMovement = ActorDefinition.GetEatTargets(def, inActor.CurrentState).Length > 0;

            float intervalMultiplier = ActorDefinition.GetMovementIntervalMultiplier(def, inActor.CurrentState);
            float movementSpeed = def.Movement.MovementSpeed * ActorDefinition.GetMovementSpeedMultiplier(def, inActor.CurrentState);
            int moveCount = !bLimitMovement ? 0 : RNG.Instance.Next(2, 4);
            Vector3 current;
            Vector3 target;
            float duration;
            float interval;
            while(!bLimitMovement || moveCount-- > 0)
            {
                current = inActor.CachedTransform.localPosition;
                target = ActorDefinition.FindRandomTankLocationInRange(RNG.Instance, inWorld.WorldBounds, inActor.CachedTransform.localPosition, def.Movement.MovementIdleDistance, def.Spawning.AvoidTankTopBottomRadius, def.Spawning.AvoidTankSidesRadius);
                duration = Vector3.Distance(current, target) / movementSpeed;
                interval = intervalMultiplier * (def.Movement.MovementInterval + RNG.Instance.NextFloat(def.Movement.MovementIntervalRandom));

                yield return inActor.CachedTransform.MoveTo(target, duration, Axis.XY, Space.Self).Ease(def.Movement.MovementCurve);
                yield return interval;
            }

            ActorInstance.SetActorAction(inActor, ActorActionId.Hungry, inWorld);
        }

        #endregion // Idle

        #region Hungry

        static private void OnActorHungryStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev)
        {
            ActorInstance eatTarget = FindGoodEatTarget(inActor, inWorld);
            if (eatTarget == null || !ActorInstance.AcquireTarget(inActor, eatTarget, inWorld))
            {
                ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
            }
        }

        static private IEnumerator ActorHungryAnimation(ActorInstance inActor, ActorWorld inWorld)
        {
            ActorDefinition def = inActor.Definition;
            ActorInstance target = inActor.CurrentTargetActor;

            if (!ActorInstance.IsValidTarget(target))
            {
                ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
                yield break;
            }

            float movementSpeed = def.Movement.MovementSpeed * def.Eating.MovementMultiplier * ActorDefinition.GetMovementSpeedMultiplier(def, inActor.CurrentState);

            Vector3 currentPos;
            Vector3 targetPos;
            Vector3 targetPosOffset = FindGoodEatPositionOffset(target);
            Vector3 distance;
            while(ActorInstance.IsValidTarget(target))
            {
                currentPos = inActor.CachedTransform.localPosition;
                targetPos = target.CachedTransform.localPosition + targetPosOffset;
                targetPos.z = currentPos.z;
                targetPos = ActorDefinition.ClampToTank(inWorld.WorldBounds, targetPos, def.Spawning.AvoidTankTopBottomRadius, def.Spawning.AvoidTankSidesRadius);

                distance = targetPos - currentPos;
                if (distance.sqrMagnitude < 0.1f)
                {
                    if (ActorInstance.AcquireInteraction(inActor, target, inWorld))
                    {
                        ActorInstance.SetActorAction(inActor, ActorActionId.Eating, inWorld);
                        yield break;
                    }
                    else
                    {
                        ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
                    }
                }
                else
                {
                    currentPos = Vector3.MoveTowards(currentPos, targetPos, movementSpeed * Routine.DeltaTime);
                    inActor.CachedTransform.SetPosition(currentPos, Axis.XY, Space.Self);
                }

                yield return null;
            }

            ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
        }

        #endregion // Hungry

        #region Eat

        static private void OnActorEatStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev)
        {
            ActorInstance.ResetAnimationTransform(inActor);
        }

        static private IEnumerator ActorEatAnimation(ActorInstance inActor, ActorWorld inWorld)
        {
            ActorInstance target = inActor.CurrentInteractionActor;
            BFEat eatRule = BestiaryUtils.FindEatingRule(inActor.Definition.Id, target.Definition.Id, inActor.CurrentState);
            // TODO: make this clickable
            yield return 1;
            if (target.Definition.FreeOnEaten)
                ActorWorld.Free(inWorld, target);
            ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
        }

        #endregion // Eat

        #region Being Eaten

        static private void OnActorBeingEatenStart(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev)
        {
            ActorInstance.ReleaseTargetsAndInteractions(inActor, inWorld);
            ActorInstance.ResetAnimationTransform(inActor);
        }

        static private IEnumerator ActorBeingEatenAnimation(ActorInstance inActor, ActorWorld inWorld)
        {
            yield return inActor.CachedTransform.MoveTo(inActor.CachedTransform.localPosition.x + 0.1f, 0.1f, Axis.X, Space.Self)
                .Wave(Wave.Function.Sin, 1).Loop().RevertOnCancel();
        }

        static private void OnActorBeingEatenEnd(ActorInstance inActor, ActorWorld inWorld, ActorActionId inNext)
        {
            ActorInstance.ResetAnimationTransform(inActor);
        }

        #endregion // Being Eaten

        static private void OnInteractionAcquired(ActorInstance inActor, ActorWorld inWorld)
        {
            ActorInstance.SetActorAction(inActor, ActorActionId.BeingEaten, inWorld);
        }

        static private void OnInteractionReleased(ActorInstance inActor, ActorWorld inWorld)
        {
            if (inActor.IncomingInteractionCount > 0 || inActor.CurrentAction != ActorActionId.BeingEaten)
                return;

            ActorInstance.SetActorAction(inActor, ActorActionId.Idle, inWorld);
        }

        #endregion // Actor States

        static private RingBuffer<PriorityValue<ActorInstance>> s_EatTargetBuffer;

        static private ActorInstance FindGoodEatTarget(ActorInstance inInstance, ActorWorld inWorld)
        {
            RingBuffer<PriorityValue<ActorInstance>> eatBuffer = s_EatTargetBuffer ?? (s_EatTargetBuffer = new RingBuffer<PriorityValue<ActorInstance>>(16, RingBufferMode.Expand));
            ActorDefinition.ValidEatTarget[] validTargets = ActorDefinition.GetEatTargets(inInstance.Definition, inInstance.CurrentState);
            eatBuffer.Clear();
            Vector3 instancePosition = inInstance.CachedTransform.localPosition;
            Vector3 critterPosition;
            float critterDistance, priority;

            foreach(var critter in inWorld.Actors)
            {
                if (!IsValidEatTarget(validTargets, critter))
                    continue;

                critterPosition = critter.CachedTransform.localPosition;
                critterDistance = Vector3.Distance(instancePosition, critterPosition);
                priority = (critter.Definition.TargetLimit - critter.IncomingTargetCount) * (5 - critterDistance);
                eatBuffer.PushBack(new PriorityValue<ActorInstance>(critter, priority));
            }

            if (eatBuffer.Count == 0)
            {
                return null;
            }
            else if (eatBuffer.Count == 1)
            {
                return eatBuffer.PopFront();
            }
            else
            {
                eatBuffer.Sort();
                ActorInstance actor = eatBuffer.PopFront().Value;
                eatBuffer.Clear();
                return actor;
            }
        }

        static private Vector3 FindGoodEatPositionOffset(ActorInstance inEatTarget)
        {
            return RNG.Instance.NextVector2(inEatTarget.Definition.EatOffsetRange);
        }

        static private bool IsValidEatTarget(ActorDefinition.ValidEatTarget[] inTargets, ActorInstance inPossibleTarget)
        {
            if (!ActorInstance.IsValidTarget(inPossibleTarget))
                return false;

            for(int i = 0, length = inTargets.Length; i < length; i++)
            {
                if (inTargets[i].TargetId == inPossibleTarget.Definition.Id)
                    return true;
            }

            return false;
        }

        public void ClearAll()
        {
            m_World.Water = default(WaterPropertyBlockF32);
            if (m_World != null)
            {
                ActorWorld.FreeAll(m_World);
            }
            m_Phase = Phase.Spawning;
        }

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            m_Allocator = FindObjectOfType<ActorAllocator>();
            m_Tank = GetComponentInParent<SelectableTank>();
        }

        #endif // UNITY_EDITOR
    }
}