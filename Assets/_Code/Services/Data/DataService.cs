#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using Aqua.Debugging;
using Aqua.Profile;
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

        [Header("Random Chances")]

        [SerializeField, Range(0, 1)] private float m_CommonChance = 0.5f;
        [SerializeField, Range(0, 1)] private float m_UncommonChance = 0.3f;
        [SerializeField, Range(0, 1)] private float m_RareChance = 0.1f;

        #endregion // Inspector

        [NonSerialized] private SaveData m_CurrentSaveData;
        [NonSerialized] private VariantTable m_SessionTable;
        [NonSerialized] private string m_UserCode;

        [NonSerialized] private CustomVariantResolver m_VariableResolver;
        [NonSerialized] private RingBuffer<DialogRecord> m_DialogHistory;
        [NonSerialized] private bool m_PostLoadQueued;

        [NonSerialized] private Routine m_SaveRoutine;

        #if UNITY_EDITOR
        [NonSerialized] private DMInfo m_BookmarksMenu;
        #endif // UNITY_EDITOR

        #region Save Data

        public SaveData Profile
        {
            get { return m_CurrentSaveData; }
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
            if (inUserCode != null && TryLoadProfileFromPrefs(inUserCode, out saveData))
            {
                DebugService.Log(LogMask.DataService, "[DataService] Loaded profile with user id '{0}'", inUserCode);
                m_UserCode = inUserCode;
            }
            else
            {
                DebugService.Log(LogMask.DataService, "[DataService] Created new profile with user id '{0}'", inUserCode);
                m_UserCode = inUserCode;
                saveData = CreateNewProfile();
            }

            DeclareProfile(saveData);
        }

        #if DEVELOPMENT

        private void LoadBookmark(string inBookmarkName)
        {
            TextAsset bookmarkAsset = Resources.Load<TextAsset>("Bookmarks/" + inBookmarkName);
            if (!bookmarkAsset)
                return;

            SaveData bookmark;
            if (TryLoadProfileFromBytes(bookmarkAsset.bytes, out bookmark))
            {
                DebugService.Log(LogMask.DataService, "[DataService] Loaded profile from bookmark '{0}'", inBookmarkName);
                m_UserCode = null;

                DeclareProfile(bookmark);

                Services.UI.HideAll();
                Services.Script.KillAllThreads();
                Services.Audio.StopAll();
                Services.State.LoadScene("Ship");
            }
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

        private void DeclareProfile(SaveData inProfile)
        {
            m_CurrentSaveData = inProfile;
            HookSaveDataToVariableResolver(inProfile);
            Services.Events.Dispatch(GameEvents.ProfileLoaded);
            m_PostLoadQueued = true;
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
        }

        public IEnumerator SaveProfile()
        {
            return SaveProfile(false);
        }

        public IEnumerator SaveProfile(bool inbForce)
        {
            if (m_CurrentSaveData == null)
            {
                Debug.LogErrorFormat("[DataService] No data to save!");
                return null;
            }

            if (!m_CurrentSaveData.HasChanges())
            {
                DebugService.Log(LogMask.DataService, "[DataService] No changes detected to save");
                return null;
            }

            if (m_SaveRoutine)
            {
                if (inbForce)
                {
                    m_SaveRoutine.Stop();
                    Debug.LogWarningFormat("[DataService] Interrupting in-progress save");
                }
                else
                {
                    Debug.LogErrorFormat("[DataService] Save is already in progress");
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

        #region IService

        protected override void Initialize()
        {
            InitVariableResolver();

            m_SessionTable = new VariantTable("session");
            BindTable("session", m_SessionTable);

            Services.Events.Register(GameEvents.SceneLoaded, PerformPostLoad, this);

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

            // save data menu

            DMInfo saveMenu = DebugService.NewDebugMenu("Player Profile");

            DMInfo bookmarkMenu = DebugService.NewDebugMenu("Bookmarks");
            #if UNITY_EDITOR
            m_BookmarksMenu = bookmarkMenu;
            #endif // UNITY_EDITOR

            RegenerateBookmarks(bookmarkMenu);
            saveMenu.AddSubmenu(bookmarkMenu);
            saveMenu.AddDivider();

            saveMenu.AddButton("Save", () => SaveProfile(), () => m_CurrentSaveData != null);
            saveMenu.AddButton("Save (Debug)", () => DebugSaveData(), () => m_CurrentSaveData != null);
            #if UNITY_EDITOR
            saveMenu.AddButton("Save as Bookmark", () => BookmarkSaveData(), () => m_CurrentSaveData != null);
            #else 
            saveMenu.AddButton("Save as Bookmark", null, () => false);
            #endif // UNITY_EDITOR

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

        static private void ClearLocalSaves()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.LogWarningFormat("[DataService] All local save data has been cleared");
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