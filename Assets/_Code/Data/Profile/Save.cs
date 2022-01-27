using System.Runtime.CompilerServices;
using Aqua.Option;
using Aqua.Profile;
using BeauUtil;

namespace Aqua {
    
    /// <summary>
    /// Save data access.
    /// </summary>
    static public class Save {

        #region Caching

        static private SaveData s_CachedProfile;
        static private OptionsData s_CachedOptions;

        static internal void DeclareProfile(SaveData profile) {
            s_CachedProfile = profile;
        }

        static internal void DeclareOptions(OptionsData options) {
            s_CachedOptions = options;
        }

        #endregion // Caching

        static public SaveData Current {
            [MethodImpl(256)] get { return s_CachedProfile; }
        }

        static public bool IsLoaded {
            [MethodImpl(256)] get { return Services.Data.IsProfileLoaded(); }
        }

        static public string Id {
            [MethodImpl(256)] get { return s_CachedProfile.Id; }
        }

        static public string Name {
            [MethodImpl(256)] get { return s_CachedProfile?.Character.DisplayName ?? Services.Data.DefaultCharacterName(); }
        }

        static public Pronouns Pronouns {
            [MethodImpl(256)] get { return s_CachedProfile?.Character.Pronouns ?? Pronouns.Neutral; }
        }

        #region Sections

        static public CharacterProfile Character {
            [MethodImpl(256)] get { return s_CachedProfile.Character; }
        }

        static public InventoryData Inventory {
            [MethodImpl(256)] get { return s_CachedProfile.Inventory; }
        }

        static public ScriptingData Script {
            [MethodImpl(256)] get { return s_CachedProfile.Script; }
        }

        static public BestiaryData Bestiary {
            [MethodImpl(256)] get { return s_CachedProfile.Bestiary; }
        }

        static public MapData Map {
            [MethodImpl(256)] get { return s_CachedProfile.Map; }
        }

        static public JobsData Jobs {
            [MethodImpl(256)] get { return s_CachedProfile.Jobs; }
        }

        static public OptionsData Options {
            [MethodImpl(256)] get { return s_CachedOptions; }
        }

        static public ScienceData Science {
            [MethodImpl(256)] get { return s_CachedProfile.Science; }
        }

        #endregion // Sections

        static public uint ActIndex {
            [MethodImpl(256)] get { return s_CachedProfile.Script.ActIndex; }
        }

        static public PlayerJob CurrentJob {
            [MethodImpl(256)] get { return s_CachedProfile.Jobs.CurrentJob; }
        }

        static public StringHash32 CurrentJobId {
            [MethodImpl(256)] get { return s_CachedProfile.Jobs.CurrentJobId; }
        }

        static public uint Cash {
            [MethodImpl(256)] get { return Inventory.ItemCount(ItemIds.Cash); }
        }

        static public uint Exp {
            [MethodImpl(256)] get { return Inventory.ItemCount(ItemIds.Exp); }
        }

        static public uint ExpLevel {
            [MethodImpl(256)] get { return Science.CurrentLevel(); }
        }
    }
}