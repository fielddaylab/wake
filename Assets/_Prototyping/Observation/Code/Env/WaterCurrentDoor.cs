using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;
using Aqua.Character;

namespace ProtoAqua.Observation {
    [RequireComponent(typeof(SceneInteractable))]
    public class WaterCurrentDoor : ScriptComponent
    {
        [Required] public SceneInteractable Door;
        [Tooltip("If set, this door cannot be used if the player does not have the engine.\nOtherwise, it will automatically trigger on collision.")]
        public bool BlockExit;

        private void Awake() {
            Script.OnSceneLoad(Load);
        }

        private void Load() {
            bool hasEngine = Save.Inventory.HasUpgrade(ItemIds.Engine);
            if (hasEngine) {
                Door.OverrideAuto(false);
                Door.Unlock();
            } else {
                if (BlockExit) {
                    Door.Lock();
                    Door.OverrideAuto(false);
                    Door.enabled = false;
                } else {
                    Door.Unlock();
                    Door.OverrideAuto(true);
                }
            }
        }

        #if UNITY_EDITOR

        private void Reset() {
            Door = GetComponent<SceneInteractable>();
        }

        #endif // UNITY_EDITOR
    }
}