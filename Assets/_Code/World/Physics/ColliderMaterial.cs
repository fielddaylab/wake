using UnityEngine;
using Aqua;
using Aqua.Scripting;
using BeauUtil;
using System.Collections;
using BeauRoutine;
using System.Collections.Generic;

namespace Aqua
{
    [RequireComponent(typeof(Collider2D)), DisallowMultipleComponent]
    public class ColliderMaterial : MonoBehaviour, IColliderMaterialSource {
        [AutoEnum] public ColliderMaterialId Material;

        static public ColliderMaterialId Find(Collider2D collider) {
            if (!collider.TryGetComponent<IColliderMaterialSource>(out IColliderMaterialSource src)) {
                return DefaultMaterial;
            }
            
            return src.GetMaterial(collider);
        }

        public const ColliderMaterialId DefaultMaterial = ColliderMaterialId.Invisible;

        public ColliderMaterialId GetMaterial(Collider2D collider) {
            return Material;
        }
    }

    public interface IColliderMaterialSource {
        ColliderMaterialId GetMaterial(Collider2D collider);
    }

    [LabeledEnum(false)]
    public enum ColliderMaterialId : byte {
        [Label("Null")] Invisible,

        [Label("Sand")] Sand,
        [Label("Rock")] Rock,
        [Label("Ice")] Ice,
        [Label("Metal")] Metal,

        [Label("Weird/Flesh")] Flesh,
        [Label("Weird/Bone")] Bone,

        [Label("Weird/Squeaky")] Squeaky
    }
}