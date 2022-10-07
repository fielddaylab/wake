using UnityEngine;

namespace ScriptableBake {

    /// <summary>
    /// Flattens a transform hierarchy.
    /// </summary>
    [AddComponentMenu("ScriptableBake/Flatten Hierarchy")]
    public sealed class FlattenHierarchy : MonoBehaviour, IBaked {

        public const int Order = -1000000;

        [Tooltip("Whether or not to destroy any inactive children of this GameObject")]
        public bool DestroyInactiveChildren = false;

        [Tooltip("If true, the full hierarchy beneath this object will be flattened.\nIf false, only the immediate children of this object will be affected")]
        public bool Recursive = false;

        [Tooltip("Whether or not to destroy the GameObject once flattened")]
        public bool DestroyGameObject = false;

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order {
            get { return Order; }
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Baking.FlattenHierarchy(transform, DestroyInactiveChildren, Recursive);
            Baking.Destroy(DestroyGameObject ? (UnityEngine.Object) gameObject : this);
            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}