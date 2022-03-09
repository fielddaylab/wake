using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;
using Aqua.Character;

namespace ProtoAqua.Observation {
    [RequireComponent(typeof(KinematicRepulsor2D))]
    public class WaterCurrentRegion : ScriptComponent
    {
        [Required] public KinematicRepulsor2D Force;
        public float Multiplier = 0.3f;

        private void Awake() {
            Script.OnSceneLoad(Load);
        }

        private void Load() {
            bool hasEngine = Save.Inventory.HasUpgrade(ItemIds.Engine);
            if (hasEngine) {
                Force.ResistBoost = Force.ResistBoost * Multiplier;
                Force.ForceMagnitude = Force.ForceMagnitude * Multiplier;
            }
        }

        #if UNITY_EDITOR

        private void Reset() {
            Force = GetComponent<KinematicRepulsor2D>();
        }

        #endif // UNITY_EDITOR
    }
}