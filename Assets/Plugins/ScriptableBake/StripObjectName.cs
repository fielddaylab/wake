using System;
using UnityEngine;

namespace ScriptableBake {

    /// <summary>
    /// Strips the name of the GameObject and its children.
    /// </summary>
    [AddComponentMenu("ScriptableBake/Strip GameObject Name"), DisallowMultipleComponent]
    public sealed class StripObjectName : MonoBehaviour, IBaked {

        public const int Order = FlattenHierarchy.Order - 10;

        [Tooltip("If true, will strip GameObject name in editor and development builds")]
        public bool Always = false;

        [Tooltip("If true, this will skip all objects that are a child of an Animator")]
        public bool IgnoreAnimators = true;

        #region IBaked

        #if UNITY_EDITOR

        private const BakeFlags RequiredFlags = BakeFlags.IsBuild;
        private const BakeFlags ExcludedFlags = BakeFlags.IsDevelopment;

        private const string StrippedName = "_";

        int IBaked.Order {
            get { return Order; }
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            bool always = Always;
            Transform cachedTransform = this.transform;
            Baking.Destroy(this);

            if (!always) {
                if ((flags & ExcludedFlags) != 0 || (flags & RequiredFlags) != RequiredFlags) {
                    return false;
                }
            }
            if ((flags & (BakeFlags.IsRuntime | BakeFlags.InEditor)) != 0) {
                StripNamesRuntime(cachedTransform, IgnoreAnimators);
            } else {
                StripNamesFull(cachedTransform, IgnoreAnimators);
            }
            return true;
        }

        static private void StripNamesRuntime(Transform root, bool skipAnimators) {
            string name = root.gameObject.name;
            if (!name.StartsWith("$") || name.Length > 4) {
                name = "$" + name.Substring(0, Math.Min(3, name.Length)).ToUpperInvariant();
                root.gameObject.name = name;
                Baking.SetDirty(root.gameObject);
            }

            if (skipAnimators && root.GetComponent<Animator>()) {
                return;
            }

            int childCount = root.childCount;
            for(int i = 0; i < childCount; i++) {
                StripNamesRuntime(root.GetChild(i), skipAnimators);
            }
        }

        static private void StripNamesFull(Transform root, bool skipAnimators) {
            root.gameObject.name = StrippedName;
            Baking.SetDirty(root.gameObject);

            if (skipAnimators && root.GetComponent<Animator>()) {
                return;
            }
            
            int childCount = root.childCount;
            for(int i = 0; i < childCount; i++) {
                StripNamesFull(root.GetChild(i), skipAnimators);
            }
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}