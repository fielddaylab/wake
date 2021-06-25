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
        [NonSerialized] private string m_ProfileName;

        [NonSerialized] private CustomVariantResolver m_VariableResolver;
        [NonSerialized] private RingBuffer<DialogRecord> m_DialogHistory;
        [NonSerialized] private bool m_PostLoadQueued;

        [NonSerialized] private Future<bool> m_SaveResult;
        [NonSerialized] private bool m_AutoSaveEnabled;
        [NonSerialized] private string m_LastKnownProfile;

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

        public Future<bool> HasProfile(string inUserCode)
        {
            if (string.IsNullOrEmpty(inUserCode))
            {
                Log.Warn("[DataService] Empty user code provided'");
                return Future.Failed<bool>();
            }

            string key = GetPrefsKeyForCode(inUserCode);
            return Future.Completed(PlayerPrefs.HasKey(key));
        }

        public Future<bool> NewProfile(string inUserCode)
        {
            if (string.IsNullOrEmpty(inUserCode))
            {
                Log.Warn("[DataService] Empty user code provided'");
                return Future.Failed<bool>();
            }

            ClearOldProfile();
            DeleteSave(inUserCode);

            DeclareProfile(CreateNewProfile(), inUserCode, true);
            return Future.Completed(true);
        }

        public Future<bool> LoadProfile(string inUserCode)
        {
            if (string.IsNullOrEmpty(inUserCode))
            {
                Log.Warn("[DataService] Empty user code provided'");
                return Future.Failed<bool>();
            }

            SaveData saveData;
            if (TryLoadProfileFromPrefs(inUserCode, out saveData))
            {
                ClearOldProfile();

                DebugService.Log(LogMask.DataService, "[DataService] Loaded profile with user id '{0}'", inUserCode);

                DeclareProfile(saveData, inUserCode, true);
                return Future.Completed(true);
            }
            else
            {
                DebugService.Log(LogMask.DataService, "[DataService] No profile with id {0}'", inUserCode);
                return Future.Completed(false);
            }
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

        private SaveData CreateNewProfile()
        {
            SaveData data = new SaveData();
            data.Id = Guid.NewGuid().ToString();
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

        private bool TryLoadProfileFromBytes(byte[] inBytes, out SaveData outProfile)
        {
            outProfile = null;
            return Serializer.Read(ref outProfile, inBytes);
        }

        private void DeclareProfile(SaveData inProfile, string inUserCode, bool inbAutoSave)
        {
            m_ProfileName = inUserCode;
            m_CurrentSaveData = inProfile;

            if (!string.IsNullOrEmpty(inUserCode))
            {
                m_LastKnownProfile = inUserCode;
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
            if (!m_PostLoadQueued)
                return;

            m_CurrentSaveData.Jobs.PostLoad();
            m_PostLoadQueued = false;
            
            Services.Events.Dispatch(GameEvents.ProfileStarted);
        }

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
            yield return null;

            string key = GetPrefsKeyForCode(m_ProfileName);
            Services.Events.Dispatch(GameEvents.ProfileSaveBegin);

            DebugService.Log(LogMask.DataService, "[DataService] Saving to '{0}'...", key);

            Serializer.WritePrefs(m_CurrentSaveData, key, OutputOptions.None, Serializer.Format.Binary);
            yield return null;
            
            PlayerPrefs.Save();
            yield return null;
            
            DebugService.Log(LogMask.DataService, "[DataService] ...finished saving to '{0}'", key);
            m_CurrentSaveData.MarkChangesPersisted();
            Services.Events.Dispatch(GameEvents.ProfileSaveCompleted);

            ioFuture.Complete(true);
            m_SaveResult = null;
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