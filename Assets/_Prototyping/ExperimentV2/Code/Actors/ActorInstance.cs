using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Aqua;
using Aqua.Animation;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.ExperimentV2 {
    public sealed class ActorInstance : MonoBehaviour, IPooledObject<ActorInstance> {
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
        public Collider2D EatCollider;

        [Header("Optional")]
        public AmbientTransform IdleAnimation;
        public Transform AnimationTransform;
        public ActorMouth Mouth;
        public ParticleSystem DistributedParticles;
        public GameObject ParasiteMarker;

        #endregion // Inspector

        [NonSerialized] public ActorDefinition Definition;
        [NonSerialized] public Bounds TankBounds;

        [NonSerialized] public ActorActionId CurrentAction;
        [NonSerialized] public ActorStateId CurrentState;

        [NonSerialized] public ActorHandle CurrentTargetActor;
        [NonSerialized] public int IncomingTargetCount;

        [NonSerialized] public ActorHandle CurrentInteractionActor;
        [NonSerialized] public int IncomingInteractionCount;

        [NonSerialized] public Routine ActionAnimation;

        [NonSerialized] public TempAlloc<VFX> StateEffect;
        [NonSerialized] public Routine StateAnimation;

        [NonSerialized] public Routine BreathAnimation;

        [NonSerialized] public bool InWater;

        [NonSerialized] private bool m_IsInstantiated;
        [NonSerialized] private ushort m_InstantiatedVersion;
        [NonSerialized] private ushort m_ActionVersion;
        [NonSerialized] private TransformState m_OriginalAnimTransformState;

        #region IPoolAllocHandler

        void IPooledObject<ActorInstance>.OnConstruct(IPool<ActorInstance> inPool) { }
        void IPooledObject<ActorInstance>.OnDestruct() { }
        void IPooledObject<ActorInstance>.OnAlloc() {
            Assert.False(m_IsInstantiated);

            if (AnimationTransform)
                m_OriginalAnimTransformState = TransformState.LocalState(AnimationTransform);
            if (ParasiteMarker)
                ParasiteMarker.SetActive(false);

            m_IsInstantiated = true;
            IncomingTargetCount = 0;
            IncomingInteractionCount = 0;
            m_ActionVersion = 0;
            IncrementVersion(ref m_InstantiatedVersion);
        }

        void IPooledObject<ActorInstance>.OnFree() {
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
            BreathAnimation.Stop();
            CachedCollider.enabled = true;
            Ref.Dispose(ref StateEffect);
            InWater = false;
            if (ColorAdjust)
                ColorAdjust.SetColor(Color.white);
            if (IdleAnimation)
                IdleAnimation.AnimationScale = 1;
            if (ParasiteMarker)
                ParasiteMarker.SetActive(false);
        }

        #endregion // IPoolAllocHandler

        public bool CheckHandle(ActorHandle handle) {
            return m_InstantiatedVersion == handle.Key;
        }

        public ActorHandle Handle() {
            return m_IsInstantiated ? new ActorHandle(this, m_InstantiatedVersion) : default;
        }

        #region States

        static public bool SetActorState(ActorInstance ioInstance, ActorStateId inStateId, ActorWorld inWorld) {
            ActorStateId oldState = ioInstance.CurrentState;

            if (!Ref.Replace(ref ioInstance.CurrentState, inStateId))
                return false;

            OnExitStateDelegates[(int)oldState]?.Invoke(ioInstance, inWorld);

            Ref.Dispose(ref ioInstance.StateEffect);
            ioInstance.StateAnimation.Stop();

            OnEnterStateDelegates[(int)inStateId]?.Invoke(ioInstance, inWorld);
            return true;
        }

        static private void OnBeginStressedState(ActorInstance inInstance, ActorWorld inWorld) {
            //Xander Grabowski - 02/04/2022
            if (inInstance.ColorAdjust) {
                inInstance.ColorAdjust.SetColor(Color.white);
            }
            //inInstance.StateAnimation.Replace(inInstance, Tween.Color(Color.white, Color.red, inInstance.ColorAdjust.SetColor, 0.5f).Wave(Wave.Function.Sin, 1).Loop());
            inInstance.StateAnimation.Replace(inInstance, EmitEmojiLoop(inInstance, inWorld, SelectableTank.Emoji_Stressed, 0.7f));
        }

        static private void OnEndStressedState(ActorInstance inInstance, ActorWorld inWorld) {
            //Xander Grabowski - 02/04/2022
            if (inInstance.ColorAdjust)
                inInstance.ColorAdjust.SetColor(Color.white);
        }

        static private void OnBeginDeadState(ActorInstance inInstance, ActorWorld inWorld) {
            if (inInstance.ColorAdjust)
                inInstance.ColorAdjust.SetColor(Color.gray);

            inInstance.StateAnimation.Replace(inInstance, EmitEmojiLoop(inInstance, inWorld, SelectableTank.Emoji_Death, 1));
        }

        static private void OnEndDeadState(ActorInstance inInstance, ActorWorld inWorld) {
            if (inInstance.ColorAdjust)
                inInstance.ColorAdjust.SetColor(Color.white);
        }

        static private readonly StateEnterExitDelegate[] OnEnterStateDelegates = { null, OnBeginStressedState, OnBeginDeadState };
        static private readonly StateEnterExitDelegate[] OnExitStateDelegates = { null, OnEndStressedState, OnEndDeadState };

        #endregion // States

        #region Actions

        static public bool SetActorAction(ActorInstance ioInstance, ActorActionId inActionId, ActorWorld inWorld) {
            ActorActionId prev = ioInstance.CurrentAction;

            if (!ActorWorld.IsActionAvailable(inActionId, inWorld))
                return false;

            if (!Ref.Replace(ref ioInstance.CurrentAction, inActionId))
                return false;

            ushort lastVersion = IncrementVersion(ref ioInstance.m_ActionVersion);
            s_ActionExitMethods[(int)prev]?.Invoke(ioInstance, inWorld, inActionId);
            if (ioInstance.m_ActionVersion != lastVersion)
                return true;

            ioInstance.ActionAnimation.Stop();
            s_ActionEnterMethods[(int)inActionId]?.Invoke(ioInstance, inWorld, prev);

            if (ioInstance.m_ActionVersion != lastVersion)
                return true;

            if (!ioInstance.ActionAnimation) {
                IEnumerator coroutine = s_ActionCoroutineMethods[(int)inActionId]?.Invoke(ioInstance, inWorld);
                if (coroutine != null)
                    ioInstance.ActionAnimation = Routine.Start(ioInstance, coroutine);
            }

            return true;
        }

        static public void ForceActorAction(ActorInstance ioInstance, ActorActionId inActionId, ActorWorld inWorld) {
            ActorActionId prev = ioInstance.CurrentAction;
            ioInstance.CurrentAction = inActionId;
            ushort lastVersion = IncrementVersion(ref ioInstance.m_ActionVersion);
            s_ActionExitMethods[(int)prev]?.Invoke(ioInstance, inWorld, inActionId);
            if (ioInstance.m_ActionVersion != lastVersion)
                return;

            ioInstance.ActionAnimation.Stop();
            s_ActionEnterMethods[(int)inActionId]?.Invoke(ioInstance, inWorld, prev);
            if (ioInstance.m_ActionVersion != lastVersion)
                return;

            if (!ioInstance.ActionAnimation) {
                IEnumerator coroutine = s_ActionCoroutineMethods[(int)inActionId]?.Invoke(ioInstance, inWorld);
                if (coroutine != null)
                    ioInstance.ActionAnimation = Routine.Start(ioInstance, coroutine);
            }
        }

        static public void ConfigureActionMethods(ActorActionId inActionId, ActionEnterDelegate inOnEnter, ActionExitDelegate inOnExit, ActionCoroutineDelegate inCoroutine) {
            s_ActionEnterMethods[(int)inActionId] = inOnEnter;
            s_ActionExitMethods[(int)inActionId] = inOnExit;
            s_ActionCoroutineMethods[(int)inActionId] = inCoroutine;
        }

        [MethodImpl(256)]
        static private ushort IncrementVersion(ref ushort ioVersion) {
            return ++ioVersion;
        }

        static private readonly ActionEnterDelegate[] s_ActionEnterMethods = new ActionEnterDelegate[(int)ActorActionId.COUNT];
        static private readonly ActionExitDelegate[] s_ActionExitMethods = new ActionExitDelegate[(int)ActorActionId.COUNT];
        static private readonly ActionCoroutineDelegate[] s_ActionCoroutineMethods = new ActionCoroutineDelegate[(int)ActorActionId.COUNT];

        #endregion // Actions

        #region Targets

        static public bool IsValidTarget(ActorInstance inActor) {
            return inActor.m_IsInstantiated && inActor.IncomingTargetCount <= inActor.Definition.TargetLimit && inActor.CurrentState != ActorStateId.Dead && inActor.CurrentAction != ActorActionId.BeingBorn;
        }

        static public bool IsValidTarget(ActorHandle inActorHandle) {
            return inActorHandle.Valid && IsValidTarget(inActorHandle.Get());
        }

        static public bool AcquireTarget(ActorInstance ioCurrentActor, ActorInstance inTarget, ActorWorld inWorld) {
            return AcquireTarget(ref ioCurrentActor.CurrentTargetActor, inTarget, inWorld);
        }

        static private bool AcquireTarget(ref ActorHandle ioCurrentTarget, ActorInstance inTarget, ActorWorld inWorld) {
            if (ioCurrentTarget == inTarget)
                return true;

            if (inTarget == null) {
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

        static public void ReleaseTarget(ActorInstance ioCurrentActor, ActorWorld inWorld) {
            ReleaseTarget(ref ioCurrentActor.CurrentTargetActor, inWorld);
        }

        static private void ReleaseTarget(ref ActorHandle ioCurrentTarget, ActorWorld inWorld) {
            if (ioCurrentTarget != default) {
                if (ioCurrentTarget) {
                    var target = ioCurrentTarget.Get();
                    target.IncomingTargetCount--;
                    s_OnTargetReleasedMethod?.Invoke(target, inWorld);
                }

                ioCurrentTarget = null;
            }
        }

        static public void ConfigureTargetMethods(GeneralDelegate inOnAcquired, GeneralDelegate inOnRelease) {
            s_OnTargetAcquiredMethod = inOnAcquired;
            s_OnTargetReleasedMethod = inOnRelease;
        }

        static private GeneralDelegate s_OnTargetAcquiredMethod;
        static private GeneralDelegate s_OnTargetReleasedMethod;

        #endregion // Targets

        #region Interactors

        static public bool AcquireInteraction(ActorInstance ioCurrentActor, ActorInstance inInteraction, ActorWorld inWorld) {
            return AcquireInteraction(ref ioCurrentActor.CurrentInteractionActor, inInteraction, inWorld);
        }

        static private bool AcquireInteraction(ref ActorHandle ioCurrentInteraction, ActorInstance inInteraction, ActorWorld inWorld) {
            if (ioCurrentInteraction == inInteraction)
                return true;

            if (inInteraction == null) {
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

        static public void ReleaseInteraction(ActorInstance ioCurrentActor, ActorWorld inWorld) {
            ReleaseInteraction(ref ioCurrentActor.CurrentInteractionActor, inWorld);
        }

        static private void ReleaseInteraction(ref ActorHandle ioCurrentInteraction, ActorWorld inWorld) {
            if (ioCurrentInteraction != default) {
                if (ioCurrentInteraction) {
                    var interact = ioCurrentInteraction.Get();
                    interact.IncomingInteractionCount--;
                    s_OnInteractionReleasedMethod?.Invoke(interact, inWorld);
                }

                ioCurrentInteraction = null;
            }
        }

        static public void ConfigureInteractionMethods(GeneralDelegate inOnAcquired, GeneralDelegate inOnRelease) {
            s_OnInteractionAcquiredMethod = inOnAcquired;
            s_OnInteractionReleasedMethod = inOnRelease;
        }

        static private GeneralDelegate s_OnInteractionAcquiredMethod;
        static private GeneralDelegate s_OnInteractionReleasedMethod;

        #endregion // Interactors

        static public void ReleaseTargetsAndInteractions(ActorInstance ioActor, ActorWorld inWorld) {
            ReleaseTarget(ref ioActor.CurrentTargetActor, inWorld);
            ReleaseInteraction(ref ioActor.CurrentInteractionActor, inWorld);
        }

        #region Animations

        static public void ResetAnimationTransform(ActorInstance inInstance) {
            Transform animTransform = inInstance.AnimationTransform;
            if (animTransform)
                inInstance.m_OriginalAnimTransformState.Apply(animTransform);
        }

        #region Spawning

        static public void StartBreathing(ActorInstance inInstance, ActorWorld inWorld) {
            if (inInstance.Definition.IsLivingOrganism && !inInstance.BreathAnimation) {
                inInstance.BreathAnimation.Replace(inInstance, EmitEmojiLoop(inInstance, inWorld, SelectableTank.Emoji_Breath, 3f));
            }
        }

        static public void StartSpawning(ActorInstance inInstance, ActorWorld inWorld, ActorActionId inPrev) {
            ActorDefinition def = inInstance.Definition;
            Vector3 targetPos = ActorDefinition.FindRandomSpawnLocation(RNG.Instance, inInstance.TankBounds, def.Spawning);

            if (inInstance.IdleAnimation)
                inInstance.IdleAnimation.AnimationScale = 0;

            switch(def.Spawning.SpawnAnimation) {
                case ActorDefinition.SpawnAnimationId.Sprout: {
                    inInstance.CachedTransform.SetPosition(targetPos, Axis.XYZ, Space.Self);
                    inInstance.CachedTransform.SetScale(0);
                    inInstance.ActionAnimation.Replace(inInstance, SproutFromBottom(inInstance, inWorld)).Tick();
                    break;
                }

                case ActorDefinition.SpawnAnimationId.Drop: {
                    Vector3 offsetPos = targetPos;
                    float surfaceDistY = inWorld.WorldBounds.max.y - targetPos.y;
                    offsetPos.y += DropSpawnAnimationDistance + surfaceDistY + def.Spawning.AvoidTankTopBottomRadius;
                    inInstance.CachedTransform.SetPosition(offsetPos, Axis.XYZ, Space.Self);
                    inInstance.ActionAnimation.Replace(inInstance, FallToPosition(inInstance, targetPos, inWorld)).Tick();
                    break;
                }

                case ActorDefinition.SpawnAnimationId.Expand: {
                    inInstance.CachedTransform.SetPosition(targetPos, Axis.XYZ, Space.Self);
                    inInstance.ActionAnimation.Replace(inInstance, ExpandIntoPosition(inInstance, inWorld)).Tick();
                    break;
                }
            }
        }

        static private IEnumerator SproutFromBottom(ActorInstance inInstance, ActorWorld inWorld) {
            inInstance.CachedCollider.enabled = false;
            yield return RNG.Instance.NextFloat(0, 0.3f);
            inInstance.CachedCollider.enabled = true;

            yield return inInstance.CachedTransform.ScaleTo(1, 0.3f).Ease(Curve.CubeOut);
            if (inInstance.IdleAnimation) {
                yield return Tween.ZeroToOne((f) => inInstance.IdleAnimation.AnimationScale = f, 0.2f);
            }
            SetActorAction(inInstance, ActorActionId.Waiting, inWorld);
        }

        static private IEnumerator FallToPosition(ActorInstance inInstance, Vector3 inTargetPosition, ActorWorld inWorld) {
            yield return RNG.Instance.NextFloat(0, 0.3f);
            yield return inInstance.CachedTransform.MoveTo(inTargetPosition, 0.3f, Axis.XYZ, Space.Self).Ease(Curve.CubeOut);
            if (inInstance.IdleAnimation) {
                yield return Tween.ZeroToOne((f) => inInstance.IdleAnimation.AnimationScale = f, 0.2f);
            }
            SetActorAction(inInstance, ActorActionId.Waiting, inWorld);
        }

        static private IEnumerator ExpandIntoPosition(ActorInstance inInstance, ActorWorld inWorld) {
            inInstance.DistributedParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            inInstance.CachedCollider.enabled = false;
            yield return RNG.Instance.NextFloat(0, 0.3f);
            inInstance.CachedCollider.enabled = true;
            inInstance.DistributedParticles.Play();
            SetActorAction(inInstance, ActorActionId.Waiting, inWorld);
        }

        static private void OnEnterWaiting(ActorInstance inInstance, ActorWorld inWorld, ActorActionId inPrev) {
            if (inInstance.IdleAnimation)
                inInstance.IdleAnimation.AnimationScale = 1;

            StartBreathing(inInstance, inWorld);

            if (inWorld.IsExecuting) {
                SetActorAction(inInstance, ActorActionId.Idle, inWorld);
            }
        }

        #endregion // Spawning

        #region Being Born

        static public void StartBeingBorn(ActorInstance inInstance, ActorWorld inWorld, ActorActionId inPrev) {
            ActorDefinition def = inInstance.Definition;
            Vector3 targetPos = ActorDefinition.FindRandomSpawnLocation(RNG.Instance, inInstance.TankBounds, def.Spawning);

            if (inInstance.IdleAnimation)
                inInstance.IdleAnimation.AnimationScale = 0;

            inInstance.CachedTransform.SetPosition(targetPos, Axis.XYZ, Space.Self);
            inInstance.CachedTransform.SetScale(0);
            inInstance.ActionAnimation.Replace(inInstance, BeingBornAnimation(inInstance, inWorld)).Tick();
        }

        static private IEnumerator BeingBornAnimation(ActorInstance inInstance, ActorWorld inWorld) {
            inInstance.CachedCollider.enabled = false;
            yield return RNG.Instance.NextFloat(0.1f, 0.5f);
            inInstance.CachedCollider.enabled = true;

            ActorWorld.EmitEmoji(inWorld, inInstance, SelectableTank.Emoji_Reproduce, null, 1);
            yield return inInstance.CachedTransform.ScaleTo(1, 0.3f).Ease(Curve.CubeOut);
            if (inInstance.IdleAnimation) {
                yield return Tween.ZeroToOne((f) => inInstance.IdleAnimation.AnimationScale = f, 0.2f);
            }
            SetActorAction(inInstance, ActorActionId.Waiting, inWorld);
        }

        #endregion // Being Born

        //Xander Grabowski - 02/04/2022
        static public IEnumerator EmitEmojiLoop(ActorInstance inActor, ActorWorld inWorld, StringHash32 inId, float inInterval) {
            while (inActor.CurrentAction == ActorActionId.Spawning || inActor.CurrentAction == ActorActionId.BeingBorn) {
                yield return null;
            }

            yield return RNG.Instance.NextFloat(0.1f, 0.3f);
            while (true) {
                ActorWorld.EmitEmoji(inWorld, inActor, inId);
                yield return RNG.Instance.NextFloat(0.8f, 1.2f) * inInterval;
            }
        }

        static public IEnumerator ShakeLoop(ActorInstance inActor, ActorWorld inWorld) {
            while (inActor.CurrentAction == ActorActionId.Spawning || inActor.CurrentAction == ActorActionId.BeingBorn) {
                yield return null;
            }

            yield return inActor.AnimationTransform.MoveTo(inActor.AnimationTransform.localPosition.x + 0.4f, 0.3f, Axis.X, Space.Self)
                .Randomize().Loop().Wave(Wave.Function.Sin, 1).RevertOnCancel(false);
        }

        #endregion // Animations

#if UNITY_EDITOR

        private void Reset() {
            this.CacheComponent(ref CachedTransform);
            this.CacheComponent(ref IdleAnimation);
            this.CacheComponent(ref ColorAdjust);
            this.CacheComponent(ref CachedCollider);
        }

#endif // UNITY_EDITOR

        static ActorInstance() {
            ConfigureActionMethods(ActorActionId.Spawning, ActorInstance.StartSpawning, null, null);
            ConfigureActionMethods(ActorActionId.Waiting, ActorInstance.OnEnterWaiting, null, null);
            ConfigureActionMethods(ActorActionId.BeingBorn, ActorInstance.StartBeingBorn, null, null);
        }

        static public void PrepareWorld(ActorInstance inInstance, ActorWorld inWorld) {
            if (inInstance.Definition.IsDistributed) {
                inInstance.TankBounds = inWorld.Tank.Bounds;
            } else {
                inInstance.TankBounds = ActorDefinition.GenerateTankBounds(inWorld.Tank.Bounds, inInstance.Definition.LocalBoundsRect, inInstance.Definition.Spawning.AvoidTankTopBottomRadius, inInstance.Definition.Spawning.AvoidTankSidesRadius);
            }
        }
    }

    public struct ActorHandle : IEquatable<ActorHandle> {
        public readonly ushort Key;
        private ActorInstance m_Instance;

        public ActorHandle(ActorInstance instance, ushort key) {
            m_Instance = instance;
            Key = key;
        }

        public bool Valid {
            get {
                if (m_Instance != null && m_Instance.CheckHandle(this)) {
                    return true;
                }

                return false;
            }
        }

        public bool Equals(ActorHandle other) {
            return m_Instance == other.m_Instance && Key == other.Key;
        }

        public ActorInstance Get() {
            return Valid ? m_Instance : null;
        }

        public override int GetHashCode() {
            int hash = Key << 7;
            if (m_Instance)
                hash = (hash << 5) ^ m_Instance.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj) {
            if (obj is ActorHandle) {
                return Equals((ActorHandle) obj);
            }
            return false;
        }

        static public implicit operator ActorHandle(ActorInstance instance) {
            return instance ? instance.Handle() : default;
        }

        static public implicit operator bool(ActorHandle handle) {
            return handle.Valid;
        }

        static public bool operator ==(ActorHandle a, ActorHandle b) {
            return a.Equals(b);
        }

        static public bool operator !=(ActorHandle a, ActorHandle b) {
            return !a.Equals(b);
        }
    }

    public enum ActorActionId : byte {
        Spawning, // playing spawning animation
        Waiting, // waiting for experiment to start
        Idle, // standing still
        Hungry, // moving towards food
        Eating, // eating food
        BeingEaten, // being eaten
        Dying, // dying
        BeingBorn, // being born
        Parasiting, // stressing out another organism

        [Hidden]
        COUNT,
    }
}