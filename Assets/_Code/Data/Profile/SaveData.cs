using BeauData;

namespace Aqua.Profile
{
    public class SaveData : IProfileChunk, ISerializedVersion
    {
        public string Id;
        public long LastUpdated;

        public CharacterProfile Character = new CharacterProfile();
        public InventoryData Inventory = new InventoryData();
        public ScriptingData Script = new ScriptingData();
        public BestiaryData Bestiary = new BestiaryData();
        public MapData Map = new MapData();
        public JobsData Jobs = new JobsData();
        
        #region IProfileChunk

        ushort ISerializedVersion.Version { get { return 1; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref Id);
            ioSerializer.Serialize("lastUpdated", ref LastUpdated, 0L);
            
            ioSerializer.Object("character", ref Character);
            ioSerializer.Object("inventory", ref Inventory);
            ioSerializer.Object("script", ref Script);
            ioSerializer.Object("bestiary", ref Bestiary);
            ioSerializer.Object("jobs", ref Jobs);
            ioSerializer.Object("map", ref Map);
        }

        public void MarkChangesPersisted()
        {
            Character.MarkChangesPersisted();
            Inventory.MarkChangesPersisted();
            Script.MarkChangesPersisted();
            Bestiary.MarkChangesPersisted();
            Map.MarkChangesPersisted();
            Jobs.MarkChangesPersisted();
        }

        public bool HasChanges()
        {
            return Character.HasChanges() || Inventory.HasChanges() || Script.HasChanges() || Bestiary.HasChanges() || Map.HasChanges() || Jobs.HasChanges();
        }

        #endregion // IProfileChunk
    }
}