using System.Runtime.CompilerServices;
using BeauUtil;

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

        static internal void Assign(AssetsService inService)
        {
            BestiaryDB = inService.Bestiary;
            CharacterDB = inService.Characters;
            InventoryDB = inService.Inventory;
            JobDB = inService.Jobs;
            ActDB = inService.Acts;
            MapDB = inService.Map;
            WaterPropertyDB = inService.WaterProp;
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