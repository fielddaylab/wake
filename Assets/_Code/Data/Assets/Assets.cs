using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    static public class Assets
    {
        static private BestiaryDB BestiaryDB;
        static private ScriptCharacterDB CharacterDB;
        static private InventoryDB InventoryDB;
        static private JobDB JobDB;
        static private ActDB ActDB;
        static private MapDB MapDB;
        static private WaterPropertyDB WaterPropertyDB;

        static private Dictionary<StringHash32, ScriptableObject> s_GlobalLookup;

        static internal void Assign(AssetsService inService)
        {
            BestiaryDB = inService.Bestiary;
            CharacterDB = inService.Characters;
            InventoryDB = inService.Inventory;
            JobDB = inService.Jobs;
            ActDB = inService.Acts;
            MapDB = inService.Map;
            WaterPropertyDB = inService.WaterProp;

            s_GlobalLookup = new Dictionary<StringHash32, ScriptableObject>(512);

            Import(BestiaryDB);
            Import(CharacterDB);
            Import(InventoryDB);
            Import(JobDB);
            Import(ActDB);
            Import(MapDB);
            Import(WaterPropertyDB);

            foreach(var fact in BestiaryDB.AllFacts())
                s_GlobalLookup.Add(fact.Id, fact);

            Log.Msg("[Assets] Imported {0} assets", s_GlobalLookup.Count);
        }

        static private void Import<T>(DBObjectCollection<T> inCollection) where T : DBObject
        {
            foreach(var obj in inCollection.Objects)
            {
                s_GlobalLookup.Add(obj.Id(), obj);
            }
        }

        static public ScriptableObject Find(StringHash32 inId)
        {
            if (inId.IsEmpty)
                return null;

            ScriptableObject obj;
            Assert.True(s_GlobalLookup.ContainsKey(inId), "No asset with id '{0}'", inId);
            s_GlobalLookup.TryGetValue(inId, out obj);
            return obj;
        }

        [MethodImpl(256)]
        static public T Find<T>(StringHash32 inId) where T : ScriptableObject
        {
            return (T) Find(inId);
        }

        [MethodImpl(256)]
        static public BestiaryDesc Bestiary(StringHash32 inId)
        {
            return BestiaryDB.Get(inId);
        }

        [MethodImpl(256)]
        static public BFBase Fact(StringHash32 inId)
        {
            return BestiaryDB.Fact(inId);
        }

        [MethodImpl(256)]
        static public T Fact<T>(StringHash32 inId) where T : BFBase
        {
            return BestiaryDB.Fact<T>(inId);
        }

        [MethodImpl(256)]
        static public ScriptCharacterDef Character(StringHash32 inId)
        {
            return CharacterDB.Get(inId);
        }

        [MethodImpl(256)]
        static public InvItem Item(StringHash32 inId)
        {
            return InventoryDB.Get(inId);
        }

        [MethodImpl(256)]
        static public JobDesc Job(StringHash32 inId)
        {
            return JobDB.Get(inId);
        }

        [MethodImpl(256)]
        static public ActDesc Act(uint inActIndex)
        {
            return ActDB.Act(inActIndex);
        }

        [MethodImpl(256)]
        static public MapDesc Map(StringHash32 inId)
        {
            return MapDB.Get(inId);
        }

        [MethodImpl(256)]
        static public WaterPropertyDesc Property(WaterPropertyId inProperty)
        {
            return WaterPropertyDB.Property(inProperty);
        }
    }
}