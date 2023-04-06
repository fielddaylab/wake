using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class KinematicObject2D : MonoBehaviour
    {
        #region Inspector

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public KinematicState2D State;

        [Inline]
        public KinematicConfig2D Config;

        [Header("Components")]

        [Required(ComponentLookupDirection.Self)] public Rigidbody2D Body;
        [Required(ComponentLookupDirection.Children)] public Collider2D Collider;
        
        [Header("Solid")]

        public LayerMask SolidMask;
        public float Bounce = 0;

        [NonSerialized] public Transform Transform;
        
        [NonSerialized] public unsafe PhysicsContact* Contacts;
        [NonSerialized] public int ContactCount;

        [NonSerialized] public Vector2 AccumulatedForce;
        [NonSerialized] public float AdditionalDrag;

        [NonSerialized] public float ScaledForceMultiplier = 1;

        [NonSerialized] public float AdditionalDragMultiplier = 1;
        [NonSerialized] public float AccumulatedForceMultiplier = 1;

        [NonSerialized] public CastableEvent<KinematicObject2D> OnContact = new CastableEvent<KinematicObject2D>();

        #endregion // Inspector

        private void OnEnable()
        {
            this.CacheComponent(ref Transform);
            Services.Physics.Register(this);
        }

        private void OnDisable()
        {
            Services.Physics?.Deregister(this);
        }

        public bool CheckSolid(Vector2 inOffset)
        {
            Vector2 ignored;
            return PhysicsService.CheckSolid(this, inOffset, out ignored);
        }

        public bool CheckSolid(Vector2 inOffset, out Vector2 outNormal)
        {
            return PhysicsService.CheckSolid(this, inOffset, out outNormal);
        }
    }
}