using BeauData;
using BeauUtil;
using BeauUtil.Variants;

namespace Aqua
{
    public struct GTEventDate : ISerializedObject
    {
        public StringHash32 Id;
        public GTDate Time;
        public Variant Data;

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32Proxy("id", ref Id);
            ioSerializer.Object("time", ref Time);
            ioSerializer.Object("data", ref Data);
        }
    }
}