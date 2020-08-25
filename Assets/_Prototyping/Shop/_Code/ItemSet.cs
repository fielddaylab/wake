using BeauData;

namespace ProtoAqua.Shop
{
    public class ItemSet : ISerializedObject
    {
        public Item[] ItemData;

        // Load the array of items from a given JSON file
        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.ObjectArray("ItemData", ref ItemData);
        }
    }
}
