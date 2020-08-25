using BeauData;
using UnityEditor;
using UnityEngine;

namespace ProtoAqua.Shop
{
    public class Item : ISerializedObject
    {
        private string name;
        private string description;
        private int price;
        private bool isAvailable;
        private string spritePath;

        #region Accessors

        public string Name 
        { 
            get { return name; }
            set { name = value; }
        }

        public string Description
        { 
            get { return description; }
            set { description = value; }
        }

        public int Price 
        { 
            get { return price; }
            set { price = value; }
        }

        public bool IsAvailable
        { 
            get { return isAvailable; }
            set { isAvailable = value; }
        }

        public string SpritePath
        { 
            get { return spritePath; }
            set { spritePath = value; }
        }

        #endregion // Accessors

        public Sprite ItemSprite { get; set; }

        // Serialize the Item and load the ItemSprite asset from the given path
        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("Name", ref name);
            ioSerializer.Serialize("Description", ref description);
            ioSerializer.Serialize("Price", ref price);
            ioSerializer.Serialize("IsAvailable", ref isAvailable);
            ioSerializer.Serialize("SpritePath", ref spritePath);
            
            ItemSprite = (Sprite)AssetDatabase.LoadAssetAtPath(spritePath, typeof(Sprite));
        }
    }
}
