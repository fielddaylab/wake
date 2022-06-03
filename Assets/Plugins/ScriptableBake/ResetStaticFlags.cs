using UnityEngine;

namespace ScriptableBake {

    /// <summary>
    /// Resets the static flags for a GameObject.
    /// </summary>
    [AddComponentMenu("ScriptableBake/Reset Static Flags")]
    public sealed class ResetStaticFlags : MonoBehaviour, IBaked {

        public const int Order = -1000001;

        [Tooltip("If true, the full hierarchy beneath this object will have its static flags reset.")]
        public bool Recursive = false;

        [Tooltip("Whether or not to destroy the GameObject once static flags have been set")]
        public bool DestroyGameObject = false;

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order {
            get { return Order; }
        }

        bool IBaked.Bake(BakeFlags flags) {
            Bake.ResetStaticFlags(gameObject, Recursive);
            Bake.Destroy(DestroyGameObject ? (UnityEngine.Object) gameObject : this);
            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}