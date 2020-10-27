using BeauData;

namespace ProtoAqua.Profile
{
    public class SaveData : ISerializedObject, ISerializedVersion
    {
        public string Id;
        public long LastUpdated;

        public CharacterProfile Character = new CharacterProfile();
        public InventoryData Inventory = new InventoryData();
        public ScriptingData Script = new ScriptingData();
        public BestiaryData Bestiary = new BestiaryData();

        #region ISerializedData

        ushort ISerializedVersion.Version { get { return 3; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref Id);
            ioSerializer.Serialize("lastUpdated", ref LastUpdated, 0L);
            ioSerializer.Object("character", ref Character);

            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.Object("inventory", ref Inventory);
                ioSerializer.Object("script", ref Script);
            }

            if (ioSerializer.ObjectVersion >= 3)
            {
                ioSerializer.Object("bestiary", ref Bestiary);
            }
        }

        #endregion // ISerializedData
    }
}