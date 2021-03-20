using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Variants;
using Aqua.Profile;
using UnityEngine;
using BeauUtil.Services;

namespace Aqua
{
    [ServiceDependency(typeof(AssetsService), typeof(EventService))]
    public partial class DataService : ServiceBehaviour
    {
        // static private readonly string DebugUserDataPrefsKey = "_debugUserProfile";

        #region Inspector

        [SerializeField] private string m_DefaultPlayerDisplayName = "Unknown Player";

        [Header("Random Chances")]

        [SerializeField, Range(0, 1)] private float m_CommonChance = 0.5f;
        [SerializeField, Range(0, 1)] private float m_UncommonChance = 0.3f;
        [SerializeField, Range(0, 1)] private float m_RareChance = 0.1f;

        #endregion // Inspector

        [NonSerialized] private SaveData m_CurrentSaveData = new SaveData();
        [NonSerialized] private VariantTable m_SessionTable;

        [NonSerialized] private CustomVariantResolver m_VariableResolver;

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

        #region Load

        public void LoadProfile()
        {
            m_CurrentSaveData.Inventory.SetDefaults();

            // SaveData fromPrefs = null;
            // if (Serializer.ReadPrefs(ref fromPrefs, DebugUserDataPrefsKey))
            // {
            //     m_CurrentSaveData = fromPrefs;
            // }

            HookSaveDataToVariableResolver(m_CurrentSaveData);
            m_SessionTable.Clear();

            Services.Events.Dispatch(GameEvents.ProfileLoaded);
        }

        public void SaveProfile()
        {
            // Serializer.WritePrefs(m_CurrentSaveData, DebugUserDataPrefsKey, OutputOptions.None, Serializer.Format.JSON);
            // PlayerPrefs.Save();
        }

        #endregion // Load

        #region IService

        protected override void Initialize()
        {
            InitVariableResolver();

            m_SessionTable = new VariantTable("session");
            BindTable("session", m_SessionTable);
        }

        protected override void Shutdown()
        {
            SaveProfile();
        }

        #endregion // IService
    }
}