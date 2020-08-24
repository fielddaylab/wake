using BeauData;

namespace ProtoAqua.Shop
{
    public class ItemSet : ISerializedObject
    {
        public Item[] ItemData;

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.ObjectArray("ItemData", ref ItemData);
        }
    }
}
