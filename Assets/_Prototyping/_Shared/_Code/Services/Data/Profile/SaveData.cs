using BeauData;

namespace ProtoAqua.Profile
{
    public class SaveData : ISerializedObject, ISerializedVersion
    {
        public string Id;
        public long LastUpdated;

        public CharacterProfile Character = new CharacterProfile();

        #region ISerializedData

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref Id);
            ioSerializer.Serialize("lastUpdated", ref LastUpdated, 0L);
            ioSerializer.Object("character", ref Character);
        }

        #endregion // ISerializedData
    }
}