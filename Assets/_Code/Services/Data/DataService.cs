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
using BeauUtil.Debugger;
using Aqua.Debugging;

namespace Aqua
{
    [ServiceDependency(typeof(AssetsService), typeof(EventService))]
    public partial class DataService : ServiceBehaviour, IDebuggable
    {
        // static private readonly string DebugUserDataPrefsKey = "_debugUserProfile";

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
        // [NonSerialized] private Options m_CurrentOptions;
        [NonSerialized] private VariantTable m_SessionTable;

        [NonSerialized] private CustomVariantResolver m_VariableResolver;
        [NonSerialized] private RingBuffer<DialogRecord> m_DialogHistory;

        #region Save Data

        public SaveData Profile
        {
            get { return m_CurrentSaveData; }
        }

        // public Options Settings
        // {
        //     get { return m_CurrentOptions; }
        // }

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

        public bool IsProfileLoaded()
        {
            return m_CurrentSaveData != null;
        }

        public void LoadProfile()
        {
            m_CurrentSaveData = new SaveData();
            m_CurrentSaveData.Inventory.SetDefaults();
            m_DialogHistory.Clear();

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

            m_DialogHistory = new RingBuffer<DialogRecord>(m_DialogHistorySize, RingBufferMode.Overwrite);
        }

        protected override void Shutdown()
        {
            SaveProfile();
        }

        #endregion // IService

        #region IDebuggable

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus()
        {
            DMInfo jobsMenu = DebugService.NewDebugMenu("Jobs");

            DMInfo startJobMenu = DebugService.NewDebugMenu("Start Job");
            foreach(var job in Services.Assets.Jobs.Objects)
                RegisterJobStart(startJobMenu, job.Id());

            jobsMenu.AddSubmenu(startJobMenu);
            jobsMenu.AddDivider();
            jobsMenu.AddButton("Complete Current Job", () => Services.Data.Profile.Jobs.MarkComplete(Services.Data.CurrentJob()), () => !Services.Data.CurrentJobId().IsEmpty);
            jobsMenu.AddButton("Clear All Job Progress", () => Services.Data.Profile.Jobs.ClearAll());

            yield return jobsMenu;

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

        #endregion // IDebuggable
    }
}