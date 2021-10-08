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
using Aqua.Option;
using UnityEngine;

namespace Aqua
{
    [ServiceDependency(typeof(AssetsService), typeof(EventService))]
    public partial class DataService : ServiceBehaviour, IDebuggable, ILoadable
    {
        private const string LocalSettingsPrefsKey = "settings/local";
        private const string LastUserNameKey = "settings/last-known-profile";

        #if DEVELOPMENT
        private const string DebugSaveId = "__DEBUG";
        #endif // DEVELOPMENT

        private const string GameId = "AQUALAB";

        #region Inspector

        [SerializeField] private string m_ServerAddress = null;

        [Header("Conversation History")]
        [SerializeField, Range(32, 256)] private int m_DialogHistorySize = 128;

        [Header("Defaults")]
        [SerializeField] private string m_DefaultPlayerDisplayName = "Unknown Player";
        [SerializeField, MapId] private SerializedHash32 m_DefaultMapId = "Ship";

        [Header("Random Chances")]

        [SerializeField, Range(0, 1)] private float m_CommonChance = 0.5f;
        [SerializeField, Range(0, 1)] private float m_UncommonChance = 0.3f;
        [SerializeField, Range(0, 1)] private float m_RareChance = 0.1f;

        #endregion // Inspector

        [NonSerialized] private SaveData m_CurrentSaveData;
        [NonSerialized] private OptionsData m_CurrentOptions;
        [NonSerialized] private VariantTable m_SessionTable;
        [NonSerialized] private string m_ProfileName;

        [NonSerialized] private CustomVariantResolver m_VariableResolver;
        [NonSerialized] private RingBuffer<DialogRecord> m_DialogHistory;
        [NonSerialized] private bool m_PostLoadQueued;

        [NonSerialized] private Future<bool> m_SaveResult;
        [NonSerialized] private bool m_AutoSaveEnabled;
        [NonSerialized] private string m_LastKnownProfile;
        
        #if DEVELOPMENT
        [NonSerialized] private bool m_IsDebugProfile;
        [NonSerialized] private string m_LastBookmarkName;
        #endif // DEVELOPMENT

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

        #if DEVELOPMENT
        private bool IsDebugProfile() { return m_IsDebugProfile; }
        #else
        private bool IsDebugProfile() { return false; }
        #endif // DEVELOPMENT

        #endregion // Save Data

        #region Loading

        public string LastProfileName() { return m_LastKnownProfile; }

        public bool IsProfileLoaded()
        {
            return m_CurrentSaveData != null;
        }

        public void UnloadProfile()
        {
            if (ClearOldProfile())
            {
                Services.Events.Dispatch(GameEvents.ProfileUnloaded);
            }
        }

        public Future<bool> NewProfile(string inUserCode)
        {
            if (string.IsNullOrEmpty(inUserCode))
            {
                Log.Warn("[DataService] Empty user code provided'");
                return Future.Failed<bool>();
            }

            ClearOldProfile();
            DeleteLocalSave(inUserCode);

            DeclareProfile(CreateNewProfile(inUserCode), true);
            if (!IsDebugProfile())
            {
                return Future.CreateLinked<bool>(DeclareProfileToServer, this);
            }
            else
            {
                return Future.Completed(true);
            }
        }

        public Future<bool> LoadProfile(string inUserCode)
        {
            if (string.IsNullOrEmpty(inUserCode))
            {
                Log.Warn("[DataService] Empty user code provided'");
                return Future.Failed<bool>();
            }

            if (IdentifyDebugProfile(ref inUserCode))
            {
                SaveData debugSave;
                if (TryLoadProfileFromPrefs(inUserCode, out debugSave))
                {
                    ClearOldProfile();

                    DebugService.Log(LogMask.DataService, "[DataService] Loaded debug profile with user id '{0}'", inUserCode);

                    DeclareProfile(debugSave, true);
                    return Future.Completed(true);
                }
                else
                {
                    DebugService.Log(LogMask.DataService, "[DataService] No debug profile with id {0}'", inUserCode);
                    return Future.Failed<bool>();
                }
            }
            
            return Future.CreateLinked<bool, string>(LoadProfileRoutine, inUserCode, this);
        }

        private IEnumerator LoadProfileRoutine(Future<bool> ioFuture, string inUserCode)
        {
            Future<SaveData> serverCheck = TryLoadProfileFromServer(inUserCode);
            yield return serverCheck;

            if (!serverCheck.IsComplete())
            {
                DebugService.Log(LogMask.DataService, "[DataService] No profile with id {0} available from server: {1}", inUserCode, serverCheck.GetFailure().Object);
                ioFuture.Fail(serverCheck.GetFailure());
                yield break;
            }

            SaveData serverSave = serverCheck.Get();
            SaveData authoritativeSave = serverSave;
            SaveData localSave;
            if (TryLoadProfileFromPrefs(inUserCode, out localSave))
            {
                DebugService.Log(LogMask.DataService, "[DataService] Local backup for save {0} found...", inUserCode);
                if (serverSave == null || localSave.LastUpdated > serverSave.LastUpdated)
                {
                    DebugService.Log(LogMask.DataService, "[DataService] Local backup is more recent!");
                    authoritativeSave = localSave;
                }
            }

            if (authoritativeSave == null)
            {
                DebugService.Log(LogMask.DataService, "[DataService] No save located for user id {0}", inUserCode);
                ioFuture.Fail();
                yield break;
            }

            ClearOldProfile();

            DebugService.Log(LogMask.DataService, "[DataService] Loaded profile with user id '{0}'", inUserCode);

            DeclareProfile(authoritativeSave, true);
            ioFuture.Complete(true);
        }

        public void StartPlaying()
        {
            StartPlaying(null, false);
        }

        public void StartPlaying(string inSceneOverride)
        {
            StartPlaying(inSceneOverride, false);
        }

        private void StartPlaying(string inSceneOverride, bool inbHardReset)
        {
            Assert.NotNull(m_CurrentSaveData, "No save data loaded!");

            Services.Audio.StopMusic();
            Services.Script.KillAllThreads();

            if (inbHardReset)
            {
                Services.Audio.StopAll();
                Services.UI.HideAll();
            }

            Services.State.ClearSceneHistory();
            Services.Events.Dispatch(GameEvents.ProfileStarting, m_ProfileName);
            
            if (string.IsNullOrEmpty(inSceneOverride))
            {
                StringHash32 mapId = FindMapId(m_CurrentSaveData);
                StateUtil.LoadMapWithWipe(mapId, m_CurrentSaveData.Map.SavedSceneLocationId());
            }
            else
            {
                StateUtil.LoadSceneWithWipe(inSceneOverride);
            }

            AutoSave.Suppress();
        }

        private bool ClearOldProfile()
        {
            m_DialogHistory.Clear();
            m_SessionTable.Clear();

            if (m_CurrentSaveData != null)
            {
                m_CurrentSaveData = null;
                return true;
            }

            return false;
        }

        private SaveData CreateNewProfile(string inId)
        {
            SaveData data = new SaveData();
            data.Id = inId;
            data.Map.SetDefaults();
            data.Inventory.SetDefaults();
            OptionsData.SyncFrom(m_CurrentOptions, data.Options, OptionsData.Authority.All);
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

        private Future<SaveData> TryLoadProfileFromServer(string inUserCode)
        {
            return Future.CreateLinked<SaveData, string>(TryLoadProfileFromServerRoutine, inUserCode, this);
        }

        private IEnumerator TryLoadProfileFromServerRoutine(Future<SaveData> ioFuture, string inUserCode)
        {
            using(var future = Future.Create<string>())
            using(var request = OGD.GameState.RequestLatestState(inUserCode, future.Complete, (r, s) => future.Fail(r)))
            {
                yield return future;

                if (future.IsComplete())
                {
                    DebugService.Log(LogMask.DataService, "[DataService] Save with profile name {0} found on server!", m_ProfileName);
                    SaveData serverData = null;
                    if (!Serializer.Read(ref serverData, future.Get()))
                    {
                        Log.Error("[DataService] Server profile could not be read...");
                        ioFuture.Complete(null);
                    }
                    else
                    {
                        ioFuture.Complete(serverData);
                    }
                }
                else
                {
                    Log.Error("[DataService] Failed to find profile on server: {0}", future.GetFailure().Object);
                    ioFuture.Fail(future.GetFailure());
                }
            }
        }

        private bool TryLoadProfileFromBytes(byte[] inBytes, out SaveData outProfile)
        {
            outProfile = null;
            return Serializer.Read(ref outProfile, inBytes);
        }

        private void DeclareProfile(SaveData inProfile, bool inbAutoSave)
        {
            m_CurrentSaveData = inProfile;
            
            #if DEVELOPMENT
            m_IsDebugProfile = IdentifyDebugProfile(ref inProfile.Id);
            m_LastBookmarkName = null;
            #endif // DEVELOPMENT

            m_ProfileName = inProfile.Id;

            uint oldVersion = inProfile.Version;
            if (SavePatcher.TryPatch(inProfile))
            {
                Log.Msg("[DataService] Patched save data from version {0} to {1}", oldVersion, SavePatcher.CurrentVersion);
            }

            if (!IsDebugProfile())
            {
                m_LastKnownProfile = inProfile.Id;
                PlayerPrefs.SetString(LastUserNameKey, m_LastKnownProfile ?? string.Empty);
                PlayerPrefs.Save();
            }

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
            LeafIntegration.ClearBatches();

            if (!m_PostLoadQueued)
                return;

            m_CurrentSaveData.Jobs.PostLoad();
            m_PostLoadQueued = false;
            
            Services.Events.Dispatch(GameEvents.ProfileStarted);
        }

        private IEnumerator DeclareProfileToServer(Future<bool> ioFuture)
        {
            var profileName = m_ProfileName;

            using(var future = Future.Create())
            using(var request = OGD.Player.ClaimId(m_ProfileName, null, future.Complete, (r, s) => future.Fail(r)))
            {
                yield return future;

                if (future.IsComplete())
                {
                    DebugService.Log(LogMask.DataService, "[DataService] Profile name {0} declared to server!", m_ProfileName);
                }
                else
                {
                    Log.Error("[DataService] Failed to declare name to server: {0}", future.GetFailure().Object);
                    ioFuture.Fail(future.GetFailure());
                }
            }

            // push an empty save up to the server

            string saveData = Serializer.Write(m_CurrentSaveData, OutputOptions.None, Serializer.Format.Binary);

            using(var future = Future.Create())
            using(var saveRequest = OGD.GameState.PushState(m_ProfileName, saveData, future.Complete, (r, s) => future.Fail(r)))
            {
                yield return future;

                if (future.IsComplete())
                {
                    DebugService.Log(LogMask.DataService, "[DataService] Saved to server!");
                    ioFuture.Complete(true);
                }
                else
                {
                    Log.Error("[DataService] Failed to save to server: {0}", future.GetFailure().Object);
                    ioFuture.Fail(future.GetFailure());
                }
            }
        }

        #if DEVELOPMENT

        static private bool IdentifyDebugProfile(ref string ioId)
        {
            if (ioId == DebugSaveId)
                return true;
                
            if (Guid.TryParse(ioId, out _))
            {
                ioId = DebugSaveId;
                return true;
            }

            return false;
        }

        #else

        static private bool IdentifyDebugProfile(ref string ioId) { return false; }

        #endif // DEVELOPMENT

        #endregion // Loading

        #region Saving

        public bool IsSaving()
        {
            return m_SaveResult != null;
        }

        private void SyncProfile()
        {
            m_CurrentSaveData.LastUpdated = DateTime.UtcNow.ToFileTime();
            OptionsData.SyncFrom(m_CurrentOptions, m_CurrentSaveData.Options, OptionsData.Authority.All);
            m_CurrentSaveData.Map.SyncTime();
        }

        public bool NeedsSave()
        {
            return m_CurrentSaveData != null && (m_CurrentSaveData.HasChanges() || m_CurrentOptions.HasChanges());
        }

        public Future<bool> SaveProfile(StringHash32 inLocationId)
        {
            return SaveProfile(inLocationId, false);
        }

        public Future<bool> SaveProfile(StringHash32 inLocationId, bool inbForce)
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

            if (m_SaveResult != null)
            {
                if (inbForce)
                {
                    m_SaveResult.Cancel();
                    Log.Warn("[DataService] Interrupting in-progress save");
                }
                else
                {
                    Log.Error("[DataService] Save is already in progress");
                    return m_SaveResult;
                }
            }

            m_SaveResult = Future.CreateLinked<bool, StringHash32>(SaveRoutine, inLocationId, this);
            return m_SaveResult;
        }

        private IEnumerator SaveRoutine(Future<bool> ioFuture, StringHash32 inLocationId)
        {
            SyncProfile();
            m_CurrentSaveData.Map.SetEntranceId(inLocationId);

            DebugService.Log(LogMask.DataService, "[DataService] Saving profile '{0}'...", m_ProfileName);

            string key = GetPrefsKeyForCode(m_ProfileName);
            Services.Events.Dispatch(GameEvents.ProfileSaveBegin);

            DebugService.Log(LogMask.DataService, "[DataService] Local backup at  '{0}'...", key);

            string saveData = Serializer.Write(m_CurrentSaveData, OutputOptions.None, Serializer.Format.Binary);
            m_CurrentSaveData.MarkChangesPersisted();

            yield return null;

            PlayerPrefs.SetString(key, saveData);
            yield return null;
            
            PlayerPrefs.Save();
            yield return null;

            if (!IsDebugProfile())
            {
                using(var future = Future.Create())
                using(var saveRequest = OGD.GameState.PushState(m_ProfileName, saveData, future.Complete, (r, s) => future.Fail(r)))
                {
                    yield return future;

                    if (future.IsComplete())
                    {
                        DebugService.Log(LogMask.DataService, "[DataService] Saved to server!");
                    }
                    else
                    {
                        Log.Warn("[DataService] Failed to save to server: {0}", future.GetFailure().Object);
                    }
                }
            }
            
            DebugService.Log(LogMask.DataService, "[DataService] ...finished saving!");
            Services.Events.Dispatch(GameEvents.ProfileSaveCompleted);

            ioFuture.Complete(true);
            m_SaveResult = null;
        }

        private void DeleteLocalSave(string inUserCode)
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

        private void LoadOptionsSettings() 
        {
            if (Serializer.ReadPrefs(ref m_CurrentOptions, LocalSettingsPrefsKey))
            {
                DebugService.Log(LogMask.DataService, "[DataService] Loaded local options");
            }
            else
            {
                Log.Warn("[DataService] Local options not found, creating default");
                m_CurrentOptions = new OptionsData();
                m_CurrentOptions.SetDefaults(OptionsData.Authority.All);
            }
        }

        public void SaveOptionsSettings()
        {
            if (m_CurrentOptions.HasChanges())
            {
                DebugService.Log(LogMask.DataService, "[DataService] Wrote options to local");
                Serializer.WritePrefs(m_CurrentOptions, LocalSettingsPrefsKey, OutputOptions.None, Serializer.Format.Binary);
                PlayerPrefs.Save();

                if (m_CurrentSaveData != null)
                {
                    m_CurrentSaveData.Options.SetDirty();
                    AutoSave.Force();
                }
            }
        }

        #endregion // Options

        #region IService

        protected override void Initialize()
        {
            InitVariableResolver();

            m_SessionTable = new VariantTable("session");
            BindTable("session", m_SessionTable);

            Services.Events.Register(GameEvents.SceneLoaded, PerformPostLoad, this);

            m_LastKnownProfile = PlayerPrefs.GetString(LastUserNameKey, string.Empty);
            LoadOptionsSettings();

            m_DialogHistory = new RingBuffer<DialogRecord>(m_DialogHistorySize, RingBufferMode.Overwrite);

            OGD.Core.Configure(m_ServerAddress, GameId);
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);
            Ref.Dispose(ref m_SaveResult);
        }

        #endregion // IService

        #region ILoadable

        bool ILoadable.IsLoading()
        {
            return m_SaveResult != null;
        }

        #endregion // ILoadable
    }
}