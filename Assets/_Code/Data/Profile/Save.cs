using System.Runtime.CompilerServices;
using Aqua.Option;
using Aqua.Profile;

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

        static public string Id {
            [MethodImpl(256)] get { return s_CachedProfile.Id; }
        }

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
    }
}