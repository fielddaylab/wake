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
using FieldDay;
using BeauUtil.Debugger;
using Aqua.Debugging;

namespace Aqua
{
    [ServiceDependency(typeof(EventService), typeof(ScriptingService))]
    public partial class AnalyticsService : ServiceBehaviour, IDebuggable
    {
        private const string NoActiveJobId = "no-active-job";

        static private readonly string[] FactTypeStringTable = Enum.GetNames(typeof(BFTypeId));

        #region Inspector

        [SerializeField, Required] private string m_AppId = "AQUALAB";
        [SerializeField, Required] private string m_AppVersion = "6.2";
        [SerializeField] private FirebaseConsts m_Firebase = default(FirebaseConsts);
        
        #endregion // Inspector

        #region Logging Variables

        private OGDLog m_Log;

        [NonSerialized] private StringHash32 m_CurrentJobHash = null;
        [NonSerialized] private string m_CurrentJobName = NoActiveJobId;
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

        #endregion // Logging Variables

        #region IService

        protected override void Initialize()
        {
            Services.Events.Register<StringHash32>(GameEvents.JobStarted, LogAcceptJob, this)
                .Register<string>(GameEvents.ProfileStarting, SetUserCode, this)
                .Register(GameEvents.ProfileStarted, OnProfileStarted)
                .Register<StringHash32>(GameEvents.JobSwitched, LogSwitchJob, this)
                .Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, HandleBestiaryUpdated, this)
                .Register<StringHash32>(GameEvents.JobCompleted, LogCompleteJob, this)
                .Register<StringHash32>(GameEvents.JobTaskCompleted, LogCompleteTask, this)
                .Register<string>(GameEvents.ViewChanged, LogRoomChanged, this)
                .Register<string>(GameEvents.ScriptFired, LogScriptFired, this)
                .Register<DialogPanel.TextDisplayArgs>(GameEvents.TextLineDisplayed, LogScriptLine, this)
                .Register<TankType>(ExperimentEvents.ExperimentBegin, LogBeginExperiment, this)
                .Register<string>(GameEvents.BeginDive, LogBeginDive, this)
                .Register(ModelingConsts.Event_Simulation_Begin, LogBeginSimulation, this)
                .Register(ModelingConsts.Event_Simulation_Complete, LogSimulationSyncAchieved, this)
                .Register<PortableAppId>(GameEvents.PortableAppOpened, PortableAppOpenedHandler, this)
                .Register<PortableAppId>(GameEvents.PortableAppClosed, PortableAppClosedHandler, this)
                // .Register<BestiaryDescCategory>(GameEvents.PortableBestiaryTabSelected, PortableBestiaryTabSelectedHandler, this)
                .Register(ModelingConsts.Event_Begin_Model, LogBeginModel, this)
                .Register<byte>(ModelingConsts.Event_Phase_Changed, LogModelPhaseChanged, this)
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
                .Register<BestiaryDesc> (GameEvents.PortableEntrySelected, PortableBestiaryEntrySelectedhandler, this)
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
                .Register<StringHash32>(ArgueEvents.Completed, LogCompleteArgument,this);
                

            Services.Script.OnTargetedThreadStarted += GuideHandler;
            SceneHelper.OnSceneLoaded += LogSceneChanged;

            CrashHandler.OnCrash += OnCrash;

            NetworkStats.OnError.Register(OnNetworkError);

            m_Log = new OGDLog(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = 3
            });
            m_Log.UseFirebase(m_Firebase);

            #if DEVELOPMENT && !UNITY_EDITOR
            m_Debug = true;
            #endif // DEVELOPMENT && !UNITY_EDITOR

            m_Log.SetDebug(m_Debug);
        }

        private void SetUserCode(string userCode)
        {
            m_Log.Initialize(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = 3,
                AppBranch = BuildInfo.Branch()
            });
            m_Log.SetUserId(userCode);
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);
        }
        #endregion // IService

        private void ClearSceneState()
        {
            m_CurrentPortableAppId = PortableAppId.NULL;
            m_CurrentPortableBestiaryTabId = null;
        }

        #region Log Events

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

        private void OnCrash(Exception exception, string error) {
            string text = exception != null ? exception.Message : error;
            using(var e = m_Log.NewEvent("game_error")) {
                e.Param("error_message", text);
                e.Param("scene", SceneHelper.ActiveScene().Name);
                e.Param("time_since_launch", Time.realtimeSinceStartup, 2);
                e.Param("job_name", m_CurrentJobName);
            }
            m_Log.Flush();
        }

        private void LogSceneChanged(SceneBinding scene, object context)
        {
            string sceneName = scene.Name;

            if (sceneName != "Boot" && sceneName != "Title")
            {
                using(var e = m_Log.NewEvent("scene_changed")) {
                    e.Param("job_name", m_CurrentJobName);
                    e.Param("scene_name", sceneName);
                }
            }
        }

        private void LogRoomChanged(string roomName)
        {
            using(var e = m_Log.NewEvent("room_changed")) {
                e.Param("job_name", m_CurrentJobName);
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
            SetCurrentJob(Save.CurrentJobId);
        }

        private bool SetCurrentJob(StringHash32 jobId)
        {
            m_CurrentJobHash = jobId;
            m_PreviousJobName = m_CurrentJobName;

            if (jobId.IsEmpty)
            {
                m_CurrentJobName = NoActiveJobId;
            }
            else
            {
                m_CurrentJobName = Assets.Job(jobId).name;
                if (m_PreviousJobName != NoActiveJobId)
                {
                    return true;
                }
            }

            return false;
        }

        private void LogAcceptJob(StringHash32 jobId)
        {
            using(var e = m_Log.NewEvent("accept_job")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogSwitchJob(StringHash32 jobId)
        {
            SetCurrentJob(jobId);

            using(var e = m_Log.NewEvent("switch_job")) {
                e.Param("job_name", m_CurrentJobName);
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

            if (inParams.Type == BestiaryUpdateParams.UpdateType.Fact)
            {
                BFBase fact = Assets.Fact(inParams.Id);
                
                using(var e = m_Log.NewEvent("receive_fact")) {
                    e.Param("job_name", m_CurrentJobName);
                    AddFactDetails(e, fact);
                }
            }
            else if (inParams.Type == BestiaryUpdateParams.UpdateType.UpgradeFact)
            {
                BFBase fact = Assets.Fact(inParams.Id);
                
                using(var e = m_Log.NewEvent("upgrade_fact")) {
                    e.Param("job_name", m_CurrentJobName);
                    AddFactDetails(e, fact);
                }
            }
            else if (inParams.Type == BestiaryUpdateParams.UpdateType.Entity)
            {
                string parsedEntityId = Assets.Bestiary(inParams.Id).name;

                using(var e = m_Log.NewEvent("receive_entity")) {
                    e.Param("job_name", m_CurrentJobName);
                    e.Param("entity_id", parsedEntityId);
                }
            }
        }

        private void LogCompleteJob(StringHash32 jobId)
        {
            string parsedJobName = Assets.Job(jobId).name;

            using(var e = m_Log.NewEvent("complete_job")) {
                e.Param("job_name", parsedJobName);
            }
        }

        private void LogCompleteTask(StringHash32 inTaskId)
        {
            string taskId = Assets.Job(m_CurrentJobHash).Task(inTaskId).IdString;

            using(var e = m_Log.NewEvent("complete_task")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("task_id", taskId);
            }
        }

        private void LogBeginDive(string inTargetScene)
        {
            using(var e = m_Log.NewEvent("begin_dive")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("site_id", inTargetScene);
            }
        }

        private void LogBeginModel()
        {
            using(var e = m_Log.NewEvent("begin_model")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogBeginSimulation()
        {
            using(var e = m_Log.NewEvent("begin_simulation")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogAskForHelp(string nodeId)
        {
            using(var e = m_Log.NewEvent("ask_for_help")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("node_id", nodeId);
            }
        }

        private void LogTalkWithGuide(string nodeId)
        {
            using(var e = m_Log.NewEvent("talk_with_guide")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("node_id", nodeId);
            }
        }

        #region Bestiary App Logging
        private void LogOpenBestiaryOrganisms()
        {
            m_CurrentPortableBestiaryTabId = BestiaryDescCategory.Critter;

            using(var e = m_Log.NewEvent("open_bestiary")) {
                e.Param("job_name", m_CurrentJobName);
            }
            LogBestiaryOpenSpeciesTab();
        }

        private void LogOpenBestiaryEnvironments()
        {
            m_CurrentPortableBestiaryTabId = BestiaryDescCategory.Environment;

            using(var e = m_Log.NewEvent("open_bestiary")) {
                e.Param("job_name", m_CurrentJobName);
            }
            LogBestiaryOpenEnvironmentsTab();
        }

        private void LogBestiaryOpenSpeciesTab()
        {
            using(var e = m_Log.NewEvent("bestiary_open_species_tab")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }
        private void LogBestiaryOpenEnvironmentsTab()
        {
            using(var e = m_Log.NewEvent("bestiary_open_environments_tab")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }
        private void LogBestiaryOpenModelsTab()
        {
            using(var e = m_Log.NewEvent("bestiary_open_models_tab")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogBestiarySelectSpecies(string speciesId)
        {
            using(var e = m_Log.NewEvent("bestiary_select_species")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("species_id", speciesId);
            }
        }
        private void LogBestiarySelectEnvironment(string environmentId)
        {
            using(var e = m_Log.NewEvent("bestiary_select_environment")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("environment_id", environmentId);
            }
        }
        private void LogBestiarySelectModel(string modelId)
        {
            using(var e = m_Log.NewEvent("bestiary_select_model")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("model_id", modelId);
            }
        }
        private void LogCloseBestiary()
        {
            using(var e = m_Log.NewEvent("close_bestiary")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }
        #endregion

        #region Status App Logging
        private void LogOpenStatus()
        {
            using(var e = m_Log.NewEvent("open_status")) {
                e.Param("job_name", m_CurrentJobName);
            }

            LogStatusOpenJobTab(); //Status starts by opening tasks tab
        }

        private void LogStatusOpenJobTab()
        {
            using(var e = m_Log.NewEvent("status_open_job_tab")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogStatusOpenItemTab()
        {
            using(var e = m_Log.NewEvent("status_open_item_tab")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogStatusOpenTechTab()
        {
            using(var e = m_Log.NewEvent("status_open_tech_tab")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogCloseStatus()
        {
            using(var e = m_Log.NewEvent("close_status")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }
        #endregion

        private void LogSimulationSyncAchieved()
        {
            using(var e = m_Log.NewEvent("simulation_sync_achieved")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogGuideScriptTriggered(string nodeId)
        {
            using(var e = m_Log.NewEvent("guide_script_triggered")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("node_id", nodeId);
            }
        }

        private void LogScriptFired(string nodeId)
        {
            using(var e = m_Log.NewEvent("script_fired")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("node_id", nodeId);
            }
        }

        private void LogScriptLine(DialogPanel.TextDisplayArgs args)
        {
            using(var e = m_Log.NewEvent("script_line_displayed")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("text_string", args.VisibleText);
                e.Param("node_id", args.NodeId);
            }
        }

        #region Modeling Events

        private void LogStartModel()
        {
            using(var e = m_Log.NewEvent("model_start")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogModelPhaseChanged(byte inPhase)
        {
            m_CurrentModelPhase = ((ModelPhases)inPhase).ToString();

            using(var e = m_Log.NewEvent("model_phase_changed")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("phase", m_CurrentModelPhase);
            }
        }

        private void LogModelEcosystemSelected(string ecosystem)
        {
            m_CurrentModelEcosystem = ecosystem;

            using(var e = m_Log.NewEvent("model_ecosystem_selected")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogModelConceptStarted()
        {
            using(var e = m_Log.NewEvent("model_concept_started")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogModelConceptUpdated(ConceptualModelState.StatusId status)
        {
            using(var e = m_Log.NewEvent("model_concept_updated")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("ecosystem", m_CurrentModelEcosystem);
                e.Param("status", status.ToString());
            }
        }

        private void LogModelConceptExported()
        {
            using(var e = m_Log.NewEvent("model_concept_exported")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogModelSyncError(int sync)
        {
            using(var e = m_Log.NewEvent("model_sync_error")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("ecosystem", m_CurrentModelEcosystem);
                e.Param("sync", sync);
            }
        }

        private void LogModelPredictCompleted()
        {
            using(var e = m_Log.NewEvent("model_predict_completed")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogModelInterveneUpdate(InterveneUpdateData data)
        {
            using(var e = m_Log.NewEvent("model_intervene_update")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("ecosystem", m_CurrentModelEcosystem);
                e.Param("organism", data.Organism);
                e.Param("difference_value", data.DifferenceValue);
            }
        }

        private void LogModelInterveneError()
        {
            using(var e = m_Log.NewEvent("model_intervene_error")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogModelInterveneCompleted()
        {
            using(var e = m_Log.NewEvent("model_intervene_completed")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("ecosystem", m_CurrentModelEcosystem);
            }
        }

        private void LogEndModel()
        {
            using(var e = m_Log.NewEvent("model_end")) {
                e.Param("job_name", m_CurrentJobName);
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
            InvItem item = Services.Assets.Inventory.Get(inUpgradeId);
            string name = item.name;

            if (name != "Cash" && name != "Exp")
            {
                int cost = item.CashCost();

                using(var e = m_Log.NewEvent("purchase_upgrade")) {
                    e.Param("job_name", m_CurrentJobName);
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

            using(var e = m_Log.NewEvent("insufficient_funds")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("item_id", inUpgradeId.ToString());
                e.Param("item_name", name);
                e.Param("cost", cost);
            }
        }

        private void LogTalkToShopkeep()
        {
            using(var e = m_Log.NewEvent("talk_to_shopkeep")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        #endregion // Shop Events

        #region Experimentation Events

        private void SetCurrentTankType(TankType inTankType)
        {
            m_CurrentTankType = inTankType.ToString();
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

            using(var e = m_Log.NewEvent("add_environment")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("tank_type", m_CurrentTankType);
                e.Param("environment", environment);
            }
        }

        private void LogRemoveEnvironment(StringHash32 inEnvironmentId)
        {
            string environment = Services.Assets.Bestiary.Get(inEnvironmentId).ToString();
            m_CurrentEnvironment = string.Empty;

            using(var e = m_Log.NewEvent("remove_environment")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("tank_type", m_CurrentTankType);
                e.Param("environment", environment);
            }
        }

        private void LogAddCritter(StringHash32 inCritterId)
        {
            string critter = Services.Assets.Bestiary.Get(inCritterId).name;
            m_CurrentCritters.Add(critter);

            using(var e = m_Log.NewEvent("add_critter")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("tank_type", m_CurrentTankType);
                e.Param("environment", m_CurrentEnvironment);
                e.Param("critter", critter);
            }
        }

        private void LogRemoveCritter(StringHash32 inCritterId)
        {
            string critter = Services.Assets.Bestiary.Get(inCritterId).name;
            m_CurrentCritters.Remove(critter);

            using(var e = m_Log.NewEvent("remove_critter")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("tank_type", m_CurrentTankType);
                e.Param("environment", m_CurrentEnvironment);
                e.Param("critter", critter);
            }
        }

        private void LogBeginExperiment(TankType inTankType)
        {
            string tankType = inTankType.ToString();
            string critters = String.Join(",", m_CurrentCritters.ToArray());

            using(var e = m_Log.NewEvent("begin_experiment")) {
                e.Param("job_name", m_CurrentJobName);
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

            using(var e = m_Log.NewEvent("end_experiment")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("tank_type", tankType);
                e.Param("environment", m_CurrentEnvironment);
                e.Param("critters", critters);
                e.Param("stabilizer_enabled", m_StabilizerEnabled);
                e.Param("autofeeder_enabled", m_AutoFeederEnabled);
            }

            m_CurrentTankType = string.Empty;
            m_CurrentEnvironment = string.Empty;
            m_CurrentCritters = new List<string>();
            m_StabilizerEnabled = false;
            m_AutoFeederEnabled = false;
        }

        #endregion Experimentation Events

        #region Argumentation Events

        private void LogBeginArgument(StringHash32 id)
        {
            m_CurrentArgumentId = id;

            using(var e = m_Log.NewEvent("begin_argument")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogFactSubmitted(StringHash32 inFactId)
        {
            string factId = Assets.Fact(inFactId).name;

            using(var e = m_Log.NewEvent("fact_submitted")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("fact_id", factId);
            }
        }

        private void LogFactRejected(StringHash32 inFactId)
        {
            string factId = Assets.Fact(inFactId).name;

            using(var e = m_Log.NewEvent("fact_rejected")) {
                e.Param("job_name", m_CurrentJobName);
                e.Param("fact_id", factId);
            }
        }

        private void LogLeaveArgument()
        {
            if (ArgumentationService.LeafIsComplete(m_CurrentArgumentId)) return;

            using(var e = m_Log.NewEvent("leave_argument")) {
                e.Param("job_name", m_CurrentJobName);
            }
        }

        private void LogCompleteArgument(StringHash32 id)
        {
            using(var e = m_Log.NewEvent("complete_argument")) {
                e.Param("job_name", m_CurrentJobName);
            }
            
            m_CurrentArgumentId = null;
        }

        #endregion // Argumentation

        private void OnNetworkError(string url) {
            if (url.Length > 480) {
                url = url.Substring(0, 477) + "...";
            }
            using(var e = m_Log.NewEvent("load_error")) {
                e.Param("url", url);
            }
        }

        #endregion // Log Events

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
        }

        #endif // DEVELOPMENT
    }
}
