using UnityEngine;
using Aqua;
using BeauUtil;
using ScriptableBake;

namespace ProtoAqua.Observation
{
    public class SpecterSpawnRegion : MonoBehaviour, IBaked
    {
        [Required] public Collider2D Region;
        [Required] public Transform[] Locations;
        [HideInInspector] public Vector3[] LocationPositions;

        #if UNITY_EDITOR

        public int Order { get { return 10; } }

        public bool Bake(BakeFlags flags, BakeContext context) {
            LocationPositions = new Vector3[Locations.Length + 1];
            for(int i = 0; i < Locations.Length; i++)
            {
                Transform target = Locations[i];
                LocationPositions[i] = target.position;
                if (Baking.IsEmptyLeaf(target))
                    Baking.Destroy(target.gameObject);
            }
            LocationPositions[Locations.Length] = transform.position;
            Locations = null;
            return true;
        }

        #endif // UNITY_EDITOR
    }
}