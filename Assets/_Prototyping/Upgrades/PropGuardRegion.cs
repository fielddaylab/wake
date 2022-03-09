using UnityEngine;
using Aqua;
using Aqua.Scripting;
using BeauUtil;

namespace ProtoAqua.Upgrades {
    [RequireComponent(typeof(KinematicDrag2D))]
    public sealed class PropGuardRegion : MonoBehaviour, ISceneLoadHandler {
        #region Inspector

        [Required] public KinematicDrag2D Drag;
        [Required] public Collider2D[] Barriers;
        public float Multiplier = 0.5f;

        #endregion // Inspector

        private void Load() {
            bool bHasUpgrade = Save.Inventory.HasUpgrade(ItemIds.PropGuard);
            if (bHasUpgrade) {
                Drag.Drag *= Multiplier;
                foreach(var barrier in Barriers) {
                    barrier.enabled = false;
                }
            }
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            Load();
        }

        #if UNITY_EDITOR

        private void Reset() {
            Drag = GetComponent<KinematicDrag2D>();
        }

        #endif // UNITY_EDITOR
    }
}