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
        private const int TickIterations = 1;

        private RingBuffer<KinematicObject2D> m_KinematicObjects = new RingBuffer<KinematicObject2D>();
        private Dictionary<Rigidbody2D, KinematicObject2D> m_RigidbodyMap = new Dictionary<Rigidbody2D, KinematicObject2D>(CompareUtils.DefaultComparer<Rigidbody2D>());
        private Unsafe.ArenaHandle m_Allocator;

        private ulong m_TickCount;

        #region Register/Deregister

        public unsafe void Register(KinematicObject2D inObject)
        {
            Assert.False(m_KinematicObjects.Contains(inObject), "Object '{0}' is already registered to PhysicsService", inObject);
            m_KinematicObjects.PushBack(inObject);
            m_RigidbodyMap.Add(inObject.Body, inObject);
            SetupRigidbody(inObject.Body);
        }

        public unsafe void Deregister(KinematicObject2D inObject)
        {
            Assert.True(m_KinematicObjects.Contains(inObject), "Object '{0}' is not registered to PhysicsService", inObject);
            m_KinematicObjects.FastRemove(inObject);
            m_RigidbodyMap.Remove(inObject.Body);
            inObject.Body.velocity = default;
            inObject.Contacts = null;
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

        private float m_FixedDeltaTime;

        #region Handlers

        private void FixedUpdate()
        {
            if (!Enabled)
                return;
            
            float deltaTime = Time.fixedDeltaTime;
            if (deltaTime <= 0)
                return;

            m_Allocator.Reset();

            int contactCount = 0;
            
            // Tick forward

            BeginTick();
            Physics.Simulate(deltaTime);
            contactCount += Tick(deltaTime, m_KinematicObjects, m_Allocator, m_TickCount);
            EndTick();

            // Pass contacts off

            if (contactCount > 0)
            {
                DebugService.Log(LogMask.Physics, "[PhysicsService] Generated {0} contacts on tick {1}", contactCount, m_TickCount);
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

            Unsafe.TryDestroyArena(ref m_Allocator);
            m_Allocator = Unsafe.CreateArena(1024 * 4, "Physics"); // 4KiB
        }

        protected override void Shutdown()
        {
            Unsafe.TryDestroyArena(ref m_Allocator);

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

        static private unsafe int Tick(float inDeltaTime, RingBuffer<KinematicObject2D> inObjects, Unsafe.ArenaHandle inAlloc, ulong inTickIdx)
        {
            int addedContacts = 0;

            ContactPoint2D[] contactBuffer = s_ContactBuffer;
            RaycastHit2D[] raycastBuffer = s_RaycastBuffer;
            ContactFilter2D filter = default(ContactFilter2D);
            Rigidbody2D objBody;
            Collider2D objCollider;

            RingBuffer<KinematicObject2D> objectCollection = inObjects;
            int objectCount = objectCollection.Count;

            // buffers
            KinematicState2D* states = stackalloc KinematicState2D[objectCount];
            KinematicConfig2D* configs = stackalloc KinematicConfig2D[objectCount];
            Vector2* frameOffset = stackalloc Vector2[objectCount];
            Vector2* positions = stackalloc Vector2[objectCount];

            // copy state into buffers
            int objIdx = 0;
            foreach(var obj in objectCollection)
            {
                obj.State.Velocity += obj.AccumulatedForce * obj.AccumulatedForceMultiplier;
                obj.AccumulatedForce = default;

                states[objIdx] = obj.State;
                configs[objIdx] = obj.Config;
                positions[objIdx] = obj.Body.position;
                configs[objIdx].Drag += obj.AdditionalDrag * obj.AdditionalDragMultiplier;
                obj.Body.useFullKinematicContacts = true;
                obj.Contacts = null;
                objIdx++;
            }

            float incrementDeltaTime = inDeltaTime / TickIterations;
            float invDeltaTime = 1f / incrementDeltaTime;

            for(int iterationIdx = 0; iterationIdx < TickIterations; iterationIdx++)
            {
                // integrate state
                for(objIdx = 0; objIdx < objectCount; objIdx++)
                {
                    frameOffset[objIdx] = KinematicMath2D.Integrate(ref states[objIdx], ref configs[objIdx], incrementDeltaTime);
                }

                // move objects
                objIdx = 0;
                foreach(var obj in objectCollection)
                {
                    positions[objIdx] += frameOffset[objIdx];
                    obj.Body.velocity = frameOffset[objIdx] * invDeltaTime;
                    SyncPosition(positions[objIdx], obj.Body, null);
                    objIdx++;
                }

                bool bRun = Physics2D.Simulate(incrementDeltaTime);
                Assert.True(bRun, "Physics update failed to run");

                // resolve object penetration
                objIdx = 0;
                foreach(var obj in objectCollection)
                {
                    obj.Contacts = null;
                    obj.ContactCount = 0;

                    if (obj.SolidMask != 0)
                    {
                        objBody = obj.Body;
                        objCollider = obj.Collider;
                        
                        ContactPoint2D contact;
                        float separation;

                        filter.useLayerMask = true;
                        filter.layerMask = obj.SolidMask;
                        int contactCount = Physics2D.GetContacts(objCollider, filter, contactBuffer);
                        if (contactCount > 0)
                        {
                            Array.Sort(contactBuffer, 0, contactCount, ContactSorter.Instance);

                            Collider2D lastCollider = null;
                            Vector2 separationAccum = Vector2.zero;
                            PhysicsContact lastContact = default(PhysicsContact);
                            KinematicState2D originalState = states[objIdx];

                            PhysicsContact* localBuffer = Unsafe.AllocArray<PhysicsContact>(inAlloc, contactCount);
                            int localContactCount = 0;

                            for(int contactIdx = 0; contactIdx < contactCount; ++contactIdx)
                            {
                                contact = contactBuffer[contactIdx];
                                separation = contact.separation + OverlapThreshold;
                                if (separation < 0 && contact.otherCollider == objCollider)
                                {
                                    Vector2 separateVector = contact.normal;
                                    separateVector.x *= -separation;
                                    separateVector.y *= -separation;
                                    
                                    DebugService.Log(LogMask.Physics, "[PhysicsService] Resolving contact on {0} by {1} {2} (tick {3}, tickIter {4})", objCollider, contact.normal, separation, inTickIdx, iterationIdx);

                                    if (!ReferenceEquals(lastCollider, contact.collider))
                                    {
                                        lastCollider = contact.collider;
                                        localBuffer[localContactCount++] = lastContact = new PhysicsContact(obj, originalState, contact.collider, contact.point, contact.normal);
                                        addedContacts++;

                                        positions[objIdx] += separationAccum;
                                        frameOffset[objIdx] += separationAccum;

                                        separationAccum = separateVector;
                                    }
                                    else if (lastContact)
                                    {
                                        // average out so we only generate one contact per other collider
                                        lastContact.Point = (lastContact.Point + contact.point) * 0.5f;
                                        lastContact.Normal = ((lastContact.Normal + contact.normal) * 0.5f).normalized;
                                        localBuffer[localContactCount - 1] = lastContact;

                                        // some wonky averaging of separation?
                                        // 1. max distance to move
                                        // 2. average direction to move
                                        float maxSep = (float) Math.Sqrt(Mathf.Max(separationAccum.sqrMagnitude, separateVector.sqrMagnitude));
                                        separationAccum = ((separationAccum.normalized + separateVector.normalized) / 2).normalized * maxSep;
                                    }

                                    AdjustVelocity(ref states[objIdx].Velocity, contact.normal, 0);
                                }
                            }

                            if (separationAccum.sqrMagnitude > 0)
                            {
                                positions[objIdx] += separationAccum;
                                frameOffset[objIdx] += separationAccum;
                            }

                            obj.Contacts = localBuffer;
                            obj.ContactCount = localContactCount;

                            Array.Clear(contactBuffer, 0, contactCount);
                        }
                    }
                    ++objIdx;
                }

                objIdx = 0;
                foreach(var obj in objectCollection)
                {
                    SyncPosition(positions[objIdx], obj.Body, obj.Transform);
                    objIdx++;
                }
            }

            // copy state from buffers to objects
            objIdx = 0;
            foreach(var obj in objectCollection)
            {
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
        static private readonly RaycastHit2D[] s_RaycastBuffer = new RaycastHit2D[16];

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