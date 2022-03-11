using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua {
    public class KinematicRepulsor2D : MonoBehaviour {
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

        #endregion // Inspector

        [NonSerialized] private TriggerListener2D m_Listener;

        private void Awake() {
            m_Listener = WorldUtils.TrackLayerMask(Collider, TrackingMask, null, null);
        }

        private void FixedUpdate() {
            if (!Services.Physics.Enabled)
                return;

            Vector2 force = ForceDirection * ForceMagnitude * Time.fixedDeltaTime;
            Vector2 forceNormal = force.normalized;
            Vector2 myCenter = Collider.bounds.center;

            foreach (var obj in m_Listener.Occupants()) {
                KinematicObject2D k2d = obj.Rigidbody.GetComponent<KinematicObject2D>();
                if (!k2d || !k2d.enabled)
                    continue;

                float mult = 1;
                if (ResistBoost != 0) {
                    mult = 1 + TweenUtil.Evaluate(Curve.QuadOut, -Math.Min(0, Vector2.Dot(k2d.State.Velocity.normalized, forceNormal))) * ResistBoost;
                }

                switch (Mode) {
                    case DirectionMode.Vector: {
                            k2d.State.Velocity += force * mult;
                            break;
                        }

                    case DirectionMode.Away: {
                            Vector2 kCenter = obj.Rigidbody.position;
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