using System.Collections.Generic;
using UnityEngine;

namespace ProtoAqua.Shop
{
    public class PlayerData : MonoBehaviour
    {
        public List<Item> PlayerInventory = new List<Item>();
        public int PlayerCurrency = 500;
    }
}
