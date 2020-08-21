using UnityEngine;

namespace ProtoAqua.Shop
{
    [System.Serializable]
    public class Item
    {
        public string Name;
        public string Description;
        public int Price;
        public bool IsAvailable = true; 

        public static Item CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<Item>(jsonString);
        }
    }
}
