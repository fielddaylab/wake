using System;
using Aqua.Option;
using BeauData;
using EasyBugReporter;

namespace Aqua.Profile
{
    public class SaveData : IProfileChunk, ISerializedVersion
    {
        public string Id;
        public long LastUpdated;
        public uint Version;
        public double Playtime;

        public CharacterProfile Character = new CharacterProfile();
        public InventoryData Inventory = new InventoryData();
        public ScriptingData Script = new ScriptingData();
        public BestiaryData Bestiary = new BestiaryData();
        public MapData Map = new MapData();
        public JobsData Jobs = new JobsData();
        public OptionsData Options = new OptionsData();
        public ScienceData Science = new ScienceData();

        public SaveData()
        {
            Options.SetDefaults(OptionsData.Authority.All);
            Version = SavePatcher.CurrentVersion;
        }
        
        #region IProfileChunk

        // v2: added options
        // v5: added playtime
        ushort ISerializedVersion.Version { get { return 5; } }

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            if (ioSerializer.ObjectVersion >= 4)
            {
                ioSerializer.Serialize("version", ref Version);
            }
            else
            {
                Version = 0;
            }

            ioSerializer.Serialize("id", ref Id);
            ioSerializer.Serialize("lastUpdated", ref LastUpdated, 0L);
            
            ioSerializer.Object("character", ref Character);
            ioSerializer.Object("inventory", ref Inventory);
            ioSerializer.Object("script", ref Script);
            ioSerializer.Object("bestiary", ref Bestiary);
            ioSerializer.Object("jobs", ref Jobs);
            ioSerializer.Object("map", ref Map);
            
            if (ioSerializer.ObjectVersion >= 2)
                ioSerializer.Object("options", ref Options);

            if (ioSerializer.ObjectVersion >= 3)
                ioSerializer.Object("science", ref Science);

            if (ioSerializer.ObjectVersion >= 5)
                ioSerializer.Serialize("playtime", ref Playtime);
        }

        public void MarkChangesPersisted()
        {
            Character.MarkChangesPersisted();
            Inventory.MarkChangesPersisted();
            Script.MarkChangesPersisted();
            Bestiary.MarkChangesPersisted();
            Map.MarkChangesPersisted();
            Jobs.MarkChangesPersisted();
            Options.MarkChangesPersisted();
            Science.MarkChangesPersisted();
        }

        public bool HasChanges()
        {
            return Character.HasChanges() || Inventory.HasChanges()
                || Script.HasChanges() || Bestiary.HasChanges()
                || Map.HasChanges() || Jobs.HasChanges()
                || Science.HasChanges();
        }

        public void Dump(EasyBugReporter.IDumpWriter writer) {
            writer.KeyValue("Save Id", Id);
            writer.KeyValue("Last Updated", DateTime.FromFileTimeUtc(LastUpdated));
            writer.KeyValue("Save Version", Version);
            writer.Space();

            writer.BeginSection("Character", false);
            Character.Dump(writer);
            writer.EndSection();

            writer.BeginSection("Inventory", false);
            Inventory.Dump(writer);
            writer.EndSection();

            writer.BeginSection("Scripting", false);
            Script.Dump(writer);
            writer.EndSection();

            writer.BeginSection("Bestiary", false);
            Bestiary.Dump(writer);
            writer.EndSection();

            writer.BeginSection("Map", false);
            Map.Dump(writer);
            writer.EndSection();

            writer.BeginSection("Jobs", false);
            Jobs.Dump(writer);
            writer.EndSection();

            writer.BeginSection("Science", false);
            Science.Dump(writer);
            writer.EndSection();
        }

        #endregion // IProfileChunk
    }
}