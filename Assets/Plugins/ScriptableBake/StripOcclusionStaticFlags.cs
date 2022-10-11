using UnityEngine;

namespace ScriptableBake {

    /// <summary>
    /// Strips occlusion-related static flags for a GameObject.
    /// </summary>
    [AddComponentMenu("ScriptableBake/Strip Occlusion Static Flags"), DisallowMultipleComponent]
    public sealed class StripOcclusionStaticFlags : MonoBehaviour, IBaked {

        public const int Order = -1000001;

        [Tooltip("If true, the full hierarchy beneath this object will have its static flags stripped.")]
        public bool Recursive = false;

        [Tooltip("Whether or not to destroy the GameObject once static flags have been set")]
        public bool DestroyGameObject = false;

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order {
            get { return Order; }
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Baking.RemoveStaticFlags(gameObject, UnityEditor.StaticEditorFlags.OccludeeStatic | UnityEditor.StaticEditorFlags.OccluderStatic, Recursive);
            Baking.Destroy(DestroyGameObject ? (UnityEngine.Object) gameObject : this);
            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}