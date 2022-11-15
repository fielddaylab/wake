using BeauUtil;
using UnityEngine;

namespace ScriptableBake {

    /// <summary>
    /// Flattens a collider.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class FlattenCollider2D : MonoBehaviour, IBaked {

        public const int Order = -900000;

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order {
            get { return Order; }
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            PhysicsUtils.EnsureUniformScale(transform, true);
            Baking.Destroy(this);
            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}