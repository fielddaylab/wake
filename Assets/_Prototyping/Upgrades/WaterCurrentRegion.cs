using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;
using Aqua.Character;

namespace ProtoAqua.Upgrades {
    [RequireComponent(typeof(KinematicForce2D))]
    public class WaterCurrentRegion : ScriptComponent
    {
        [Required] public KinematicForce2D Force;

        private void Awake() {
            Force.IsScalable = true;
        }

        #if UNITY_EDITOR

        private void Reset() {
            Force = GetComponent<KinematicForce2D>();
        }

        #endif // UNITY_EDITOR
    }
}