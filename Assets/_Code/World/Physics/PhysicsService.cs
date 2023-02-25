#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Aqua.Debugging;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public class PhysicsService : ServiceBehaviour, IPauseable, IDebuggable
    {
        public const float DefaultContactOffset = (1f / 128f);
        private const float OverlapThreshold = DefaultContactOffset;

        #region Inspector

        #endregion // Inspector

        private RingBuffer<KinematicObject2D> m_KinematicObjects = new RingBuffer<KinematicObject2D>(8, RingBufferMode.Expand);
        private Dictionary<RuntimeObjectHandle, KinematicObject2D> m_RigidbodyMap = Collections.NewDictionary<RuntimeObjectHandle, KinematicObject2D>(8);
        private Unsafe.ArenaHandle m_ContactArena;

        private ulong m_TickCount;

        #region Register/Deregister

        public void Register(KinematicObject2D inObject)
        {
            Assert.False(m_KinematicObjects.Contains(inObject), "Object '{0}' is already registered to PhysicsService", inObject);
            m_KinematicObjects.PushBack(inObject);
            m_RigidbodyMap.Add(inObject.Body, inObject);
            SetupRigidbody(inObject.Body);
        }

        public void Deregister(KinematicObject2D inObject)
        {
            Assert.True(m_KinematicObjects.Contains(inObject), "Object '{0}' is not registered to PhysicsService", inObject);
            m_KinematicObjects.FastRemove(inObject);
            m_RigidbodyMap.Remove(inObject.Body);
            inObject.Body.velocity = default;

            unsafe {
                inObject.Contacts = null;
                inObject.ContactCount = 0;
            }
        }

        private void SetupRigidbody(Rigidbody2D inBody)
        {
            inBody.simulated = true;
            inBody.isKinematic = true;
        }

        #endregion // Register/Deregister

        /// <summary>
        /// Returns the KinematicObject2D for the given Rigidbody2D
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KinematicObject2D Lookup(Rigidbody2D inBody)
        {
            KinematicObject2D kinematic;
            m_RigidbodyMap.TryGetValue(inBody, out kinematic);
            return kinematic;
        }

        [NonSerialized] public bool Enabled = false;
        [NonSerialized] public float TimeScale = 1;

        private float m_FixedDeltaTime;

        #region Handlers

        private void FixedUpdate()
        {
            if (!Enabled)
                return;
            
            float deltaTime = Time.fixedDeltaTime * TimeScale;
            if (deltaTime <= 0)
                return;

            int contactCount = 0;
            
            // Tick forward

            Unsafe.ResetArena(m_ContactArena);

            BeginTick();
            Physics.Simulate(deltaTime);
            contactCount += Tick(deltaTime, m_KinematicObjects, m_ContactArena, m_TickCount);
            EndTick();

            // Pass contacts off

            if (contactCount > 0)
            {
                DebugService.Log(LogMask.Physics, "[PhysicsService] Generated {0} contacts on tick {1}", contactCount, m_TickCount);

                foreach(var obj in m_KinematicObjects)
                {
                    if (obj.ContactCount > 0 && !obj.OnContact.IsEmpty) {
                        obj.OnContact.Invoke(obj);
                    }
                }
            }

            m_TickCount++;
        }

        #endregion // Handlers

        #region Service

        protected override void Initialize()
        {
            base.Initialize();

            Time.fixedDeltaTime = Time.fixedDeltaTime = 1f / 60;
            Physics2D.autoSimulation = false;
            Physics.autoSimulation = false;
            Physics.autoSyncTransforms = false;

            m_ContactArena = Unsafe.CreateArena(Unsafe.SizeOf<PhysicsContact>() * 64, "physics");
        }

        protected override void Shutdown()
        {
            Unsafe.DestroyArena(m_ContactArena);

            base.Shutdown();
        }

        #endregion // Service

        #region IPauseable
        
        bool IPauseable.IsPaused()
        {
            return !Enabled;
        }

        void IPauseable.Pause()
        {
            Enabled = false;
        }

        void IPauseable.Resume()
        {
            Enabled = true;
        }

        #endregion // IPauseable

        #region Tick Logic

        static private void BeginTick()
        {
            Physics2D.autoSyncTransforms = false;
            Physics2D.SyncTransforms();

            Physics.autoSyncTransforms = false;
            Physics.SyncTransforms();
        }

        static private void EndTick()
        {
            Physics2D.SyncTransforms();
            Physics2D.autoSyncTransforms = true;
        }

        static private unsafe int Tick(float inDeltaTime, RingBuffer<KinematicObject2D> inObjects, Unsafe.ArenaHandle inContactAllocator, ulong inTickIdx)
        {
            int addedContacts = 0;

            ContactPoint2D[] contactBuffer = s_ContactBuffer;
            ContactFilter2D filter = default(ContactFilter2D);
            Rigidbody2D objBody;
            Collider2D objCollider;
            KinematicObject2D obj;

            int objectCount = inObjects.Count;

            // buffers
            KinematicState2D* states = stackalloc KinematicState2D[objectCount];
            KinematicConfig2D* configs = stackalloc KinematicConfig2D[objectCount];
            Vector2* frameOffset = stackalloc Vector2[objectCount];
            Vector2* positions = stackalloc Vector2[objectCount];

            float invDeltaTime = 1f / inDeltaTime;

            // copy state into buffers
            int objIdx = 0;
            for(objIdx = 0; objIdx < objectCount; objIdx++) {
                obj = inObjects[objIdx];

                obj.State.Velocity += obj.AccumulatedForce * obj.AccumulatedForceMultiplier;
                obj.AccumulatedForce = default;

                states[objIdx] = obj.State;
                configs[objIdx] = obj.Config;
                positions[objIdx] = obj.Body.position;
                configs[objIdx].Drag += obj.AdditionalDrag * obj.AdditionalDragMultiplier;
                obj.Body.useFullKinematicContacts = true;

                obj.Contacts = null;
                obj.ContactCount = 0;
            }

            // integrate state
            for(objIdx = 0; objIdx < objectCount; objIdx++)
            {
                frameOffset[objIdx] = KinematicMath2D.Integrate(ref states[objIdx], ref configs[objIdx], inDeltaTime);
            }

            // move objects
            for(objIdx = 0; objIdx < objectCount; objIdx++) {
                obj = inObjects[objIdx];
                positions[objIdx] += frameOffset[objIdx];
                obj.Body.velocity = frameOffset[objIdx] * invDeltaTime;
                SyncPosition(positions[objIdx], obj.Body, null);
                objIdx++;
            }

            bool bRun = Physics2D.Simulate(inDeltaTime);
            Assert.True(bRun, "Physics update failed to run");

            // resolve object penetration
            for(objIdx = 0; objIdx < objectCount; objIdx++) {
                obj = inObjects[objIdx];
                if (obj.SolidMask == 0) {
                    continue;
                }

                objBody = obj.Body;
                objCollider = obj.Collider;

                filter.useLayerMask = true;
                filter.layerMask = obj.SolidMask;
                int contactCount = Physics2D.GetContacts(objCollider, filter, contactBuffer);

                if (contactCount <= 0) {
                    continue;
                }

                Array.Sort(contactBuffer, 0, contactCount, ContactSorter.Instance);

                ContactPoint2D contact;
                float separation;
                float adjustedSeparation;

                Vector2 accumulatedSeparationVector = Vector2.zero;

                PhysicsContact* contactOutput = Unsafe.AllocArray<PhysicsContact>(inContactAllocator, contactCount);
                obj.Contacts = contactOutput;
                
                KinematicState2D originalState = states[objIdx];
                Vector2 originalVelocityNormalized = originalState.Velocity.normalized;
                float originalVelocityMagnitude = originalState.Velocity.magnitude;
                int contactGenIdx = 0;

                Collider2D checkingCollider = null;
                Collider2D lastCollider = null;

                for(int contactIdx = 0; contactIdx < contactCount; ++contactIdx)
                {
                    contact = contactBuffer[contactIdx];
                    Assert.True(contact.otherCollider == objCollider);
                    
                    separation = contact.separation + OverlapThreshold;
                    adjustedSeparation = separation + Vector2.Dot(accumulatedSeparationVector, contact.normal);

                    if (separation >= 0) {
                        continue;
                    }

                    float impact = -Vector2.Dot(originalVelocityNormalized, contact.normal) * originalVelocityMagnitude;
                    float slide = 0;

                    checkingCollider = contact.collider;
                    if (checkingCollider != lastCollider) {
                        contactOutput[contactGenIdx++] = new PhysicsContact(obj, originalState, contact.collider, contact.point, contact.normal, impact, slide);
                        addedContacts++;
                        lastCollider = checkingCollider;
                    } else {
                        ref PhysicsContact lastContact = ref contactOutput[contactGenIdx - 1];
                        lastContact.Point = (lastContact.Point + contact.point) * 0.5f; // average contact point
                        lastContact.Impact = (lastContact.Impact + impact) * 0.5f; // and average impact value
                    }

                    if (adjustedSeparation < 0)
                    {
                        Vector2 separateVector = contact.normal;
                        separateVector.x *= -adjustedSeparation;
                        separateVector.y *= -adjustedSeparation;
                        
                        DebugService.Log(LogMask.Physics, "[PhysicsService] Resolving contact on {0} by {1} {2} (tick {3})", objCollider, contact.normal, separation, inTickIdx);

                        positions[objIdx] += separateVector;
                        frameOffset[objIdx] += separateVector;

                        accumulatedSeparationVector += separateVector;
                    }

                    AdjustVelocity(ref states[objIdx].Velocity, contact.normal, 0);
                }

                if (accumulatedSeparationVector.sqrMagnitude > 0)
                {
                    positions[objIdx] += accumulatedSeparationVector;
                    frameOffset[objIdx] += accumulatedSeparationVector;
                }

                obj.ContactCount = contactGenIdx;

                Array.Clear(contactBuffer, 0, contactCount);
            }

            for(objIdx = 0; objIdx < objectCount; objIdx++) {
                obj = inObjects[objIdx];
                SyncPosition(positions[objIdx], obj.Body, obj.Transform);
                obj.State = states[objIdx];
                obj.Body.useFullKinematicContacts = false;
                objIdx++;
            }

            return addedContacts;
        }

        static private void AdjustVelocity(ref Vector2 ioVelocity, Vector2 inNormal, float inBounce)
        {
            float val = (-1f - inBounce) * Vector2.Dot(inNormal, ioVelocity);
            ioVelocity.x = val * inNormal.x + ioVelocity.x;
            ioVelocity.y = val * inNormal.y + ioVelocity.y;
        }

        static private void SyncPosition(Vector2 inPosition, Rigidbody2D inBody, Transform inTransform)
        {
            inBody.position = inPosition;

            if (!inTransform.IsReferenceNull())
            {
                Vector3 transformPos = inTransform.position;
                transformPos.x = inPosition.x;
                transformPos.y = inPosition.y;
                inTransform.position = transformPos;
            }
        }

        static private readonly ContactPoint2D[] s_ContactBuffer = new ContactPoint2D[16];

        private class ContactSorter : IComparer<ContactPoint2D>
        {
            static public readonly ContactSorter Instance = new ContactSorter();

            public int Compare(ContactPoint2D x, ContactPoint2D y)
            {
                return x.collider.GetInstanceID().CompareTo(y.collider.GetInstanceID());
            }
        }

        #endregion // Tick
    
        #region Utils

        /// <summary>
        /// Checks if a solid object is next 
        /// </summary>
        static public bool CheckSolid(KinematicObject2D inObject, Vector2 inOffset, out Vector2 outNormal)
        {
            RaycastHit2D hit;
            if (inObject.SolidMask != 0 && inObject.Collider.IsCastOverlapping(inObject.SolidMask, inOffset, out hit))
            {
                outNormal = hit.normal;
                return true;
            }

            outNormal = default(Vector2);
            return false;
        }

        /// <summary>
        /// Smoothly deflects a vector off of a normal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Vector2 SmoothDeflect(Vector2 inDirection, Vector2 inNormal, float inBounce = 0)
        {
            AdjustVelocity(ref inDirection, inNormal, inBounce);
            return inDirection;
        }

        /// <summary>
        /// Smooths velocity additions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Vector2 SmoothVelocity(Vector2 inDirection)
        {
            if (inDirection.x > -DefaultContactOffset && inDirection.x < DefaultContactOffset)
                inDirection.x = 0;
            if (inDirection.y > -DefaultContactOffset && inDirection.y < DefaultContactOffset)
                inDirection.y = 0;

            return inDirection;
        }

        /// <summary>
        /// Returns a unit offset for the given vector.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Vector2 UnitOffset(Vector2 inVector)
        {
            Vector2 vec = inVector.normalized;
            float invMag = DefaultContactOffset / inVector.magnitude;
            vec.x *= invMag;
            vec.y *= invMag;
            return vec;
        }

        /// <summary>
        /// Performs collision checks but does not update positions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void PerformCollisionChecks()
        {
            Physics2D.Simulate(CollisionCheckTick);
        }

        private const float CollisionCheckTick = 1f / 65536f;

        #endregion // Utils

        #region IDebuggable

        #if DEVELOPMENT

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus()
        {
            DMInfo physicsMenu = new DMInfo("Physics", 8);
            physicsMenu.AddToggle("Noclip (Free Player Movement)", () => {
                return Services.State.Player != null && Services.State.Player.Kinematics.SolidMask == 0;
            }, (b) => {
                Services.State.Player.Kinematics.SolidMask = b ? 0 : GameLayers.Solid_Mask;
            }, () => Services.State.Player != null);

            yield return physicsMenu;
        }

        #endif // DEVELOPMENT

        #endregion // IDebuggable
    }
}