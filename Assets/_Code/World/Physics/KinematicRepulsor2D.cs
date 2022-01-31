using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class KinematicRepulsor2D : MonoBehaviour
    {
        public enum DirectionMode
        {
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

        #endregion // Inspector

        [NonSerialized] private TriggerListener2D m_Listener;

        private void Awake()
        {
            m_Listener = WorldUtils.TrackLayerMask(Collider, TrackingMask, null, null);
        }

        private void FixedUpdate()
        {
            if (!Services.Physics.Enabled)
                return;

            Vector2 force = ForceDirection * ForceMagnitude * Time.fixedDeltaTime;
            Vector2 myCenter = Collider.bounds.center;

            foreach(var obj in m_Listener.Occupants())
            {
                KinematicObject2D k2d = obj.Rigidbody.GetComponent<KinematicObject2D>();
                if (!k2d)
                    continue;

                switch(Mode)
                {
                    case DirectionMode.Vector: {
                        k2d.State.Velocity += force;
                        break;
                    }

                    case DirectionMode.Away: {
                        Vector2 kCenter = obj.Rigidbody.position;
                        Vector2 vec = kCenter - myCenter;
                        vec.Normalize();
                        vec *= ForceMagnitude * Time.fixedDeltaTime;
                        k2d.State.Velocity += vec;
                        break;
                    }
                }
            }
        }
    }
}