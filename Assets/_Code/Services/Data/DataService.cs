#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using Aqua.Debugging;
using Aqua.Profile;
using Aqua.Scripting;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using BeauUtil.Variants;
using System;
using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Aqua.Option;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua
{
    [ServiceDependency(typeof(AssetsService), typeof(EventService))]
    public partial class DataService : ServiceBehaviour, IDebuggable, ILoadable
    {
        #region Inspector

        [Header("Conversation History")]
        [SerializeField, Range(32, 256)] private int m_DialogHistorySize = 128;

        [Header("Defaults")]
        [SerializeField] private string m_DefaultPlayerDisplayName = "Unknown Player";
        [SerializeField] private SerializedHash32 m_DefaultMapId = "Ship";

        [Header("Random Chances")]

        [SerializeField, Range(0, 1)] private float m_CommonChance = 0.5f;
        [SerializeField, Range(0, 1)] private float m_UncommonChance = 0.3f;
        [SerializeField, Range(0, 1)] private float m_RareChance = 0.1f;

        #endregion // Inspector

        [NonSerialized] private SaveData m_CurrentSaveData;
        [NonSerialized] private OptionsData m_CurrentOptions;
        [NonSerialized] private VariantTable m_SessionTable;
        [NonSerialized] private string m_UserCode;

        [NonSerialized] private CustomVariantResolver m_VariableResolver;
        [NonSerialized] private RingBuffer<DialogRecord> m_DialogHistory;
        [NonSerialized] private bool m_PostLoadQueued;

        [NonSerialized] private Routine m_SaveRoutine;

        #if UNITY_EDITOR
        [NonSerialized] private DMInfo m_BookmarksMenu;
        #endif // UNITY_EDITOR

        [NonSerialized] private bool m_AutoSaveEnabled;

        #region Save Data

        public SaveData Profile
        {
            get { return m_CurrentSaveData; }
        }

        public OptionsData Options
        {
            get { return m_CurrentOptions; }
        }

        public string CurrentCharacterName()
        {
            return m_CurrentSaveData?.Character?.DisplayName ?? m_DefaultPlayerDisplayName;
        }

        public Pronouns CurrentCharacterPronouns()
        {
            CharacterProfile profile = m_CurrentSaveData?.Character;
            return profile != null ? profile.Pronouns : Pronouns.Neutral;
        }

        public StringHash32 CurrentJobId()
        {
            return m_CurrentSaveData?.Jobs?.CurrentJobId ?? StringHash32.Null;
        }

        public PlayerJob CurrentJob()
        {
            return m_CurrentSaveData?.Jobs?.CurrentJob;
        }

        public uint CurrentAct()
        {
            return m_CurrentSaveData?.Script?.ActIndex ?? 0;
        }

        #endregion // Save Data

        #region Loading

        public bool IsProfileLoaded()
        {
            return m_CurrentSaveData != null;
        }

        public void LoadProfile(string inUserCode)
        {
            ClearOldProfile();

            SaveData saveData;
            if (TryLoadProfileFromPrefs(inUserCode, out saveData))
            {
                DebugService.Log(LogMask.DataService, "[DataService] Loaded profile with user id '{0}'", inUserCode);
                m_UserCode = inUserCode;
            }
            else
            {
                DebugService.Log(LogMask.DataService, "[DataService] Created new profile with user id '{0}'", inUserCode);
                m_UserCode = inUserCode ?? string.Empty;
                saveData = CreateNewProfile();
            }

            DeclareProfile(saveData, true);
        }

        public void StartPlaying()
        {
            StartPlaying(false);
        }

        private void StartPlaying(bool inbHardReset)
        {
            Services.Audio.StopMusic();
            Services.Script.KillAllThreads();

            if (inbHardReset)
            {
                Services.Audio.StopAll();
                Services.UI.HideAll();
            }

            StringHash32 mapId = FindMapId(m_CurrentSaveData);
            Services.State.ClearSceneHistory();
            StateUtil.LoadMapWithWipe(mapId);
            AutoSave.Suppress();
        }

        #if DEVELOPMENT

        internal void UseDebugProfile()
        {
            ClearOldProfile();
            m_CurrentOptions.SetDefaults();

            m_UserCode = string.Empty;
            SaveData saveData = CreateNewProfile();
            DebugService.Log(LogMask.DataService, "[DataService] Created debug profile");
            DeclareProfile(saveData, false);
        }

        private void LoadBookmark(string inBookmarkName)
        {
            TextAsset bookmarkAsset = Resources.Load<TextAsset>("Bookmarks/" + inBookmarkName);
            if (!bookmarkAsset)
                return;

            SaveData bookmark;
            if (TryLoadProfileFromBytes(bookmarkAsset.bytes, out bookmark))
            {
                ClearOldProfile();

                DebugService.Log(LogMask.DataService, "[DataService] Loaded profile from bookmark '{0}'", inBookmarkName);
                m_UserCode = null;

                DeclareProfile(bookmark, false);
                StartPlaying(true);
            }
        }

        private void ForceReloadSave()
        {
            LoadProfile(m_UserCode);
            StartPlaying(true);
        }

        private void ForceRestart()
        {
            DeleteSave(m_UserCode);
            LoadProfile(m_UserCode);
            StartPlaying(true);
        }

        #endif // DEVELOPMENT

        private void ClearOldProfile()
        {
            m_DialogHistory.Clear();
            m_SessionTable.Clear();

            m_CurrentSaveData = null;
        }

        private SaveData CreateNewProfile()
        {
            SaveData data = new SaveData();
            data.Id = Guid.NewGuid().ToString();
            data.Map.SetDefaults();
            data.Inventory.SetDefaults();
            return data;
        }

        private bool TryLoadProfileFromPrefs(string inUserCode, out SaveData outProfile)
        {
            outProfile = null;

            string key = GetPrefsKeyForCode(inUserCode);
            if (PlayerPrefs.HasKey(key))
            {
                return Serializer.ReadPrefs(ref outProfile, key);
            }

            return false;
        }

        private bool TryLoadProfileFromBytes(byte[] inBytes, out SaveData outProfile)
        {
            outProfile = null;
            return Serializer.Read(ref outProfile, inBytes);
        }

        private void DeclareProfile(SaveData inProfile, bool inbAutoSave)
        {
            m_CurrentSaveData = inProfile;
            OptionsData.SyncFrom(m_CurrentSaveData.Options, m_CurrentOptions, OptionsData.Authority.Profile);

            HookSaveDataToVariableResolver(inProfile);
            Services.Events.Dispatch(GameEvents.ProfileLoaded);
            
            SetAutosaveEnabled(inbAutoSave);
            m_PostLoadQueued = true;
        }

        private StringHash32 FindMapId(SaveData inSaveData)
        {
            StringHash32 mapId = inSaveData.Map.SavedSceneId();
            if (mapId.IsEmpty)
                return m_DefaultMapId;
            return mapId;
        }

        private void PerformPostLoad()
        {
            if (!m_PostLoadQueued)
                return;

            m_CurrentSaveData.Jobs.PostLoad();
            m_PostLoadQueued = false;
        }

        #endregion // Loading

        #region Saving

        public bool IsSaving()
        {
            return m_SaveRoutine;
        }

        private void SyncProfile()
        {
            m_CurrentSaveData.LastUpdated = DateTime.UtcNow.ToFileTime();
            OptionsData.SyncFrom(m_CurrentOptions, m_CurrentSaveData.Options, OptionsData.Authority.All);
        }

        public bool NeedsSave()
        {
            return m_CurrentSaveData != null && (m_CurrentSaveData.HasChanges() || m_CurrentOptions.HasChanges());
        }

        public IEnumerator SaveProfile()
        {
            return SaveProfile(false);
        }

        public IEnumerator SaveProfile(bool inbForce)
        {
            if (m_CurrentSaveData == null)
            {
                Log.Error("[DataService] No data to save!");
                return null;
            }

            if (!m_CurrentSaveData.HasChanges() && !m_CurrentOptions.HasChanges())
            {
                DebugService.Log(LogMask.DataService, "[DataService] No changes detected to save");
                return null;
            }

            if (m_SaveRoutine)
            {
                if (inbForce)
                {
                    m_SaveRoutine.Stop();
                    Log.Warn("[DataService] Interrupting in-progress save");
                }
                else
                {
                    Log.Error("[DataService] Save is already in progress");
                    return m_SaveRoutine.Wait();
                }
            }

            m_SaveRoutine.Replace(this, SaveRoutine()).TryManuallyUpdate(0);
            return m_SaveRoutine.Wait();
        }

        private IEnumerator SaveRoutine()
        {
            SyncProfile();
            yield return null;

            string key = GetPrefsKeyForCode(m_UserCode);
            Services.Events.Dispatch(GameEvents.ProfileSaveBegin);

            DebugService.Log(LogMask.DataService, "[DataService] Saving to '{0}'...", key);

            Serializer.WritePrefs(m_CurrentSaveData, key, OutputOptions.None, Serializer.Format.Binary);
            yield return null;
            
            PlayerPrefs.Save();
            yield return null;
            
            DebugService.Log(LogMask.DataService, "[DataService] ...finished saving to '{0}'", key);
            m_CurrentSaveData.MarkChangesPersisted();
            Services.Events.Dispatch(GameEvents.ProfileSaveCompleted);
        }

        private void DebugSaveData()
        {
            SyncProfile();

            #if UNITY_EDITOR

            Directory.CreateDirectory("Saves");
            string saveName = string.Format("Saves/save_{0}.json", m_CurrentSaveData.LastUpdated);
            string binarySaveName = string.Format("Saves/save_{0}.bbin", m_CurrentSaveData.LastUpdated);
            Serializer.WriteFile(m_CurrentSaveData, saveName, OutputOptions.PrettyPrint, Serializer.Format.JSON);
            Serializer.WriteFile(m_CurrentSaveData, binarySaveName, OutputOptions.None, Serializer.Format.Binary);
            Debug.LogFormat("[DataService] Saved Profile to {0} and {1}", saveName, binarySaveName);
            EditorUtility.OpenWithDefaultApp(saveName);

            #elif DEVELOPMENT_BUILD

            string json = Serializer.Write(m_CurrentSaveData, OutputOptions.None, Serializer.Format.JSON);
            Debug.LogFormat("[DataService] Current Profile: {0}", json);

            #endif // UNITY_EDITOR
        }

        private void DeleteSave(string inUserCode)
        {
            PlayerPrefs.DeleteKey(GetPrefsKeyForCode(inUserCode));
            PlayerPrefs.Save();
            Log.Warn("[DataService] Local save data for user id '{0}' has been cleared", inUserCode);
        }

        public bool AutosaveEnabled()
        {
            return m_AutoSaveEnabled;
        }

        public void SetAutosaveEnabled(bool inbEnabled)
        {
            if (m_AutoSaveEnabled != inbEnabled)
            {
                m_AutoSaveEnabled = inbEnabled;
                if (inbEnabled)
                    AutoSave.Force();
            }
        }

        #if UNITY_EDITOR

        private void BookmarkSaveData()
        {
            SyncProfile();

            Cursor.visible = true;
            
            string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Save Bookmark", string.Empty, "json", "Choose a location to save your bookmark", "Assets/Resources/Bookmarks/");
            if (!string.IsNullOrEmpty(path))
            {
                Serializer.WriteFile(m_CurrentSaveData, path, OutputOptions.PrettyPrint, Serializer.Format.JSON);
                Debug.LogFormat("[DataService] Saved bookmark at {0}", path);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                RegenerateBookmarks(m_BookmarksMenu);
            }

            Cursor.visible = false;
        }

        #endif // UNITY_EDITOR

        static private string GetPrefsKeyForCode(string inUserCode)
        {
            if (string.IsNullOrEmpty(inUserCode))
            {
                return string.Format("aqualab/profiles/debug/profile");
            }

            return string.Format("aqualab/profiles/{0}", inUserCode.Replace("/", "_"));
        }

        #endregion // Saving

        #region Dialog History

        public void AddToDialogHistory(in DialogRecord inRecord)
        {
            m_DialogHistory.PushBack(inRecord);
        }

        public RingBuffer<DialogRecord> DialogHistory
        {
            get { return m_DialogHistory; }
        }

        #endregion // Dialog History

        #region Options

        public bool IsOptionsLoaded()
        {
            return m_CurrentOptions != null;
        }

        public void LoadOptionsSettings() 
        {
            m_CurrentOptions = new OptionsData();
        }

        #endregion // Options

        #region IService

        protected override void Initialize()
        {
            InitVariableResolver();

            m_SessionTable = new VariantTable("session");
            BindTable("session", m_SessionTable);

            Services.Events.Register(GameEvents.SceneLoaded, PerformPostLoad, this);

            m_CurrentOptions = new OptionsData();
            m_CurrentOptions.SetDefaults();

            m_DialogHistory = new RingBuffer<DialogRecord>(m_DialogHistorySize, RingBufferMode.Overwrite);
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);
            m_SaveRoutine.Stop();
        }

        #endregion // IService

        #region IDebuggable

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus()
        {
            // jobs menu

            DMInfo jobsMenu = DebugService.NewDebugMenu("Jobs");

            DMInfo startJobMenu = DebugService.NewDebugMenu("Start Job");
            foreach(var job in Services.Assets.Jobs.Objects)
                RegisterJobStart(startJobMenu, job.Id());

            jobsMenu.AddSubmenu(startJobMenu);
            jobsMenu.AddDivider();
            jobsMenu.AddButton("Complete Current Job", () => Services.Data.Profile.Jobs.MarkComplete(Services.Data.CurrentJob()), () => !Services.Data.CurrentJobId().IsEmpty);
            jobsMenu.AddButton("Clear All Job Progress", () => Services.Data.Profile.Jobs.ClearAll());

            yield return jobsMenu;

            // bestiary menu

            DMInfo bestiaryMenu = DebugService.NewDebugMenu("Bestiary");

            DMInfo critterMenu = DebugService.NewDebugMenu("Critters");
            foreach(var critter in Services.Assets.Bestiary.AllEntriesForCategory(BestiaryDescCategory.Critter))
                RegisterEntityToggle(critterMenu, critter.Id());

            DMInfo envMenu = DebugService.NewDebugMenu("Environments");
            foreach(var env in Services.Assets.Bestiary.AllEntriesForCategory(BestiaryDescCategory.Environment))
                RegisterEntityToggle(envMenu, env.Id());

            DMInfo factMenu = DebugService.NewDebugMenu("Facts");
            Dictionary<StringHash32, DMInfo> factSubmenus = new Dictionary<StringHash32, DMInfo>();
            foreach(var fact in Services.Assets.Bestiary.AllFacts())
            {
                if (Services.Assets.Bestiary.IsAutoFact(fact.Id()))
                    continue;

                DMInfo submenu;
                StringHash32 submenuKey = fact.Parent().Id();
                if (!factSubmenus.TryGetValue(submenuKey, out submenu))
                {
                    submenu = DebugService.NewDebugMenu(submenuKey.ToDebugString());
                    factSubmenus.Add(submenuKey, submenu);
                    factMenu.AddSubmenu(submenu);
                }

                RegisterFactToggle(submenu, fact.Id());
            }

            bestiaryMenu.AddSubmenu(critterMenu);
            bestiaryMenu.AddSubmenu(envMenu);
            bestiaryMenu.AddSubmenu(factMenu);

            bestiaryMenu.AddDivider();

            bestiaryMenu.AddButton("Unlock All Entries", () => UnlockAllBestiaryEntries(false));
            bestiaryMenu.AddButton("Unlock All Facts", () => UnlockAllBestiaryEntries(true));
            bestiaryMenu.AddButton("Clear Bestiary", () => ClearBestiary());

            yield return bestiaryMenu;

            // map menu

            DMInfo mapMenu = DebugService.NewDebugMenu("World Map");

            foreach(var map in Services.Assets.Map.Stations())
            {
                RegisterStationToggle(mapMenu, map.Id());
            }

            mapMenu.AddDivider();

            mapMenu.AddButton("Unlock All Stations", () => UnlockAllStations());

            yield return mapMenu;

            // save data menu

            DMInfo saveMenu = DebugService.NewDebugMenu("Player Profile");

            DMInfo bookmarkMenu = DebugService.NewDebugMenu("Bookmarks");
            #if UNITY_EDITOR
            m_BookmarksMenu = bookmarkMenu;
            #endif // UNITY_EDITOR

            RegenerateBookmarks(bookmarkMenu);
            saveMenu.AddSubmenu(bookmarkMenu);
            saveMenu.AddDivider();

            saveMenu.AddButton("Save", () => SaveProfile(), IsProfileLoaded);
            saveMenu.AddButton("Save (Debug)", () => DebugSaveData(), IsProfileLoaded);
            #if UNITY_EDITOR
            saveMenu.AddButton("Save as Bookmark", () => BookmarkSaveData(), IsProfileLoaded);
            #else 
            saveMenu.AddButton("Save as Bookmark", null, () => false);
            #endif // UNITY_EDITOR
            saveMenu.AddToggle("Autosave Enabled", AutosaveEnabled, SetAutosaveEnabled);

            saveMenu.AddDivider();

            #if DEVELOPMENT
            saveMenu.AddButton("Reload Save", () => ForceReloadSave(), IsProfileLoaded);
            saveMenu.AddButton("Restart from Beginning", () => ForceRestart());
            #endif // DEVELOPMENT

            saveMenu.AddDivider();

            saveMenu.AddButton("Clear Local Saves", () => ClearLocalSaves());

            yield return saveMenu;
        }

        static private void RegenerateBookmarks(DMInfo inMenu)
        {
            var allBookmarks = Resources.LoadAll<TextAsset>("Bookmarks");

            inMenu.Clear();

            if (allBookmarks.Length == 0)
            {
                inMenu.AddText("No bookmarks :(", () => string.Empty);
            }
            else if (allBookmarks.Length <= 10)
            {
                foreach(var bookmark in allBookmarks)
                {
                    RegisterBookmark(inMenu, bookmark);
                }
            }
            else
            {
                int pageNumber = 0;
                int bookmarkCounter = 0;
                DMInfo page = new DMInfo("Page 1", 10);
                inMenu.AddSubmenu(page);
                foreach(var bookmark in allBookmarks)
                {
                    if (bookmarkCounter >= 10)
                    {
                        pageNumber++;
                        page = new DMInfo("Page " + pageNumber.ToString(), 10);
                        inMenu.AddSubmenu(page);
                    }

                    RegisterBookmark(page, bookmark);

                    bookmarkCounter++;
                }
            }
        }

        static private void RegisterBookmark(DMInfo inMenu, TextAsset inAsset)
        {
            #if DEVELOPMENT
            string name = inAsset.name;
            inMenu.AddButton(name, () => Services.Data.LoadBookmark(name), () => !Services.Data.m_SaveRoutine);
            #endif // DEVELOPMENT
            
            Resources.UnloadAsset(inAsset);
        }

        static private void RegisterJobStart(DMInfo inMenu, StringHash32 inJobId)
        {
            inMenu.AddButton(inJobId.ToDebugString(), () => 
            {
                Services.Data.Profile.Jobs.ForgetJob(inJobId);
                Services.Data.Profile.Jobs.SetCurrentJob(inJobId); 
            }, () => Services.Data.CurrentJobId() != inJobId);
        }

        static private void RegisterEntityToggle(DMInfo inMenu, StringHash32 inEntityId)
        {
            inMenu.AddToggle(inEntityId.ToDebugString(),
                () => { return Services.Data.Profile.Bestiary.HasEntity(inEntityId); },
                (b) =>
                {
                    if (b)
                        Services.Data.Profile.Bestiary.RegisterEntity(inEntityId);
                    else
                        Services.Data.Profile.Bestiary.DeregisterEntity(inEntityId);
                }
            );
        }

        static private void RegisterFactToggle(DMInfo inMenu, StringHash32 inFactId)
        {
            inMenu.AddToggle(inFactId.ToDebugString(),
                () => { return Services.Data.Profile.Bestiary.HasFact(inFactId); },
                (b) =>
                {
                    if (b)
                        Services.Data.Profile.Bestiary.RegisterFact(inFactId);
                    else
                        Services.Data.Profile.Bestiary.DeregisterFact(inFactId);
                }
            );
        }


        static private void UnlockAllBestiaryEntries(bool inbIncludeFacts)
        {
            foreach(var entry in Services.Assets.Bestiary.Objects)
            {
                Services.Data.Profile.Bestiary.RegisterEntity(entry.Id());
                if (inbIncludeFacts)
                {
                    foreach(var fact in entry.Facts)
                        Services.Data.Profile.Bestiary.RegisterFact(fact.Id());
                }
            }
        }

        static private void ClearBestiary()
        {
            foreach(var entry in Services.Assets.Bestiary.Objects)
            {
                Services.Data.Profile.Bestiary.DeregisterEntity(entry.Id());
                foreach(var fact in entry.Facts)
                    Services.Data.Profile.Bestiary.DeregisterFact(fact.Id());
            }
        }

        static private void RegisterStationToggle(DMInfo inMenu, StringHash32 inStationId)
        {
            inMenu.AddToggle(inStationId.ToDebugString(),
                () => { return Services.Data.Profile.Map.IsStationUnlocked(inStationId); },
                (b) =>
                {
                    if (b)
                        Services.Data.Profile.Map.UnlockStation(inStationId);
                    else
                        Services.Data.Profile.Map.LockStation(inStationId);
                }
            );
        }

        static private void UnlockAllStations()
        {
            foreach(var map in Services.Assets.Map.Stations())
            {
                Services.Data.Profile.Map.UnlockStation(map.Id());
            }
        }

        static private void ClearLocalSaves()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Log.Warn("[DataService] All local save data has been cleared");
        }

        #endregion // IDebuggable

        #region ILoadable

        bool ILoadable.IsLoading()
        {
            return m_SaveRoutine;
        }

        #endregion // ILoadable
    }
}