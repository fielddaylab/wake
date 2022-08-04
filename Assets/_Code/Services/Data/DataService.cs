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
using EasyBugReporter;

using LogMask = Aqua.Debugging.LogMask;

namespace Aqua
{
    [ServiceDependency(typeof(AssetsService), typeof(EventService))]
    public partial class DataService : ServiceBehaviour, IDebuggable, ILoadable, IDumpSource
    {
        private const string LocalSettingsPrefsKey = "settings/local";
        private const string LastUserNameKey = "settings/last-known-profile";
        private const string DeserializeError = "deserialize-error";

        #if DEVELOPMENT
        private const string DebugSaveId = "__DEBUG";
        #endif // DEVELOPMENT

        private const string GameId = "AQUALAB";

        #region Inspector

        [SerializeField, Required] private TextAsset m_IdRenames = null;

        [Header("Server")]
        [SerializeField] private string m_ServerAddress = null;
        [SerializeField] private uint m_SaveRetryCount = 8;
        [SerializeField] private float m_SaveRetryDelay = 5;

        // [Header("Conversation History")]
        // [SerializeField, Range(32, 256)] private int m_DialogHistorySize = 128;

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
        // [NonSerialized] private RingBuffer<DialogRecord> m_DialogHistory;
        [NonSerialized] private bool m_PostLoadQueued;

        [NonSerialized] private Future<bool> m_SaveResult;
        [NonSerialized] private bool m_AutoSaveEnabled;
        [NonSerialized] private string m_LastKnownProfile;
        [NonSerialized] private bool m_ForceSavingDisabled;
        
        #if DEVELOPMENT
        [NonSerialized] private bool m_IsDebugProfile;
        [NonSerialized] private string m_LastBookmarkName;
        #endif // DEVELOPMENT

        #region Save Data

        public string DefaultCharacterName()
        {
            return m_DefaultPlayerDisplayName;
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

            DeclareProfile(CreateNewProfile(inUserCode), true, false);
            if (!IsDebugProfile())
            {
                return Future.CreateLinked<bool>(DeclareProfileToServer, this);
            }
            else
            {
                SetLastKnownProfile(m_CurrentSaveData);
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

                    DeclareProfile(debugSave, true, false);
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

            DeclareProfile(authoritativeSave, true, true);
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
                StateUtil.LoadMapWithWipe(mapId, m_CurrentSaveData.Map.SavedSceneLocationId(), null, SceneLoadFlags.Default | SceneLoadFlags.DoNotOverrideEntrance);
            }
            else
            {
                StateUtil.LoadSceneWithWipe(inSceneOverride, null, null, SceneLoadFlags.Default | SceneLoadFlags.DoNotOverrideEntrance);
            }

            AutoSave.Suppress();
        }

        private bool ClearOldProfile()
        {
            // m_DialogHistory.Clear();
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
            using(var request = OGD.GameState.RequestLatestState(inUserCode, future.Complete, (r) => future.Fail(r)))
            {
                yield return future;

                if (future.IsComplete())
                {
                    DebugService.Log(LogMask.DataService, "[DataService] Save with profile name {0} found on server!", m_ProfileName);
                    SaveData serverData = null;
                    bool bSuccess;
                    using(Profiling.Time("reading save data from server"))
                    {
                        bSuccess = Serializer.Read(ref serverData, future.Get());
                    }
                    
                    if (!bSuccess)
                    {
                        UnityEngine.Debug.LogErrorFormat("[DataService] Server profile '{0}' could not be read...", inUserCode);
                        ioFuture.Fail(DeserializeError);
                    }
                    else
                    {
                        ioFuture.Complete(serverData);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogErrorFormat("[DataService] Failed to find profile on server: {0}", future.GetFailure());
                    ioFuture.Fail(future.GetFailure());
                }
            }
        }

        private bool TryLoadProfileFromBytes(byte[] inBytes, out SaveData outProfile)
        {
            outProfile = null;
            bool bSuccess;
            using(Profiling.Time("reading save data from server"))
            {
                bSuccess = Serializer.Read(ref outProfile, inBytes);
            }
            return bSuccess;
        }

        private void DeclareProfile(SaveData inProfile, bool inbAutoSave, bool inbSetLastKnown)
        {
            m_CurrentSaveData = inProfile;
            Save.DeclareProfile(inProfile);
            
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

            if (!IsDebugProfile() && inbSetLastKnown)
            {
                SetLastKnownProfile(m_CurrentSaveData);
            }

            OptionsData.SyncFrom(m_CurrentSaveData.Options, m_CurrentOptions, OptionsData.Authority.Profile);

            HookSaveDataToVariableResolver(inProfile);
            Services.Events.Dispatch(GameEvents.ProfileLoaded);
            
            SetAutosaveEnabled(inbAutoSave);
            m_PostLoadQueued = true;
        }

        private void SetLastKnownProfile(SaveData inProfile)
        {
            m_LastKnownProfile = inProfile.Id;
            DebugService.Log(LogMask.DataService, "[DataService] Profile name {0} set as last known name", m_LastKnownProfile);
            PlayerPrefs.SetString(LastUserNameKey, m_LastKnownProfile ?? string.Empty);
            PlayerPrefs.Save();
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
            using(var request = OGD.Player.ClaimId(m_ProfileName, null, future.Complete, (r) => future.Fail(r)))
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
                    yield break;
                }
            }

            // push an empty save up to the server

            string saveData;
            using(Profiling.Time("writing save data to binary"))
            {
                saveData = Serializer.Write(m_CurrentSaveData, OutputOptions.None, Serializer.Format.Binary);
            }

            using(var future = Future.Create())
            using(var saveRequest = OGD.GameState.PushState(m_ProfileName, saveData, future.Complete, (r) => future.Fail(r)))
            {
                yield return future;

                if (future.IsComplete())
                {
                    DebugService.Log(LogMask.DataService, "[DataService] Saved to server!");
                    SetLastKnownProfile(m_CurrentSaveData);
                    ioFuture.Complete(true);
                }
                else
                {
                    Log.Error("[DataService] Failed to save to server: {0}", future.GetFailure().Object);
                    ioFuture.Fail(future.GetFailure());
                    yield break;
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

            m_SaveResult = Future.Create<bool>();
            Routine saveRoutine = Routine.Start(this, SaveRoutine(m_SaveResult, inLocationId));
            m_SaveResult.LinkTo(saveRoutine);
            saveRoutine.Tick();
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
                int attempts = (int) (m_SaveRetryCount + 1);
                while(attempts > 0)
                {
                    using(var future = Future.Create())
                    using(var saveRequest = OGD.GameState.PushState(m_ProfileName, saveData, future.Complete, (r) => future.Fail(r)))
                    {
                        yield return future;

                        if (future.IsComplete())
                        {
                            DebugService.Log(LogMask.DataService, "[DataService] Saved to server!");
                            break;
                        }
                        else
                        {
                            attempts--;
                            Log.Warn("[DataService] Failed to save to server: {0}", future.GetFailure().Object);
                            if (attempts > 0)
                            {
                                Log.Warn("[DataService] Retrying server save...", attempts);
                                yield return m_SaveRetryDelay;
                            }
                            else
                            {
                                Log.Error("[DataService] Server save failed after {0} attempts", m_SaveRetryCount + 1);
                            }
                        }
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
            return m_AutoSaveEnabled && !m_ForceSavingDisabled;
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

        internal void ForceNoSaving(bool inbNoSave)
        {
            m_ForceSavingDisabled = inbNoSave;
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
            // m_DialogHistory.PushBack(inRecord);
        }

        public RingBuffer<DialogRecord> DialogHistory
        {
            get { return /* m_DialogHistory */ null; }
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

            Save.DeclareOptions(m_CurrentOptions);
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

            SavePatcher.InitializeIdPatcher(m_IdRenames);

            // m_DialogHistory = new RingBuffer<DialogRecord>(m_DialogHistorySize, RingBufferMode.Overwrite);

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

        #region IDumpSource

        bool IDumpSource.Dump(EasyBugReporter.IDumpWriter dump) {
            dump.KeyValue("Profile Name", m_ProfileName);
            dump.KeyValue("Using Debug Profile", IsDebugProfile());
            if (m_CurrentSaveData != null) {
                dump.BeginSection("Save Profile", true);
                m_CurrentSaveData.Dump(dump);
                dump.EndSection();
            }

            return true;
        }

        #endregion // IDumpSource

        #region Utils

        static public string ErrorMessage(IFuture future, string defaultValue = "Unknown Error") {
            if (future.TryGetFailure(out Future.Failure failure)) {
                object obj = failure.Object;
                if (obj is string) {
                    return (string) obj;
                } else if (obj is OGD.Core.Error) {
                    return ((OGD.Core.Error) obj).Msg;
                }
            }

            return defaultValue;
        }

        static public ErrorStatus ReturnStatus(IFuture future, ErrorStatus defaultValue = ErrorStatus.Unknown) {
            if (future.TryGetFailure(out Future.Failure failure)) {
                object obj = failure.Object;
                if (obj is OGD.Core.ReturnStatus) {
                    return (ErrorStatus) (OGD.Core.ReturnStatus) obj;
                } else if (obj is OGD.Core.Error) {
                    return (ErrorStatus) ((OGD.Core.Error) obj).Status;
                } else if (obj == (object) DeserializeError) {
                    return ErrorStatus.DeserializeError;
                }
            }

            return defaultValue;
        }

        public enum ErrorStatus
        {
            Success,
            Error_DB,
            Error_Request,
            Error_Server,

            Error_Network,
            Error_Exception,
            Unknown,

            DeserializeError
        }

        #endregion // Utils
    }
}