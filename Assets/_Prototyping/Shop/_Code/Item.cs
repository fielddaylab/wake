using UnityEngine;

namespace ProtoAqua.Shop
{
    public class Item : MonoBehaviour
    {
        public string Name { get; }
        public string Description { get; }
        public int Price { get; set; }
        public bool IsAvailable { get; set; } 
    }
}
