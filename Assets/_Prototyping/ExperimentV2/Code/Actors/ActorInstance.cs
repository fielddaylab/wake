using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using BeauPools;
using Aqua.Animation;
using Aqua;
using System.Collections;
using BeauUtil.Debugger;
using System.Runtime.CompilerServices;

namespace ProtoAqua.ExperimentV2
{
    public sealed class ActorInstance : MonoBehaviour, IPooledObject<ActorInstance>
    {
        public const float DropSpawnAnimationDistance = 4;
        public delegate void GeneralDelegate(ActorInstance inActor, ActorWorld inWorld);
        public delegate void StateEnterExitDelegate(ActorInstance inActor, ActorWorld inWorld);
        public delegate void ActionEnterDelegate(ActorInstance inActor, ActorWorld inWorld, ActorActionId inPrev);
        public delegate IEnumerator ActionCoroutineDelegate(ActorInstance inActor, ActorWorld inWorld);
        public delegate void ActionExitDelegate(ActorInstance inActor, ActorWorld inWorld, ActorActionId inNext);

        #region Inspector

        [Required(ComponentLookupDirection.Self)] public Transform CachedTransform;
        [Required(ComponentLookupDirection.Self)] public Collider2D CachedCollider;
        [Required(ComponentLookupDirection.Self)] public ColorGroup ColorAdjust;
        public AmbientTransform IdleAnimation;
        public Transform AnimationTransform;

        #endregion // Inspector
        
        [NonSerialized] public ActorDefinition Definition;

        [NonSerialized] public ActorActionId CurrentAction;
        [NonSerialized] public ActorStateId CurrentState;

        [NonSerialized] public ActorInstance CurrentTargetActor;
        [NonSerialized] public int IncomingTargetCount;
        
        [NonSerialized] public ActorInstance CurrentInteractionActor;
        [NonSerialized] public int IncomingInteractionCount;

        [NonSerialized] public Routine ActionAnimation;
        
        [NonSerialized] public TempAlloc<VFX> StateEffect;
        [NonSerialized] public Routine StateAnimation;
        
        [NonSerialized] public bool InWater;

        [NonSerialized] private bool m_IsInstantiated;
        [NonSerialized] private ushort m_ActionVersion;
        [NonSerialized] private TransformState m_OriginalAnimTransformState;

        #region IPoolAllocHandler

        void IPooledObject<ActorInstance>.OnConstruct(IPool<ActorInstance> inPool) { }
        void IPooledObject<ActorInstance>.OnDestruct() { }
        void IPooledObject<ActorInstance>.OnAlloc()
        {
            Assert.False(m_IsInstantiated);
            
            if (AnimationTransform)
                m_OriginalAnimTransformState = TransformState.LocalState(AnimationTransform);

            m_IsInstantiated = true;
            IncomingTargetCount = 0;
            IncomingInteractionCount = 0;
            m_ActionVersion = 0;
        }

        void IPooledObject<ActorInstance>.OnFree()
        {
            Assert.True(m_IsInstantiated);
            m_IsInstantiated = false;
            CurrentState = ActorStateId.Alive;
            CurrentAction = ActorActionId.Waiting;
            ReleaseTargetsAndInteractions(this, null);
            ResetAnimationTransform(this);
            IncomingTargetCount = 0;
            IncomingInteractionCount = 0;
            m_ActionVersion = 0;
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

        #region States
        
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

        static private void OnBeginStressedState(ActorInstance inInstance, ActorWorld inWorld)
        {
            //Xander Grabowski - 02/04/2022
            if (inInstance.ColorAdjust)
            {
                inInstance.ColorAdjust.SetColor(Color.white);
            }
            //inInstance.StateAnimation.Replace(inInstance, Tween.Color(Color.white, Color.red, inInstance.ColorAdjust.SetColor, 0.5f).Wave(Wave.Function.Sin, 1).Loop());
            inInstance.StateAnimation.Replace(inInstance, EmitEmojiLoop(inInstance, inWorld, "Stress", 0.7f));
        }

        static private void OnEndStressedState(ActorInstance inInstance, ActorWorld inWorld)
        {
            //Xander Grabowski - 02/04/2022
            if (inInstance.ColorAdjust)
                inInstance.ColorAdjust.SetColor(Color.white);
        }

        static private void OnBeginDeadState(ActorInstance inInstance, ActorWorld inWorld)
        {
            if (inInstance.ColorAdjust)
                inInstance.ColorAdjust.SetColor(Color.gray);
            
            inInstance.StateAnimation.Replace(inInstance, EmitEmojiLoop(inInstance, inWorld, "Dead", 1));
        }

        static private void OnEndDeadState(ActorInstance inInstance, ActorWorld inWorld)
        {
            if (inInstance.ColorAdjust)
                inInstance.ColorAdjust.SetColor(Color.white);
        }

        static private readonly StateEnterExitDelegate[] OnEnterStateDelegates = { null, OnBeginStressedState, OnBeginDeadState };
        static private readonly StateEnterExitDelegate[] OnExitStateDelegates = { null, OnEndStressedState, OnEndDeadState };

        #endregion // States

        #region Actions

        static public bool SetActorAction(ActorInstance ioInstance, ActorActionId inActionId, ActorWorld inWorld)
        {
            ActorActionId prev = ioInstance.CurrentAction;

            if (inWorld.Tank.IsActionAvailable != null && !inWorld.Tank.IsActionAvailable(inActionId))
                return false;

            if (!Ref.Replace(ref ioInstance.CurrentAction, inActionId))   
                return false;

            ushort lastVersion = IncrementVersion(ref ioInstance.m_ActionVersion);
            s_ActionExitMethods[(int) prev]?.Invoke(ioInstance, inWorld, inActionId);
            if (ioInstance.m_ActionVersion != lastVersion)
                return true;
            
            ioInstance.ActionAnimation.Stop();
            s_ActionEnterMethods[(int) inActionId]?.Invoke(ioInstance, inWorld, prev);

            if (ioInstance.m_ActionVersion != lastVersion)
                return true;

            if (!ioInstance.ActionAnimation)
            {
                IEnumerator coroutine = s_ActionCoroutineMethods[(int) inActionId]?.Invoke(ioInstance, inWorld);
                if (coroutine != null)
                    ioInstance.ActionAnimation = Routine.Start(ioInstance, coroutine);
            }

            return true;
        }

        static public void ForceActorAction(ActorInstance ioInstance, ActorActionId inActionId, ActorWorld inWorld)
        {
            ActorActionId prev = ioInstance.CurrentAction;
            ioInstance.CurrentAction = inActionId;
            ushort lastVersion = IncrementVersion(ref ioInstance.m_ActionVersion);
            s_ActionExitMethods[(int) prev]?.Invoke(ioInstance, inWorld, inActionId);
            if (ioInstance.m_ActionVersion != lastVersion)
                return;
            
            ioInstance.ActionAnimation.Stop();
            s_ActionEnterMethods[(int) inActionId]?.Invoke(ioInstance, inWorld, prev);
            if (ioInstance.m_ActionVersion != lastVersion)
                return;
            
            if (!ioInstance.ActionAnimation)
            {
                IEnumerator coroutine = s_ActionCoroutineMethods[(int) inActionId]?.Invoke(ioInstance, inWorld);
                if (coroutine != null)
                    ioInstance.ActionAnimation = Routine.Start(ioInstance, coroutine);
            }
        }

        static public void ConfigureActionMethods(ActorActionId inActionId, ActionEnterDelegate inOnEnter, ActionExitDelegate inOnExit, ActionCoroutineDelegate inCoroutine)
        {
            s_ActionEnterMethods[(int) inActionId] = inOnEnter;
            s_ActionExitMethods[(int) inActionId] = inOnExit;
            s_ActionCoroutineMethods[(int) inActionId] = inCoroutine;
        }

        [MethodImpl(256)]
        static private ushort IncrementVersion(ref ushort ioVersion)
        {
            return ++ioVersion;
        }

        static private readonly ActionEnterDelegate[] s_ActionEnterMethods = new ActionEnterDelegate[(int) ActorActionId.COUNT];
        static private readonly ActionExitDelegate[] s_ActionExitMethods = new ActionExitDelegate[(int) ActorActionId.COUNT];
        static private readonly ActionCoroutineDelegate[] s_ActionCoroutineMethods = new ActionCoroutineDelegate[(int) ActorActionId.COUNT];

        #endregion // Actions

        #region Targets

        static public bool IsValidTarget(ActorInstance inActor)
        {
            return inActor.m_IsInstantiated && inActor.IncomingInteractionCount <= inActor.Definition.TargetLimit && inActor.CurrentState != ActorStateId.Dead;
        }

        static public bool AcquireTarget(ActorInstance ioCurrentActor, ActorInstance inTarget, ActorWorld inWorld)
        {
            return AcquireTarget(ref ioCurrentActor.CurrentTargetActor, inTarget, inWorld);
        }

        static private bool AcquireTarget(ref ActorInstance ioCurrentTarget, ActorInstance inTarget, ActorWorld inWorld)
        {
            if (ioCurrentTarget == inTarget)
                return true;

            if (inTarget == null)
            {
                ReleaseTarget(ref ioCurrentTarget, inWorld);
                return true;
            }
            
            if (!inTarget.m_IsInstantiated || inTarget.CurrentState == ActorStateId.Dead || inTarget.IncomingTargetCount >= inTarget.Definition.TargetLimit)
                return false;

            ReleaseTarget(ref ioCurrentTarget, inWorld);

            ioCurrentTarget = inTarget;
            inTarget.IncomingTargetCount++;
            s_OnTargetAcquiredMethod?.Invoke(inTarget, inWorld);
            return true;
        }

        static public void ReleaseTarget(ActorInstance ioCurrentActor, ActorWorld inWorld)
        {
            ReleaseTarget(ref ioCurrentActor.CurrentTargetActor, inWorld);
        }

        static private void ReleaseTarget(ref ActorInstance ioCurrentTarget, ActorWorld inWorld)
        {
            if (ioCurrentTarget != null)
            {
                if (ioCurrentTarget.m_IsInstantiated)
                {
                    ioCurrentTarget.IncomingTargetCount--;
                    s_OnTargetReleasedMethod?.Invoke(ioCurrentTarget, inWorld);
                }
                ioCurrentTarget = null;
            }
        }

        static public void ConfigureTargetMethods(GeneralDelegate inOnAcquired, GeneralDelegate inOnRelease)
        {
            s_OnTargetAcquiredMethod = inOnAcquired;
            s_OnTargetReleasedMethod = inOnRelease;
        }

        static private GeneralDelegate s_OnTargetAcquiredMethod;
        static private GeneralDelegate s_OnTargetReleasedMethod;

        #endregion // Targets

        #region Interactors

        static public bool AcquireInteraction(ActorInstance ioCurrentActor, ActorInstance inInteraction, ActorWorld inWorld)
        {
            return AcquireInteraction(ref ioCurrentActor.CurrentInteractionActor, inInteraction, inWorld);
        }

        static private bool AcquireInteraction(ref ActorInstance ioCurrentInteraction, ActorInstance inInteraction, ActorWorld inWorld)
        {
            if (ioCurrentInteraction == inInteraction)
                return true;

            if (inInteraction == null)
            {
                ReleaseInteraction(ref ioCurrentInteraction, inWorld);
                return true;
            }
            
            if (!inInteraction.m_IsInstantiated || inInteraction.CurrentState == ActorStateId.Dead || inInteraction.IncomingInteractionCount >= inInteraction.Definition.TargetLimit)
                return false;

            ReleaseInteraction(ref ioCurrentInteraction, inWorld);

            ioCurrentInteraction = inInteraction;
            inInteraction.IncomingInteractionCount++;
            s_OnInteractionAcquiredMethod?.Invoke(inInteraction, inWorld);
            return true;
        }

        static public void ReleaseInteraction(ActorInstance ioCurrentActor, ActorWorld inWorld)
        {
            ReleaseInteraction(ref ioCurrentActor.CurrentInteractionActor, inWorld);
        }

        static private void ReleaseInteraction(ref ActorInstance ioCurrentInteraction, ActorWorld inWorld)
        {
            if (ioCurrentInteraction != null)
            {
                if (ioCurrentInteraction.m_IsInstantiated)
                {
                    ioCurrentInteraction.IncomingInteractionCount--;
                    s_OnInteractionReleasedMethod?.Invoke(ioCurrentInteraction, inWorld);
                }
                ioCurrentInteraction = null;
            }
        }

        static public void ConfigureInteractionMethods(GeneralDelegate inOnAcquired, GeneralDelegate inOnRelease)
        {
            s_OnInteractionAcquiredMethod = inOnAcquired;
            s_OnInteractionReleasedMethod = inOnRelease;
        }

        static private GeneralDelegate s_OnInteractionAcquiredMethod;
        static private GeneralDelegate s_OnInteractionReleasedMethod;

        #endregion // Interactors

        static public void ReleaseTargetsAndInteractions(ActorInstance ioActor, ActorWorld inWorld)
        {
            ReleaseTarget(ref ioActor.CurrentTargetActor, null);
            ReleaseInteraction(ref ioActor.CurrentInteractionActor, null);
        }
    
        #region Animations

        static public void ResetAnimationTransform(ActorInstance inInstance)
        {
            Transform animTransform = inInstance.AnimationTransform;
            if (animTransform)
                inInstance.m_OriginalAnimTransformState.Apply(animTransform);
        }

        #region Spawning

        static public void StartSpawning(ActorInstance inInstance, ActorWorld inWorld, ActorActionId inPrev)
        {
            ActorDefinition def = inInstance.Definition;
            Vector3 targetPos = ActorDefinition.FindRandomSpawnLocation(RNG.Instance, inWorld.WorldBounds, def.Spawning);

            if (inInstance.IdleAnimation)
                inInstance.IdleAnimation.AnimationScale = 0;

            if (def.Spawning.SpawnAnimation == ActorDefinition.SpawnAnimationId.Sprout)
            {
                inInstance.CachedTransform.SetPosition(targetPos, Axis.XYZ, Space.Self);
                inInstance.CachedTransform.SetScale(0);
                inInstance.ActionAnimation.Replace(inInstance, SproutFromBottom(inInstance, inWorld)).Tick();
            }
            else
            {
                Vector3 offsetPos = targetPos;
                float surfaceDistY = inWorld.WorldBounds.max.y - targetPos.y;
                offsetPos.y += DropSpawnAnimationDistance + surfaceDistY + def.Spawning.AvoidTankTopBottomRadius;
                inInstance.CachedTransform.SetPosition(offsetPos, Axis.XYZ, Space.Self);
                inInstance.ActionAnimation.Replace(inInstance, FallToPosition(inInstance, targetPos, inWorld)).Tick();
            }
        }

        static private IEnumerator SproutFromBottom(ActorInstance inInstance, ActorWorld inWorld)
        {
            inInstance.CachedCollider.enabled = false;
            yield return RNG.Instance.NextFloat(0, 0.3f);
            inInstance.CachedCollider.enabled = true;

            yield return inInstance.CachedTransform.ScaleTo(1, 0.3f).Ease(Curve.CubeOut);
            if (inInstance.IdleAnimation) {
                yield return Tween.ZeroToOne((f) => inInstance.IdleAnimation.AnimationScale = f, 0.2f);
            }
            SetActorAction(inInstance, ActorActionId.Waiting, inWorld);
        }

        static private IEnumerator FallToPosition(ActorInstance inInstance, Vector3 inTargetPosition, ActorWorld inWorld)
        {
            yield return RNG.Instance.NextFloat(0, 0.3f);
            yield return inInstance.CachedTransform.MoveTo(inTargetPosition, 0.3f, Axis.XYZ, Space.Self).Ease(Curve.CubeOut);
            if (inInstance.IdleAnimation) {
                yield return Tween.ZeroToOne((f) => inInstance.IdleAnimation.AnimationScale = f, 0.2f);
            }
            SetActorAction(inInstance, ActorActionId.Waiting, inWorld);
        }

        static private void OnEnterWaiting(ActorInstance inInstance, ActorWorld inWorld, ActorActionId inPrev)
        {
            if (inInstance.IdleAnimation)
                inInstance.IdleAnimation.AnimationScale = 1;
        }

        #endregion // Spawning

        //Xander Grabowski - 02/04/2022
        static public IEnumerator EmitEmojiLoop(ActorInstance inActor, ActorWorld inWorld, StringHash32 inId, float inInterval){
            while(inActor.CurrentAction == ActorActionId.Spawning) {
                yield return null;
            }

            yield return RNG.Instance.NextFloat(0.1f, 0.3f);
            while(true) {
                ActorWorld.EmitEmoji(inWorld, inActor, inId);
                yield return RNG.Instance.NextFloat(0.8f, 1.2f) * inInterval;
            }
        }

        static public IEnumerator ShakeLoop(ActorInstance inActor, ActorWorld inWorld) {
            while(inActor.CurrentAction == ActorActionId.Spawning) {
                yield return null;
            }

            yield return inActor.AnimationTransform.MoveTo(inActor.AnimationTransform.localPosition.x + 0.4f, 0.3f, Axis.X, Space.Self)
                .Randomize().Loop().Wave(Wave.Function.Sin, 1).RevertOnCancel(false);
        }

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

        static ActorInstance()
        {
            ConfigureActionMethods(ActorActionId.Spawning, ActorInstance.StartSpawning, null, null);
            ConfigureActionMethods(ActorActionId.Waiting, ActorInstance.OnEnterWaiting, null, null);
        }
    }

    public enum ActorActionId : byte
    {
        Spawning, // playing spawning animation
        Waiting, // waiting for experiment to start
        Idle, // standing still
        Hungry, // moving towards food
        Eating, // eating food
        BeingEaten, // being eaten
        Dying, // dying
        Reproducing, // reproducing
        
        [Hidden]
        COUNT,
    }
}