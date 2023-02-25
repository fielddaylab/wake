using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua {
    public class KinematicForce2D : MonoBehaviour {
        public enum DirectionMode {
            Away,
            Vector
        }

        #region Inspector

        [Header("Components")]

        [Required(ComponentLookupDirection.Children)] public Collider2D Collider;

        [Header("Collisions")]

        public LayerMask TrackingMask;

        [Header("Direction")]

        public DirectionMode Mode;
        public Vector2 ForceDirection;
        public float ForceMagnitude;
        [Tooltip("Multiplier on Force Magnitude when object is moving against force direction")] public float ResistBoost;
        [Tooltip("Whether this force can be scaled by the object")] public bool IsScalable;

        [Header("")]

        #endregion // Inspector

        [NonSerialized] private TriggerListener2D m_Listener;
        [NonSerialized] private RingBuffer<KinematicObject2D> m_Occupants = new RingBuffer<KinematicObject2D>(4, RingBufferMode.Overwrite);

        private void Awake() {
            m_Listener = WorldUtils.ListenForLayerMask(Collider, TrackingMask, OnEnter, OnLeave);
        }

        private void OnEnter(Collider2D collider) {
            KinematicObject2D obj = collider.attachedRigidbody.GetComponent<KinematicObject2D>();
            if (obj != null) {
                m_Occupants.PushBack(obj);
            }
        }

        private void OnLeave(Collider2D collider) {
            KinematicObject2D obj = collider.attachedRigidbody.GetComponent<KinematicObject2D>();
            if (obj != null) {
                m_Occupants.FastRemove(obj);
            }
        }

        private void FixedUpdate() {
            if (!Services.Physics.Enabled)
                return;

            Vector2 force = (transform.rotation * ForceDirection) * (ForceMagnitude * Time.fixedDeltaTime);
            Vector2 forceNormal = force.normalized;
            Vector2 myCenter = Collider.bounds.center;

            for(int i = 0, count = m_Occupants.Count; i < count; i++) {
                var k2d = m_Occupants[i];
                if (!k2d || !k2d.enabled)
                    continue;

                float mult = 1;
                if (ResistBoost != 0) {
                    mult = 1 + TweenUtil.Evaluate(Curve.QuadOut, -Math.Min(0, Vector2.Dot(k2d.State.Velocity.normalized, forceNormal))) * ResistBoost;
                }
                if (IsScalable) {
                    mult *= k2d.ScaledForceMultiplier;
                }

                switch (Mode) {
                    case DirectionMode.Vector: {
                            k2d.State.Velocity += force * mult;
                            break;
                        }

                    case DirectionMode.Away: {
                            Vector2 kCenter = k2d.Body.position;
                            Vector2 vec = kCenter - myCenter;
                            vec.Normalize();
                            vec *= mult * ForceMagnitude * Time.fixedDeltaTime;
                            k2d.State.Velocity += vec;
                            break;
                        }
                }
            }
        }
    }
}