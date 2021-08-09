using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using BeauPools;
using Aqua.Animation;
using Aqua;
using System.Collections;

namespace ProtoAqua.ExperimentV2
{
    public sealed class ActorInstance : MonoBehaviour, IPooledObject<ActorInstance>
    {
        public const float DropSpawnAnimationDistance = 8;
        public delegate void ActionEnterExitDelegate(ActorInstance inActor, ActorWorld inSystem);

        #region Inspector

        public Transform CachedTransform;
        public Collider2D CachedCollider;
        public AmbientTransform IdleAnimation;
        public ColorGroup ColorAdjust;

        #endregion // Inspector
        
        [NonSerialized] public ActorDefinition Definition;

        [NonSerialized] public ActorActionId CurrentAction;
        [NonSerialized] public ActorStateId CurrentState;
        [NonSerialized] public Routine ActionAnimation;
        [NonSerialized] public TempAlloc<Transform> StateEffect;
        [NonSerialized] public Routine StateAnimation;
        [NonSerialized] public bool InWater;

        #region IPoolAllocHandler

        void IPooledObject<ActorInstance>.OnConstruct(IPool<ActorInstance> inPool) { }
        void IPooledObject<ActorInstance>.OnDestruct() { }
        void IPooledObject<ActorInstance>.OnAlloc() { }
        void IPooledObject<ActorInstance>.OnFree()
        {
            CurrentState = ActorStateId.Alive;
            CurrentAction = ActorActionId.Waiting;
            StateAnimation.Stop();
            ActionAnimation.Stop();
            CachedCollider.enabled = true;
            Ref.Dispose(ref StateEffect);
            InWater = false;
            if (ColorAdjust)
                ColorAdjust.SetColor(Color.white);
            if (IdleAnimation)
                IdleAnimation.AnimationScale = 1;
        }

        #endregion // IPoolAllocHandler

        #region Methods

        static public bool SetActorState(ActorInstance ioInstance, ActorStateId inStateId, ActorWorld inWorld)
        {
            ActorStateId oldState = ioInstance.CurrentState;

            if (!Ref.Replace(ref ioInstance.CurrentState, inStateId))   
                return false;

            OnExitStateDelegates[(int) oldState]?.Invoke(ioInstance, inWorld);

            Ref.Dispose(ref ioInstance.StateEffect);
            ioInstance.StateAnimation.Stop();

            OnEnterStateDelegates[(int) inStateId]?.Invoke(ioInstance, inWorld);
            return true;
        }

        static public bool SetActorAction(ActorInstance ioInstance, ActorActionId inActionId)
        {
            if (!Ref.Replace(ref ioInstance.CurrentAction, inActionId))   
                return false;

            ioInstance.ActionAnimation.Stop();
            return true;
        }

        static public bool SetActorAction(ActorInstance ioInstance, ActorActionId inActionId, ActorWorld inWorld, ActionEnterExitDelegate inOnSet)
        {
            if (!Ref.Replace(ref ioInstance.CurrentAction, inActionId))   
                return false;

            ioInstance.ActionAnimation.Stop();
            inOnSet?.Invoke(ioInstance, inWorld);
            return true;
        }

        static public void ForceActorAction(ActorInstance ioInstance, ActorActionId inActionId, ActorWorld inWorld, ActionEnterExitDelegate inOnSet)
        {
            ioInstance.CurrentAction = inActionId;
            ioInstance.ActionAnimation.Stop();
            inOnSet?.Invoke(ioInstance, inWorld);
        }

        #endregion // Methods
    
        #region Animations

        #region Spawning

        static public void StartSpawning(ActorInstance inInstance, ActorWorld inWorld)
        {
            ActorDefinition def = inInstance.Definition;
            Vector3 targetPos = ActorDefinition.FindRandomSpawnLocation(RNG.Instance, inWorld.WorldBounds, def.Spawning);

            if (inInstance.IdleAnimation)
                inInstance.IdleAnimation.AnimationScale = 0;

            if (def.Spawning.SpawnAnimation == ActorDefinition.SpawnAnimationId.Sprout)
            {
                inInstance.CachedTransform.SetPosition(targetPos, Axis.XYZ, Space.Self);
                inInstance.CachedTransform.SetScale(0);
                inInstance.ActionAnimation.Replace(inInstance, SproutFromBottom(inInstance, inWorld)).TryManuallyUpdate(0);
            }
            else
            {
                Vector3 offsetPos = targetPos;
                offsetPos.y += DropSpawnAnimationDistance;
                inInstance.CachedTransform.SetPosition(offsetPos, Axis.XYZ, Space.Self);
                inInstance.ActionAnimation.Replace(inInstance, FallToPosition(inInstance, targetPos, inWorld)).TryManuallyUpdate(0);
            }
        }

        static private IEnumerator SproutFromBottom(ActorInstance inInstance, ActorWorld inWorld)
        {
            inInstance.CachedCollider.enabled = false;
            yield return RNG.Instance.NextFloat(0, 0.2f);
            inInstance.CachedCollider.enabled = true;

            yield return inInstance.CachedTransform.ScaleTo(1, 0.2f).Ease(Curve.CubeOut);
            SetActorAction(inInstance, ActorActionId.Waiting, inWorld, OnEnterWaiting);
        }

        static private IEnumerator FallToPosition(ActorInstance inInstance, Vector3 inTargetPosition, ActorWorld inWorld)
        {
            yield return RNG.Instance.NextFloat(0, 0.2f);
            yield return inInstance.CachedTransform.MoveTo(inTargetPosition, 0.2f, Axis.XYZ, Space.Self).Ease(Curve.CubeOut);
            SetActorAction(inInstance, ActorActionId.Waiting, inWorld, OnEnterWaiting);
        }

        static private void OnEnterWaiting(ActorInstance inInstance, ActorWorld inWorld)
        {
            if (inInstance.IdleAnimation)
                inInstance.IdleAnimation.AnimationScale = 1;
        }

        #endregion // Spawning

        #region States

        static private void OnBeginStressedState(ActorInstance inInstance, ActorWorld inWorld)
        {
            if (inInstance.ColorAdjust)
            {
                inInstance.ColorAdjust.SetColor(Color.white);
                inInstance.StateAnimation.Replace(inInstance, Tween.Color(Color.white, Color.red, inInstance.ColorAdjust.SetColor, 0.5f).Wave(Wave.Function.Sin, 1).Loop());
            }
        }

        static private void OnEndStressedState(ActorInstance inInstance, ActorWorld inWorld)
        {
            if (inInstance.ColorAdjust)
                inInstance.ColorAdjust.SetColor(Color.white);
        }

        static private void OnBeginDeadState(ActorInstance inInstance, ActorWorld inWorld)
        {
            if (inInstance.ColorAdjust)
                inInstance.ColorAdjust.SetColor(Color.gray);
        }

        static private void OnEndDeadState(ActorInstance inInstance, ActorWorld inWorld)
        {
            if (inInstance.ColorAdjust)
                inInstance.ColorAdjust.SetColor(Color.white);
        }

        static private readonly ActionEnterExitDelegate[] OnEnterStateDelegates = { null, OnBeginStressedState, OnBeginDeadState };
        static private readonly ActionEnterExitDelegate[] OnExitStateDelegates = { null, OnEndStressedState, OnEndDeadState };

        #endregion // States

        #endregion // Animations

        #if UNITY_EDITOR

        private void Reset()
        {
            this.CacheComponent(ref CachedTransform);
            this.CacheComponent(ref IdleAnimation);
            this.CacheComponent(ref ColorAdjust);
            this.CacheComponent(ref CachedCollider);
        }

        #endif // UNITY_EDITOR
    }

    public enum ActorActionId : byte
    {
        Spawning, // playing spawning animation
        Waiting, // waiting for experiment to start
        Idle, // standing still
        IdleMove, // idle moving
        Hungry, // moving towards food
        Eating, // eating food
        Dying // dying
    }
}