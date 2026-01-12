#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using Aqua.Argumentation;
using Aqua.Modeling;
using Aqua.Portable;
using Aqua.Profile;
using Aqua.Scripting;
using Aqua.Shop;
using BeauUtil;
using BeauUtil.Services;
using ProtoAqua.ExperimentV2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using OGD;
using BeauUtil.Debugger;
using Aqua.Debugging;
using BeauPools;
using BeauData;
using Aqua.Analytics;
using Aqua.JobBoard;
using System.Runtime.CompilerServices;
using FieldDay;
using FieldDay.Data;
using UnityEngine.SceneManagement;
using Leaf;

namespace Aqua
{
    [ServiceDependency(typeof(EventService), typeof(ScriptingService))]
    public partial class AnalyticsService : ServiceBehaviour, IDebuggable
    {
        private const string NoActiveJobId = "no-active-job";
        private const int ClientLogVersion = 5;

        static private readonly string[] FactTypeStringTable = Enum.GetNames(typeof(BFTypeId));

        static private readonly LruCache<StringHash32, string> AssetNameCache = new LruCache<StringHash32, string>(2048, new CacheCallbacks<StringHash32, string>() {
            Fetch = (a) => Assets.NameOf(a)
        });

        #region GameState

        /// <summary>
        /// A snapshot of the player's current game context for analytics logging.
        /// This struct is refreshed via <see cref="RefreshGameState()"> before each analytics event is logged, serializing this contextual state to JSON and attaching it to the event payload.
        /// See also <see cref="UpdateJobInfo()"> and <see cref="UpdateCurrencyInfo()"/>
        /// </summary>
        private struct GameState {
            public struct JobTask {
                public string task_id;
                public bool is_complete;
                public string task_description;
            }

            public string job_id;
            public int job_experimentation;
            public int job_modeling;
            public int job_argumentation;
            public JobTask[] task_list;
            public int task_list_count;

            public string region;
            public string site;
            public string scene_type;

            public int money;
            public int science_points;
            public int science_level;

            public double time_since_launch;

            public void WriteToJSON(JsonBuilder gs) {
                gs.Field("job_id", job_id);
                if (job_experimentation >= 0) {
                    gs.Field("job_experimentation", job_experimentation);
                    gs.Field("job_modeling", job_modeling);
                    gs.Field("job_argumentation", job_argumentation);
                    gs.BeginArray("task_list");
                    for(int i = 0; i < task_list_count; i++) {
                        var task = task_list[i];
                        gs.BeginObject();
                        gs.Field("task_id", task.task_id);
                        gs.Field("task_description", task.task_description);
                        gs.Field("is_complete", task.is_complete);
                        gs.EndObject();
                    }
                    gs.EndArray();
                }

                gs.Field("region", region);
                gs.Field("site", site);
                gs.Field("scene_type", scene_type);

                gs.Field("money", money);
                gs.Field("science_points", science_points);
                gs.Field("science_level", science_level);

                gs.Field("time_since_launch", time_since_launch, 2);
            }
        }

        #endregion // GameState

        #region Inspector

        [SerializeField, Required] private string m_AppId = "AQUALAB";
        [SerializeField, Required] private string m_AppVersion = "6.2";
        [SerializeField, Required] private SurveyPanel m_SurveyPrefab;
        [SerializeField] private TextAsset m_SurveyData;
        [SerializeField] private FirebaseConsts m_Firebase = default(FirebaseConsts);
        
        #endregion // Inspector

        #region Logging Variables

        private OGDLog m_Log;
        private OGDSurvey m_Survey;

        [NonSerialized] private StringHash32 m_CurrentJobHash = null;
        [NonSerialized] private JobDesc m_CurrentJobAsset;
        [NonSerialized] private string m_PreviousJobName = NoActiveJobId;

        [NonSerialized] private PortableAppId m_CurrentPortableAppId = PortableAppId.NULL;
        [NonSerialized] private BestiaryDescCategory? m_CurrentPortableBestiaryTabId = null;
        [NonSerialized] private string m_CurrentModelPhase = string.Empty;
        [NonSerialized] private string m_CurrentModelEcosystem = string.Empty;
        [NonSerialized] private string m_CurrentTankType = string.Empty;
        [NonSerialized] private string m_CurrentEnvironment = string.Empty;
        [NonSerialized] private List<string> m_CurrentCritters = new List<string>();
        [NonSerialized] private bool m_StabilizerEnabled = false;
        [NonSerialized] private bool m_AutoFeederEnabled = false;
        [NonSerialized] private StringHash32 m_CurrentArgumentId = null;
        [NonSerialized] private bool m_Debug;
        [NonSerialized] private FourCC m_CurrentLanguage;

        [NonSerialized] private GameState m_GameState = new GameState() {
            job_id = NoActiveJobId,
            job_argumentation = -1,
            job_experimentation = -1,
            job_modeling = -1,
            task_list = new GameState.JobTask[16],
        };

        [NonSerialized] private JsonBuilder m_JsonBuilder = new JsonBuilder(2048);
        
        #endregion // Logging Variables

        #region IService

        protected override void Initialize()
        {
            Services.Events.Register<StringHash32>(GameEvents.JobStarted, LogAcceptJob, this)
                .Register<string>(GameEvents.ProfileStarting, OnProfileStarting, this)
                .Register(GameEvents.ProfileUnloaded, OnProfileUnloaded, this)
                .Register(GameEvents.ProfileStarted, OnProfileStarted, this)
                .Register<FourCC>(GameEvents.OnLanguageChange, LogSelectLanguage, this)
                .Register<StringHash32>(GameEvents.JobSwitched, LogSwitchJob, this)
                .Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, HandleBestiaryUpdated, this)
                .Register<StringHash32>(GameEvents.JobCompleted, LogCompleteJob, this)
                .Register<string>(GameEvents.ViewChanged, LogRoomChanged, this)
                .Register<string>(GameEvents.ScriptFired, LogScriptFired, this)
                .Register<DialogPanel.TextDisplayArgs>(GameEvents.TextLineDisplayed, LogScriptLine, this)
                .Register<TankType>(ExperimentEvents.ExperimentBegin, LogBeginExperiment, this)
                .Register<string>(GameEvents.BeginDive, LogBeginDive, this)
                .Register(ModelingConsts.Event_Simulation_Begin, LogBeginSimulation, this)
                .Register<int>(ModelingConsts.Event_Simulation_Complete, LogSimulationSyncAchieved, this)
                .Register<PortableAppId>(GameEvents.PortableAppOpened, PortableAppOpenedHandler, this)
                .Register<PortableAppId>(GameEvents.PortableAppClosed, PortableAppClosedHandler, this)
                // .Register<BestiaryDescCategory>(GameEvents.PortableBestiaryTabSelected, PortableBestiaryTabSelectedHandler, this)
                .Register(ModelingConsts.Event_Begin_Model, LogBeginModel, this)
                .Register<ModelPhases>(ModelingConsts.Event_Phase_Changed, LogModelPhaseChanged, this)
                .Register<string>(ModelingConsts.Event_Ecosystem_Selected, LogModelEcosystemSelected, this)
                .Register(ModelingConsts.Event_Concept_Started, LogModelConceptStarted, this)
                .Register<ConceptualModelState.StatusId>(ModelingConsts.Event_Concept_Updated, LogModelConceptUpdated, this)
                .Register(ModelingConsts.Event_Concept_Exported, LogModelConceptExported, this)
                .Register<int>(ModelingConsts.Event_Sync_Error, LogModelSyncError, this)
                .Register(ModelingConsts.Event_Predict_Complete, LogModelPredictCompleted, this)
                .Register<InterveneUpdateData>(ModelingConsts.Event_Intervene_Update, LogModelInterveneUpdate, this)
                .Register(ModelingConsts.Event_Intervene_Error, LogModelInterveneError, this)
                .Register(ModelingConsts.Event_Intervene_Complete, LogModelInterveneCompleted, this)
                .Register(ModelingConsts.Event_End_Model, LogEndModel, this)
                .Register<BestiaryDesc>(GameEvents.PortableEntrySelected, PortableBestiaryEntrySelectedhandler, this)
                .Register(GameEvents.ScenePreloading, ClearSceneState, this)
                .Register(GameEvents.PortableClosed, PortableClosed, this)
                .Register<StringHash32>(GameEvents.InventoryUpdated, LogPurchaseUpgrade, this)
                .Register<StringHash32>(ShopConsts.Event_InsufficientFunds, LogInsufficientFunds, this)
                .Register(ShopConsts.Event_TalkToShopkeep, LogTalkToShopkeep, this)
                .Register<TankType>(ExperimentEvents.ExperimentView, SetCurrentTankType, this)
                .Register<MeasurementTank.FeatureMask>(ExperimentEvents.ExperimentEnableFeature, SetTankFeatureEnabled, this)
                .Register<MeasurementTank.FeatureMask>(ExperimentEvents.ExperimentDisableFeature, SetTankFeatureDisabled, this)
                .Register<StringHash32>(ExperimentEvents.ExperimentAddEnvironment, LogAddEnvironment, this)
                .Register<StringHash32>(ExperimentEvents.ExperimentRemoveEnvironment, LogRemoveEnvironment, this)
                .Register<StringHash32>(ExperimentEvents.ExperimentAddCritter, LogAddCritter, this)
                .Register<StringHash32>(ExperimentEvents.ExperimentRemoveCritter, LogRemoveCritter, this)
                .Register<TankType>(ExperimentEvents.ExperimentEnded, LogEndExperiment, this)
                .Register<StringHash32>(ArgueEvents.Loaded, LogBeginArgument, this)
                .Register<StringHash32>(ArgueEvents.FactSubmitted, LogFactSubmitted, this)
                .Register<StringHash32>(ArgueEvents.FactRejected, LogFactRejected, this)
                .Register(ArgueEvents.Unloaded, LogLeaveArgument, this)
                .Register<StringHash32>(ArgueEvents.Completed, LogCompleteArgument, this)
                .Register<JobRecommendationArgs>(GameEvents.DisplayedJobRecommendation, LogRecommendedJob, this);

            JobEvents.OnJobTaskCompleted.Register(LogCompleteTask);

            Services.Script.OnTargetedThreadStarted += GuideHandler;
            SceneHelper.OnSceneLoaded += LogSceneChanged;

            CrashHandler.OnCrash += OnCrash;

            NetworkStats.OnError.Register(OnNetworkError);

            m_Log = new OGDLog(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = ClientLogVersion
            }, new OGDLog.MemoryConfig(
                4096, 1024 * 32, 256
            )); // 32 kb game_state buffer? it's for switch_job, that can be massive, up to 32kb
            m_Log.UseFirebase(m_Firebase);

            #if DEVELOPMENT && !UNITY_EDITOR
            m_Debug = true;
            #endif // DEVELOPMENT && !UNITY_EDITOR

            m_Log.SetDebug(m_Debug);

            m_Survey = new OGDSurvey(m_SurveyPrefab, m_Log);
            m_Survey.OnSurveyBegin += (s) => {
                Services.Events.Dispatch(GameEvents.SurveyStart, EvtArgs.Ref(s));
            };
            m_Survey.OnSurveyEnd += (s) => {
                Services.Events.Dispatch(GameEvents.SurveyEnd, EvtArgs.Ref(s));
            };

            if (m_SurveyData != null) {
                m_Survey.LoadSurveyPackageFromString(m_SurveyData.text);
            }

            Services.Loc.OnManifestUpdated.Register((m) => {
                if (m.Surveys != m_SurveyData) {
                    m_SurveyData = m.Surveys;
                    m_Survey.LoadSurveyPackageFromString(m_SurveyData.text);
                }
            });

            RefreshGameState();
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);
            m_Log.Dispose();
            m_Survey = null;
        }

        #endregion // IService

        #region Handlers

        private void OnProfileStarting(string userCode) {
            SetUserCode(userCode);

            /// Ensure <see cref='m_GameState'> is properly initalized
            UpdateJobInfo(Save.CurrentJobId);
            UpdateCurrencyInfo();

            ResearchTests.HandleProfileStart(m_Survey);
        }

        private void SetUserCode(string userCode) {
            m_Log.Initialize(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = ClientLogVersion,
                AppBranch = ResearchTests.GetModifiedBranchName(BuildInfo.Branch())
            });
            m_Log.SetUserId(userCode);
        }

        private void OnProfileUnloaded() {
            m_Log.Initialize(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = ClientLogVersion,
                AppBranch = BuildInfo.Branch()
            });
            m_Log.SetUserId(null);

            m_CurrentJobAsset = null;
            m_GameState.money = m_GameState.science_level = m_GameState.science_points = 0;

            UpdateJobInfo(default);
            UpdateCurrencyInfo();
            ResearchTests.HandleProfileEnd();
        }

        #endregion // Handlers

        #region GameState

        private void ClearSceneState()
        {
            m_CurrentPortableAppId = PortableAppId.NULL;
            m_CurrentPortableBestiaryTabId = null;
        }

        /// <summary>
        /// Update <see cref="m_GameState"/> with data from the provided current job <paramref name="id"/>.
        /// If no job is provided (empty id), we reset all job-related fields to default values.
        /// Otherwise, populates job metadata including difficulty ratings for each science activity type and the list of associated tasks with their completion status.
        /// </summary>
        /// <param name="id">The unique identifier of the job to update, or empty to clear job info.</param>
        private void UpdateJobInfo(StringHash32 id) {
            if (id.IsEmpty) {
                m_CurrentJobAsset = null;

                m_GameState.job_id = NoActiveJobId;
                m_GameState.job_argumentation = -1;
                m_GameState.job_modeling = -1;
                m_GameState.job_argumentation = -1;
                m_GameState.task_list_count = 0;
            } else {
                JobDesc job = Assets.Job(id);
                m_CurrentJobAsset = job;

                m_GameState.job_id = job.name;
                m_GameState.job_experimentation = job.Difficulty(ScienceActivityType.Experimentation);
                m_GameState.job_modeling = job.Difficulty(ScienceActivityType.Modeling);
                m_GameState.job_argumentation = job.Difficulty(ScienceActivityType.Argumentation);

                var tasks = job.Tasks();
                m_GameState.task_list_count = tasks.Length;
                Assert.True(tasks.Length <= m_GameState.task_list.Length);
                for(int i = 0; i < tasks.Length; i++) {
                    var task = tasks[i];
                    ref var taskData = ref m_GameState.task_list[i];
                    taskData.task_id = task.IdString;
                    taskData.task_description = Loc.Find(task.LabelId);
                    taskData.is_complete = Save.Jobs.IsTaskComplete(task.Id);
                }
            }
        }

        private void UpdateJobTaskCompletion() {
            if (m_CurrentJobAsset != null) {
                var tasks = m_CurrentJobAsset.Tasks();
                for (int i = 0; i < tasks.Length; i++) {
                    m_GameState.task_list[i].is_complete = Save.Jobs.IsTaskComplete(tasks[i].Id);
                }
            }
        }

        private void UpdateSceneInfo() {
            var currentScene = SceneManager.GetActiveScene();
            StringHash32 sceneId = currentScene.name;
            int sceneIdx = currentScene.buildIndex;

            if (sceneIdx >= GameConsts.GameSceneIndexStart) {
                Assert.True(Save.IsLoaded, "Save is not loaded");

                MapDesc map = Assets.Map(MapDB.LookupMap(currentScene));
                m_GameState.region = Assets.Map(Save.Map.CurrentStationId()).name;
                if (map) {
                    m_GameState.site = map.name;

                    if (map.HasFlags(MapFlags.IsStationInterior)) {
                        m_GameState.scene_type = "station-interior";
                    } else if (map.Category() == MapCategory.ShipRoom) {
                        m_GameState.scene_type = "ship";
                    } else if (map.Category() == MapCategory.Station) {
                        m_GameState.scene_type = "surface";
                    } else {
                        m_GameState.scene_type = "dive-site";
                    }
                } else {
                    m_GameState.site = currentScene.name;
                    if (sceneId == GameScenes.StationTransition) {
                        m_GameState.scene_type = "transition";
                    } else if (m_GameState.site.StartsWith("Dream")) {
                        m_GameState.scene_type = "dream";
                    } else {
                        m_GameState.scene_type = "unknown";
                    }
                }
            } else {
                m_GameState.region = "no-active-region";
                m_GameState.site = "no-active-site";
                m_GameState.scene_type = "menu";
            }
        }

        private void UpdateCurrencyInfo() {
            m_GameState.money = (int) Save.Cash;
            m_GameState.science_points = (int) Save.Exp;
            m_GameState.science_level = (int) Save.ExpLevel;
        }

        private void RefreshGameState() {
            using (var gs = m_Log.OpenGameState(m_JsonBuilder)) {
                m_GameState.time_since_launch = Time.realtimeSinceStartup;
                m_GameState.WriteToJSON(gs);
            }
        }

        #endregion // Game State

        #region Log Events

        /// <summary>
        /// Quick note, to ensure that new events are logged correctly, make a call to RefreshGameState() before each call.
        /// </summary>

        private void GuideHandler(ScriptThreadHandle inThread)
        {
            if (inThread.TargetId() != GameConsts.Target_V1ctor)
            {
                return;
            }

            string nodeId = inThread.RootNodeName();

            if (inThread.TriggerId() == GameTriggers.RequestPartnerHelp)
            {
                LogAskForHelp(nodeId);
            }
            else
            {
                LogGuideScriptTriggered(nodeId);
            }
        }

        private void LogSceneChanged(SceneBinding scene, object context)
        {
            string sceneName = scene.Name;

            if (sceneName != "Boot" && sceneName != "Title")
            {
                UpdateSceneInfo();
                RefreshGameState();

                using(var e = m_Log.NewEvent("scene_changed")) {
                    e.Param("scene_name", sceneName);
                }
            }
        }

        private void LogRoomChanged(string roomName)
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("room_changed")) {
                e.Param("room_name", roomName);
            }
        }

        #region bestiary handlers
        private void PortableAppOpenedHandler(PortableAppId inId)
        {

            if (m_CurrentPortableAppId != inId)
                PortableAppClosedHandler(m_CurrentPortableAppId);

            m_CurrentPortableAppId = inId;
            switch(inId)
            {
                case PortableAppId.Organisms:
                    {
                        LogOpenBestiaryOrganisms();
                        break;
                    }

                case PortableAppId.Environments:
                    {
                        LogOpenBestiaryEnvironments();
                        break;
                    }

                case PortableAppId.Job:
                    {
                        LogOpenStatus();
                        LogStatusOpenJobTab();
                        break;
                    }

                case PortableAppId.Tech:
                    {
                        LogOpenStatus();
                        LogStatusOpenTechTab();
                        break;
                    }
            }
        }

        private void PortableAppClosedHandler(PortableAppId appId)
        {
            if (m_CurrentPortableAppId != appId)
                return;

            m_CurrentPortableAppId = PortableAppId.NULL;
            switch(appId)
            {
                case PortableAppId.Environments:
                case PortableAppId.Organisms:
                    {
                        m_CurrentPortableBestiaryTabId = null;
                        LogCloseBestiary();
                        break;
                    }

                case PortableAppId.Job:
                case PortableAppId.Tech:
                    {
                        LogCloseStatus();
                        break;
                    }
            }
        }

        private void PortableClosed()
        {
            if (m_CurrentPortableAppId != PortableAppId.NULL)
                PortableAppClosedHandler(m_CurrentPortableAppId);
        }

        private void PortableBestiaryTabSelectedHandler(BestiaryDescCategory tabName)
        {
            if (tabName == m_CurrentPortableBestiaryTabId) //Tab already open, don't send another log
                return;
            else
                m_CurrentPortableBestiaryTabId = tabName;

            switch (tabName)
            {
                case (BestiaryDescCategory.Critter): //Critter Tab
                    {
                        LogBestiaryOpenSpeciesTab();
                        break;
                    }
                case (BestiaryDescCategory.Environment): //Ecosystems Tab
                    {
                        LogBestiaryOpenEnvironmentsTab();
                        break;
                    }
                // case (BestiaryDescCategory.Model): //Models Tab
                //     {
                //         LogBestiaryOpenModelsTab();
                //         break;
                //     }
            }
        }

        private void PortableBestiaryEntrySelectedhandler(BestiaryDesc selectedData)
        {
            switch (selectedData.Category())
            {
                case (BestiaryDescCategory.Critter): //Critter Selected
                    {
                        LogBestiarySelectSpecies(selectedData.name);
                        break;
                    }
                case (BestiaryDescCategory.Environment): //Ecosystem Selected
                    {
                        LogBestiarySelectEnvironment(selectedData.name);
                        break;
                    }
                // case (BestiaryDescCategory.Model): //Model Selected
                //     {
                //         LogBestiarySelectModel(selectedData.name);
                //         break;
                //     }
            }
        }
        #endregion

        private void OnProfileStarted() {
            m_PreviousJobName = NoActiveJobId;
            UpdateCurrencyInfo();
            SetCurrentJob(Save.CurrentJobId);
        }

        private void LogSelectLanguage(FourCC langCode) {
            m_CurrentLanguage = Services.Loc.CurrentLanguageId;

            string selectedLang;
            if (langCode.Equals(FourCC.Parse("ES"))) {
                selectedLang = "SPANISH";
            }
            else {
                selectedLang = "ENGLISH";
            }

            RefreshGameState();
            using (var e = m_Log.NewEvent("select_language")) {
                e.Param("language", selectedLang);
            }
        }

        private void SetCurrentJob(StringHash32 jobId)
        {
            m_CurrentJobHash = jobId;
            m_PreviousJobName = m_GameState.job_id;

            UpdateJobInfo(jobId);
            RefreshGameState();
        }

        private void LogAcceptJob(StringHash32 jobId)
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("accept_job")) {
            }
        }

        private void LogRecommendedJob(JobRecommendationArgs recArgs) {
            RefreshGameState();
            using(var e = m_Log.NewEvent("recommended_job")) {
                e.Param("attempted_job_name", Assets.NameOf(recArgs.JobId));
                e.Param("recommended_job_name", recArgs.RecommendationId.IsEmpty ? "" : Assets.NameOf(recArgs.RecommendationId));
            }
        }

        private void LogSwitchJob(StringHash32 jobId)
        {
            SetCurrentJob(jobId);

            RefreshGameState();
            
            using (var e = m_Log.NewEvent("switch_job")) {
                e.Param("prev_job_name", m_PreviousJobName);
            }
        }

        private void HandleBestiaryUpdated(BestiaryUpdateParams inParams)
        {
            void AddFactDetails(EventScope e, BFBase fact) {
                e.Param("fact_id", fact.name);
                e.Param("fact_entity", fact.Parent.name);
                e.Param("fact_type", FactTypeStringTable[(int) fact.Type]);
                e.Param("fact_stressed", BFType.OnlyWhenStressed(fact));

                bool hasRate = (BFType.Flags(fact) & BFFlags.HasRate) != 0;
                e.Param("fact_rate", hasRate);
                e.Param("has_rate", hasRate && (Save.Bestiary.GetDiscoveredFlags(fact) & BFDiscoveredFlags.Rate) != 0);
            };

            RefreshGameState();
            if (inParams.Type == BestiaryUpdateParams.UpdateType.Fact)
            {
                BFBase fact = Assets.Fact(inParams.Id);
                
                using(var e = m_Log.NewEvent("receive_fact")) {
                    AddFactDetails(e, fact);
                }
            }
            else if (inParams.Type == BestiaryUpdateParams.UpdateType.UpgradeFact)
            {
                BFBase fact = Assets.Fact(inParams.Id);
                
                using(var e = m_Log.NewEvent("upgrade_fact")) {
                    AddFactDetails(e, fact);
                }
            }
            else if (inParams.Type == BestiaryUpdateParams.UpdateType.Entity)
            {
                string parsedEntityId = Assets.Bestiary(inParams.Id).name;

                using(var e = m_Log.NewEvent("receive_entity")) {
                    e.Param("entity_id", parsedEntityId);
                }
            }
        }

        private void LogCompleteJob(StringHash32 jobId)
        {
            var job = Assets.Job(jobId);
            string parsedJobName = job.name;

            UpdateCurrencyInfo();
            RefreshGameState();
            using(var e = m_Log.NewEvent("complete_job")) {
                e.Param("job_name", parsedJobName);
            }

            ScienceTweaks tweaks = Services.Tweaks.Get<ScienceTweaks>();

            if (!job.HasFlags(JobDescFlags.Hidden | JobDescFlags.NoPopup)) {
                CheckDefaultSurveys();
            }

            if (jobId == JobIds.Final_final && !string.IsNullOrEmpty(tweaks.FinalJobSurvey())) {
                SceneHelper.OnSceneLoaded += FinalFinalSurveyTriggerCheck;
            }
        }

        private string[] CheckDefaultSurveys() {
            ScienceTweaks tweaks = Services.Tweaks.Get<ScienceTweaks>();
            int jobCount = Save.Current.Jobs.CompletedJobIds().Count;
            string[] surveys = tweaks.GetJobCountSurveys(jobCount);

            if (surveys != null && surveys.Length > 0) {
                for (int i = 0; i < surveys.Length; i++) {
                    string survey = surveys[i];
                    Services.Script.QueueInvoke(() => {
                        m_Survey.TryDisplaySurvey(survey);
                    }, -10 - i);
                }
            }

            return surveys;
        }

        private void FinalFinalSurveyTriggerCheck(SceneBinding s, object c) {
            if (s.Id == GameScenes.RS_1C_StationInterior) {
                string[] surveys = CheckDefaultSurveys();
                ScienceTweaks tweaks = Services.Tweaks.Get<ScienceTweaks>();
                string finalSurvey = tweaks.FinalJobSurvey();
                if (surveys == null || !ArrayUtils.Contains(surveys, finalSurvey)) {
                    Services.Script.QueueInvoke(() => {
                        m_Survey.TryDisplaySurvey(finalSurvey);
                    }, -10000);
                }
                SceneHelper.OnSceneLoaded -= FinalFinalSurveyTriggerCheck;
            }
        }

        private void LogCompleteTask(StringHash32 jobId, StringHash32 inTaskId)
        {
            string taskId = Assets.Job(jobId).Task(inTaskId).IdString;

            RefreshGameState();
            using(var e = m_Log.NewEvent("complete_task")) {
                e.Param("task_id", taskId);
            }
        }

        private void LogBeginDive(string inTargetScene)
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("begin_dive")) {
                e.Param("site_id", inTargetScene);
            }
        }

        private void LogBeginModel() {
            RefreshGameState();
            using(var e = m_Log.NewEvent("begin_model")) { }
        }

        private void LogBeginSimulation() {
            RefreshGameState();
            using(var e = m_Log.NewEvent("begin_simulation")) { }
        }

        #region Bestiary App Logging
        private void LogOpenBestiaryOrganisms()
        {
            m_CurrentPortableBestiaryTabId = BestiaryDescCategory.Critter;

            RefreshGameState();
            using(var e = m_Log.NewEvent("open_bestiary")) { }
            LogBestiaryOpenSpeciesTab();
        }

        private void LogOpenBestiaryEnvironments()
        {
            m_CurrentPortableBestiaryTabId = BestiaryDescCategory.Environment;

            RefreshGameState();
            using(var e = m_Log.NewEvent("open_bestiary")) { }
            LogBestiaryOpenEnvironmentsTab();
        }

        private void LogBestiaryOpenSpeciesTab() {
            RefreshGameState();
            using(var e = m_Log.NewEvent("bestiary_open_species_tab")) { }
        }
        private void LogBestiaryOpenEnvironmentsTab()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("bestiary_open_environments_tab")) { }
        }
        private void LogBestiaryOpenModelsTab()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("bestiary_open_models_tab")) { }
        }

        private void LogBestiarySelectSpecies(string speciesId)
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("bestiary_select_species")) {
                e.Param("species_id", speciesId);
            }
        }
        private void LogBestiarySelectEnvironment(string environmentId)
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("bestiary_select_environment")) {
                e.Param("environment_id", environmentId);
            }
        }
        private void LogBestiarySelectModel(string modelId)
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("bestiary_select_model")) {
                e.Param("model_id", modelId);
            }
        }
        private void LogCloseBestiary()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("close_bestiary")) {
            }
        }
        #endregion

        #region Status App Logging
        private void LogOpenStatus()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("open_status")) {
            }

            LogStatusOpenJobTab(); //Status starts by opening tasks tab
        }

        private void LogStatusOpenJobTab()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("status_open_job_tab")) {
            }
        }

        private void LogStatusOpenItemTab()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("status_open_item_tab")) {
            }
        }

        private void LogStatusOpenTechTab()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("status_open_tech_tab")) {
            }
        }

        private void LogCloseStatus()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("close_status")) {
            }
        }
        #endregion

        private void LogSimulationSyncAchieved(int sync)
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("simulation_sync_achieved")) {
                e.Param("ecosystem", m_CurrentModelEcosystem);
                e.Param("sync", sync);
            }
        }

        #region Job Events

        #endregion // Job Events

        #region Dialog Events

        private void LogAskForHelp(string nodeId) {
            RefreshGameState();
            using (var e = m_Log.NewEvent("ask_for_help")) {
                e.Param("node_id", nodeId);
            }
        }

        private void LogGuideScriptTriggered(string nodeId) {
            RefreshGameState();
            using (var e = m_Log.NewEvent("guide_script_triggered")) {
                e.Param("node_id", nodeId);
            }
        }

        private void LogScriptFired(string nodeId) {
            RefreshGameState();
            using (var e = m_Log.NewEvent("script_began")) {
                e.Param("node_id", nodeId);
            }
        }

        private void LogScriptLine(DialogPanel.TextDisplayArgs args) {
            RefreshGameState();
            using (var e = m_Log.NewEvent("script_line_displayed")) {
                e.Param("text_string", args.VisibleText);
                e.Param("node_id", args.NodeId);
                e.Param("speaker", args.Speaker);
            }
        }

        private void LogScriptOptionsDisplayed(DialogPanel.OptionsDisplayedArgs args) {
            RefreshGameState();
        }

        private void LogDialogChoiceSelected(DialogPanel.OptionsDisplayedArgs args) {
            RefreshGameState();
            using(var e = m_Log.NewEvent("select_dialog_choice")) {
                //args.
            }
        }

        private void LogDialogNextLineClicked(DialogPanel.ClickNextLineArgs args) {
            RefreshGameState();
            using (var e = m_Log.NewEvent("click_next_line")) {
                e.Param("node_id", args.NodeId);
            }
        }

        #endregion // Dialog Events

        #region Modeling Events

        private void LogStartModel()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("model_start")) {
            }
        }

        private void LogModelPhaseChanged(ModelPhases inPhase)
        {
            m_CurrentModelPhase = ((ModelPhases)inPhase).ToString();

            RefreshGameState();
            using(var e = m_Log.NewEvent("model_phase_changed")) {
                e.Param("phase", m_CurrentModelPhase);
            }
        }

        private void LogModelEcosystemSelected(string ecosystem)
        {
            m_CurrentModelEcosystem = ecosystem;

            RefreshGameState();
            using(var e = m_Log.NewEvent("model_ecosystem_selected")) {
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogModelConceptStarted()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("model_concept_started")) {
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogModelConceptUpdated(ConceptualModelState.StatusId status)
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("model_concept_updated")) {
                e.Param("ecosystem", m_CurrentModelEcosystem);
                e.Param("status", status.ToString());
            }
        }

        private void LogModelConceptExported()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("model_concept_exported")) {
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogModelSyncError(int sync)
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("model_sync_error")) {
                e.Param("ecosystem", m_CurrentModelEcosystem);
                e.Param("sync", sync);
            }
        }

        private void LogModelPredictCompleted()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("model_predict_completed")) {
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogModelInterveneUpdate(InterveneUpdateData data)
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("model_intervene_update")) {
                e.Param("ecosystem", m_CurrentModelEcosystem);
                e.Param("organism", data.Organism);
                e.Param("difference_value", data.DifferenceValue);
            }
        }

        private void LogModelInterveneError()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("model_intervene_error")) {
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogModelInterveneCompleted()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("model_intervene_completed")) {
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogEndModel()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("model_end")) {
                e.Param("phase", m_CurrentModelPhase);
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }

            m_CurrentModelPhase = string.Empty;
            m_CurrentModelEcosystem = string.Empty;
        }

        #endregion // Modeling Events

        #region Shop Events

        private void LogPurchaseUpgrade(StringHash32 inUpgradeId)
        {
            UpdateCurrencyInfo();
            InvItem item = Services.Assets.Inventory.Get(inUpgradeId);
            string name = item.name;

            if (name != "Cash" && name != "Exp")
            {
                int cost = item.CashCost();

                RefreshGameState();
                using(var e = m_Log.NewEvent("purchase_upgrade")) {
                    e.Param("item_id", inUpgradeId.ToString());
                    e.Param("item_name", name);
                    e.Param("cost", cost);
                }
            }
        }

        private void LogInsufficientFunds(StringHash32 inUpgradeId)
        {
            InvItem item = Services.Assets.Inventory.Get(inUpgradeId);
            string name = item.name;
            int cost = item.CashCost();

            RefreshGameState();
            using(var e = m_Log.NewEvent("insufficient_funds")) {
                e.Param("item_id", inUpgradeId.ToString());
                e.Param("item_name", name);
                e.Param("cost", cost);
            }
        }

        private void LogTalkToShopkeep()
        {
            RefreshGameState();
            using(var e = m_Log.NewEvent("talk_to_shopkeep")) {
            }
        }

        #endregion // Shop Events

        #region Experimentation Events

        private void SetCurrentTankType(TankType inTankType)
        {
            m_CurrentTankType = inTankType.ToString();
            m_CurrentCritters.Clear();
        }

        private void SetTankFeatureEnabled(MeasurementTank.FeatureMask feature)
        {
            if (feature == MeasurementTank.FeatureMask.Stabilizer)
            {
                m_StabilizerEnabled = true;
            }
            else
            {
                m_AutoFeederEnabled = true;
            }
        }

        private void SetTankFeatureDisabled(MeasurementTank.FeatureMask feature)
        {
            if (feature == MeasurementTank.FeatureMask.Stabilizer)
            {
                m_StabilizerEnabled = false;
            }
            else
            {
                m_AutoFeederEnabled = false;
            }
        }

        private void LogAddEnvironment(StringHash32 inEnvironmentId)
        {
            string environment = Services.Assets.Bestiary.Get(inEnvironmentId).name;
            m_CurrentEnvironment = environment;

            RefreshGameState();
            using(var e = m_Log.NewEvent("add_environment")) {
                e.Param("tank_type", m_CurrentTankType);
                e.Param("environment", environment);
            }
        }

        private void LogRemoveEnvironment(StringHash32 inEnvironmentId)
        {
            string environment = Services.Assets.Bestiary.Get(inEnvironmentId).ToString();
            m_CurrentEnvironment = string.Empty;

            RefreshGameState();
            using(var e = m_Log.NewEvent("remove_environment")) {
                e.Param("tank_type", m_CurrentTankType);
                e.Param("environment", environment);
            }
        }

        private void LogAddCritter(StringHash32 inCritterId)
        {
            string critter = Services.Assets.Bestiary.Get(inCritterId).name;
            m_CurrentCritters.Add(critter);

            RefreshGameState();
            using(var e = m_Log.NewEvent("add_critter")) {
                e.Param("tank_type", m_CurrentTankType);
                e.Param("environment", m_CurrentEnvironment);
                e.Param("critter", critter);
            }
        }

        private void LogRemoveCritter(StringHash32 inCritterId)
        {
            string critter = Services.Assets.Bestiary.Get(inCritterId).name;
            m_CurrentCritters.Remove(critter);

            RefreshGameState();
            using(var e = m_Log.NewEvent("remove_critter")) {
                e.Param("tank_type", m_CurrentTankType);
                e.Param("environment", m_CurrentEnvironment);
                e.Param("critter", critter);
            }
        }

        private void LogBeginExperiment(TankType inTankType)
        {
            string tankType = inTankType.ToString();
            string critters = String.Join(",", m_CurrentCritters.ToArray());

            RefreshGameState();
            using(var e = m_Log.NewEvent("begin_experiment")) {
                e.Param("tank_type", tankType);
                e.Param("environment", m_CurrentEnvironment);
                e.Param("critters", critters);
                e.Param("stabilizer_enabled", m_StabilizerEnabled);
                e.Param("autofeeder_enabled", m_AutoFeederEnabled);
            }
        }

        private void LogEndExperiment(TankType inTankType)
        {
            string tankType = inTankType.ToString();
            string critters = String.Join(",", m_CurrentCritters.ToArray());

            RefreshGameState();
            using(var e = m_Log.NewEvent("end_experiment")) {
                e.Param("tank_type", tankType);
                e.Param("environment", m_CurrentEnvironment);
                e.Param("critters", critters);
                e.Param("stabilizer_enabled", m_StabilizerEnabled);
                e.Param("autofeeder_enabled", m_AutoFeederEnabled);
            }

            m_CurrentTankType = string.Empty;
            m_CurrentEnvironment = string.Empty;
            m_CurrentCritters.Clear();
            m_StabilizerEnabled = false;
            m_AutoFeederEnabled = false;
        }

        #endregion Experimentation Events

        #region Argumentation Events

        private void LogBeginArgument(StringHash32 id)
        {
            m_CurrentArgumentId = id;

            RefreshGameState();
            using(var e = m_Log.NewEvent("begin_argument")) {
            }
        }

        private void LogFactSubmitted(StringHash32 inFactId)
        {
            string factId = Assets.Fact(inFactId).name;

            RefreshGameState();
            using(var e = m_Log.NewEvent("fact_submitted")) {
                e.Param("fact_id", factId);
            }
        }

        private void LogFactRejected(StringHash32 inFactId)
        {
            string factId = Assets.Fact(inFactId).name;

            RefreshGameState();
            using(var e = m_Log.NewEvent("fact_rejected")) {
                e.Param("fact_id", factId);
            }
        }

        private void LogLeaveArgument()
        {
            if (ArgumentationService.LeafIsComplete(m_CurrentArgumentId)) return;

            RefreshGameState();
            using(var e = m_Log.NewEvent("leave_argument")) {
            }
        }

        private void LogCompleteArgument(StringHash32 id)
        {
            UpdateCurrencyInfo();
            RefreshGameState();
            using(var e = m_Log.NewEvent("complete_argument")) {
            }
            
            m_CurrentArgumentId = null;
        }

        #endregion // Argumentation

        #region Errors

        private void OnCrash(Exception exception, string error) {
            RefreshGameState();
            
            string text = exception != null ? exception.Message : error;
            using (var e = m_Log.NewEvent("game_error")) {
                e.Param("error_message", text);
                e.Param("scene", SceneHelper.ActiveScene().Name);
                e.Param("time_since_launch", Time.realtimeSinceStartup, 2);

            }
            m_Log.Flush();
        }

        private void OnNetworkError(string url) {
            RefreshGameState();

            if (url.Length > 480) {
                url = url.Substring(0, 477) + "...";
            }
            using(var e = m_Log.NewEvent("load_error")) {
                e.Param("url", url);
            }
        }

        #endregion // Errors

        #endregion // Log Events

        #region Cache

        static private string AssetName(StringHash32 id) {
            return AssetNameCache.Read(id);
        }

        #endregion // Cache

        #region Debug

#if DEVELOPMENT

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus(FindOrCreateMenu findOrCreate) {
            DMInfo menu = findOrCreate("Logging");
            menu.AddToggle("Analytics Logging", () => {
                return m_Debug;
            }, (t) => {
                m_Debug = t;
                m_Log.SetDebug(t);
            });

            yield return menu;

            DMInfo research = findOrCreate("Research");
            research.AddSlider("Force AB Test Value", () => ResearchTests.s_DEBUGForceTest, (f) => {
                ResearchTests.s_DEBUGForceTest = (int) f;
                ResearchTests.DEBUGRefreshAllTests();
            }, -1, 2, 1, (f) => {
                int i = (int) f;
                if (i < 0) {
                    return "N/a";
                } else {
                    return char.ToString((char) ('A' + i));
                }
            });
            research.AddDivider();
            research.AddToggle("Always Predict Job Failure", JobPredictionFeature.DEBUG_IsAlwaysPredictFailure, JobPredictionFeature.DEBUG_SetAlwaysPredictFailure);

            DMInfo surveys = new DMInfo("Surveys");
            foreach(var data in m_Survey.CurrentPackage.Surveys) {
                AddSurveyButton(surveys, data);
            }

            research.AddDivider();
            research.AddSubmenu(surveys);

            yield return research;
        }

        private void AddSurveyButton(DMInfo menu, SurveyData survey) {
            menu.AddButton(survey.DisplayEventId, () => {
                m_Survey.TryDisplaySurvey(survey.DisplayEventId);
            });
        }

#endif // DEVELOPMENT

        #endregion // Debug
    }
}
