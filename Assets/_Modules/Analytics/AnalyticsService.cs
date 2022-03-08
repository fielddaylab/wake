#if UNITY_WEBGL && !UNITY_EDITOR
#define FIREBASE
#endif // UNITY_WEBGL && !UNITY_EDITOR

using Aqua.Argumentation;
using Aqua.Modeling;
using Aqua.Portable;
using Aqua.Scripting;
using Aqua.Shop;
using BeauUtil;
using BeauUtil.Services;
using ProtoAqua.ExperimentV2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Aqua
{
    [ServiceDependency(typeof(EventService), typeof(ScriptingService))]
    public partial class AnalyticsService : ServiceBehaviour
    {
        #region Inspector

        [SerializeField, Required] private string m_AppId = "AQUALAB";
        
        #endregion // Inspector

        #region Firebase JS Functions

        //Init
        [DllImport("__Internal")]
        public static extern void FBStartGameWithUserCode(string userCode);

        //Progression
        [DllImport("__Internal")]
        public static extern void FBAcceptJob(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBSwitchJob(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBReceiveFact(string userCode, int appVersion, int logVersion, int jobId, string jobName, string factId);
        [DllImport("__Internal")]
        public static extern void FBCompleteJob(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBCompleteTask(string userCode, int appVersion, int logVersion, int jobId, string jobName, string taskId);

        //Player Actions
        [DllImport("__Internal")]
        public static extern void FBSceneChanged(string userCode, int appVersion, int logVersion, int jobId, string jobName,string sceneName);
        [DllImport("__Internal")]
        public static extern void FBRoomChanged(string userCode, int appVersion, int logVersion, int jobId, string jobName, string roomName);
        [DllImport("__Internal")]
        public static extern void FBBeginExperiment(string userCode, int appVersion, int logVersion, int jobId, string jobName, string tankType, string environment, string critters);
        [DllImport("__Internal")]
        public static extern void FBBeginDive(string userCode, int appVersion, int logVersion, int jobId, string jobName, string siteId);
        [DllImport("__Internal")]
        public static extern void FBBeginModel(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBeginSimulation(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBAskForHelp(string userCode, int appVersion, int logVersion, int jobId, string jobName, string nodeId);
        [DllImport("__Internal")]
        public static extern void FBTalkWithGuide(string userCode, int appVersion, int logVersion, int jobId, string jobName, string nodeId);
        
        //Bestiary Events
        [DllImport("__Internal")]
        public static extern void FBOpenBestiary(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBestiaryOpenSpeciesTab(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBestiaryOpenEnvironmentsTab(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBestiaryOpenModelsTab(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBestiarySelectSpecies(string userCode, int appVersion, int logVersion, int jobId, string jobName, string speciesId);
        [DllImport("__Internal")]
        public static extern void FBBestiarySelectEnvironment(string userCode, int appVersion, int logVersion, int jobId, string jobName, string environmentId);
        [DllImport("__Internal")]
        public static extern void FBBestiarySelectModel(string userCode, int appVersion, int logVersion, int jobId, string jobName, string modelId);
        [DllImport("__Internal")]
        public static extern void FBCloseBestiary(string userCode, int appVersion, int logVersion, int jobId, string jobName);

        //Status Events
        [DllImport("__Internal")]
        public static extern void FBOpenStatus(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBStatusOpenJobTab(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBStatusOpenItemTab(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBStatusOpenTechTab(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBCloseStatus(string userCode, int appVersion, int logVersion, int jobId, string jobName);

        //Game Feedback
        [DllImport("__Internal")]
        public static extern void FBSimulationSyncAchieved(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBGuideScriptTriggered(string userCode, int appVersion, int logVersion, int jobId, string jobName, string nodeId);

        // Modeling Events
        [DllImport("__Internal")]
        public static extern void FBModelingStart(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBModelPhaseChanged(string userCode, int appVersion, int logVersion, int jobId, string jobName, string phase);
        [DllImport("__Internal")]
        public static extern void FBModelEcosystemSelected(string userCode, int appVersion, int logVersion, int jobId, string jobName, string ecosystem);
        [DllImport("__Internal")]
        public static extern void FBModelConceptStarted(string userCode, int appVersion, int logVersion, int jobId, string jobName, string ecosystem);
        [DllImport("__Internal")]
        public static extern void FBModelConceptUpdated(string userCode, int appVersion, int logVersion, int jobId, string jobName, string ecosystem, string status);
        [DllImport("__Internal")]
        public static extern void FBModelConceptExported(string userCode, int appVersion, int logVersion, int jobId, string jobName, string ecosystem);
        [DllImport("__Internal")]
        public static extern void FBModelSyncError(string userCode, int appVersion, int logVersion, int jobId, string jobName, string ecosystem, int sync);
        [DllImport("__Internal")]
        public static extern void FBModelPredictCompleted(string userCode, int appVersion, int logVersion, int jobId, string jobName, string ecosystem);
        [DllImport("__Internal")]
        public static extern void FBModelInterveneUpdate(string userCode, int appVersion, int logVersion, int jobId, string jobName, string ecosystem, string organism, int differenceValue);
        [DllImport("__Internal")]
        public static extern void FBModelInterveneError(string userCode, int appVersion, int logVersion, int jobId, string jobName, string ecosystem);
        [DllImport("__Internal")]
        public static extern void FBModelInterveneCompleted(string userCode, int appVersion, int logVersion, int jobId, string jobName, string ecosystem);
        [DllImport("__Internal")]
        public static extern void FBModelingEnd(string userCode, int appVersion, int logVersion, int jobId, string jobName, string phase, string ecosystem);

        // Shop Events
        [DllImport("__Internal")]
        public static extern void FBPurchaseUpgrade(string userCode, int appVersion, int logVersion, int jobId, string jobName, string itemId, string itemName, int cost);
        [DllImport("__Internal")]
        public static extern void FBInsufficientFunds(string userCode, int appVersion, int logVersion, int jobId, string jobName, string itemId, string itemName, int cost);
        [DllImport("__Internal")]
        public static extern void FBTalkToShopkeep(string userCode, int appVersion, int logVersion, int jobId, string jobName);

        // Experimentation Events
        [DllImport("__Internal")]
        public static extern void FBAddEnvironment(string userCode, int appVersion, int logVersion, int jobId, string jobName, string tankType, string environment);
        [DllImport("__Internal")]
        public static extern void FBRemoveEnvironment(string userCode, int appVersion, int logVersion, int jobId, string jobName, string tankType, string environment);
        [DllImport("__Internal")]
        public static extern void FBEnvironmentCleared(string userCode, int appVersion, int logVersion, int jobId, string jobName, string tankType, string environment);
        [DllImport("__Internal")]
        public static extern void FBAddCritter(string userCode, int appVersion, int logVersion, int jobId, string jobName, string tankType, string environment, string critter);
        [DllImport("__Internal")]
        public static extern void FBRemoveCritter(string userCode, int appVersion, int logVersion, int jobId, string jobName, string tankType, string environment, string critter);
        [DllImport("__Internal")]
        public static extern void FBCrittersCleared(string userCode, int appVersion, int logVersion, int jobId, string jobName, string tankType, string environment, string critters);

        // Argumentation Events
        [DllImport("__Internal")]
        public static extern void FBBeginArgument(string userCode, int appVersion, int logVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBFactSubmitted(string userCode, int appVersion, int logVersion, int jobId, string jobName, string factId);
        [DllImport("__Internal")]
        public static extern void FBFactRejected(string userCode, int appVersion, int logVersion, int jobId, string jobName, string factId);
        [DllImport("__Internal")]
        public static extern void FBCompleteArgument(string userCode, int appVersion, int logVersion, int jobId, string jobName);

        #endregion // Firebase JS Functions

        #region Logging Variables

        private string m_UserCode = string.Empty;
        private string m_AppVersion = string.Empty;
        private int m_LogVersion = 4;
        private int m_CurrentJobId = -1;
        private string m_CurrentJobName = "no-active-job";
        private string m_PreviousJobName = "no-active-job";
        private PortableAppId m_CurrentPortableAppId = PortableAppId.NULL;
        private BestiaryDescCategory? m_CurrentPortableBestiaryTabId = null;
        private string m_CurrentModelPhase = string.Empty;
        private string m_CurrentModelEcosystem = string.Empty;
        private string m_CurrentTankType = string.Empty;
        private string m_CurrentEnvironment = string.Empty;
        private List<string> m_CurrentCritters = new List<string>();

        #endregion // Logging Variables

        #region IService

        protected override void Initialize()
        {
            Services.Events.Register<StringHash32>(GameEvents.JobStarted, LogAcceptJob, this)
                .Register<string>(GameEvents.ProfileStarting, SetUserCode, this)
                .Register<StringHash32>(GameEvents.JobSwitched, LogSwitchJob, this)
                .Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, LogReceiveFact, this)
                .Register<StringHash32>(GameEvents.JobCompleted, LogCompleteJob, this)
                .Register<StringHash32>(GameEvents.JobTaskCompleted, LogCompleteTask, this)
                .Register<string>(GameEvents.RoomChanged, LogRoomChanged, this)
                .Register<TankType>(ExperimentEvents.ExperimentBegin, LogBeginExperiment, this)
                .Register<string>(GameEvents.BeginDive, LogBeginDive, this)
                .Register(ModelingConsts.Event_Simulation_Begin, LogBeginSimulation, this)
                .Register(ModelingConsts.Event_Simulation_Complete, LogSimulationSyncAchieved, this)
                .Register<string>(GameEvents.ProfileStarting, OnTitleStart, this)
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
                .Register<StringHash32>(ExperimentEvents.ExperimentAddEnvironment, LogAddEnvironment, this)
                .Register<StringHash32>(ExperimentEvents.ExperimentRemoveEnvironment, LogRemoveEnvironment, this)
                .Register(ExperimentEvents.ExperimentEnvironmentCleared, LogEnvironmentCleared, this)
                .Register<StringHash32>(ExperimentEvents.ExperimentAddCritter, LogAddCritter, this)
                .Register<StringHash32>(ExperimentEvents.ExperimentRemoveCritter, LogRemoveCritter, this)
                .Register(ExperimentEvents.ExperimentCrittersCleared, LogCrittersCleared, this)
                .Register<StringHash32>(ArgueEvents.Loaded, LogBeginArgument, this)
                .Register<StringHash32>(ArgueEvents.FactSubmitted, LogFactSubmitted, this)
                .Register<StringHash32>(ArgueEvents.FactRejected, LogFactRejected, this)
                .Register<StringHash32>(ArgueEvents.Completed, LogCompleteArgument,this);
                

            Services.Script.OnTargetedThreadStarted += GuideHandler;
            SceneHelper.OnSceneLoaded += LogSceneChanged;
        }

        private void SetUserCode(string userCode)
        {
            m_UserCode = userCode;
            m_AppVersion = BuildInfo.Id();
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
        private void OnTitleStart(string inUserCode)
        {
            if (!string.IsNullOrEmpty(inUserCode))
            {
                #if FIREBASE
                FBStartGameWithUserCode(inUserCode);
                #endif
            }
        }

        private void GuideHandler(ScriptThreadHandle inThread)
        {
            if (inThread.TargetId() != GameConsts.Target_Kevin)
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

            #if FIREBASE
            FBSceneChanged(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, sceneName);
            #endif
        }

        private void LogRoomChanged(string roomName)
        {
            #if FIREBASE
            FBRoomChanged(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, roomName);
            #endif
        }

        #region bestiary handlers
        private void PortableAppOpenedHandler(PortableAppId inId)
        {

            if (m_CurrentPortableAppId != inId)
                PortableAppClosedHandler(m_CurrentPortableAppId);

            m_CurrentPortableAppId = inId;
            switch(inId)
            {
                case PortableAppId.Environments:
                case PortableAppId.Organisms:
                    {
                        LogOpenBestiary();
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

        private void LogAcceptJob(StringHash32 jobId)
        {
            m_PreviousJobName = m_CurrentJobName;

            if (jobId.IsEmpty)
            {
                m_CurrentJobName = "no-active-job";
                m_CurrentJobId = -1;
            }
            else
            {
                m_CurrentJobName = Assets.Job(jobId).name;
                m_CurrentJobId = JobIds.IndexOf(jobId);

                if (m_CurrentJobId == -1) {
                    Debug.Log(String.Format("Analytics: Job {0} is not mapped to an id, sent id = -1 with log event.", m_CurrentJobName));
                }
            }

            #if FIREBASE
            FBAcceptJob(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogSwitchJob(StringHash32 jobId)
        {
            // ignore case where GameEvents.JobSwitched is dispatched when accepting a new job with no current one selected
            if (m_PreviousJobName.Equals("no-active-job")) return;

            m_PreviousJobName = m_CurrentJobName;

            if (jobId.IsEmpty)
            {
                m_CurrentJobName = "no-active-job";
                m_CurrentJobId = -1;
            }
            else
            {
                m_CurrentJobName = Assets.Job(jobId).name;
                m_CurrentJobId = JobIds.IndexOf(jobId);

                if (m_CurrentJobId == -1) {
                    Debug.Log(String.Format("Analytics: Job {0} is not mapped to an id, sent id = -1 with log event.", m_CurrentJobName));
                }
            }

            #if FIREBASE
            FBSwitchJob(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogReceiveFact(BestiaryUpdateParams inParams)
        {
            if (inParams.Type == BestiaryUpdateParams.UpdateType.Fact)
            {
                string parsedFactId = Assets.Fact(inParams.Id).name;

                #if FIREBASE
                FBReceiveFact(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, parsedFactId);
                #endif
            }
        }

        private void LogCompleteJob(StringHash32 jobId)
        {
            string parsedJobName = Assets.Job(jobId).name;

            #if FIREBASE
            FBCompleteJob(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, parsedJobName);
            #endif

            m_PreviousJobName = m_CurrentJobName;
            m_CurrentJobName = "no-active-job";
            m_CurrentJobId = -1;
        }

        private void LogCompleteTask(StringHash32 inTaskId)
        {
            string taskId = inTaskId.ToString();

            #if FIREBASE
            FBCompleteTask(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, taskId);
            #endif
        }

        private void LogBeginDive(string inTargetScene)
        {
            #if FIREBASE
            FBBeginDive(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, inTargetScene);
            #endif
        }

        private void LogBeginModel()
        {
            #if FIREBASE
            FBBeginModel(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogBeginSimulation()
        {
            #if FIREBASE
            FBBeginSimulation(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogAskForHelp(string nodeId)
        {
            #if FIREBASE
            FBAskForHelp(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, nodeId);
            #endif
        }

        private void LogTalkWithGuide(string nodeId)
        {
            #if FIREBASE
            FBTalkWithGuide(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, nodeId);
            #endif
        }

        #region Bestiary App Logging
        private void LogOpenBestiary()
        {
            m_CurrentPortableBestiaryTabId = BestiaryDescCategory.Critter;

            #if FIREBASE
            FBOpenBestiary(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif

            LogBestiaryOpenSpeciesTab(); //Bestiary starts by opening Critters tab
        }

        private void LogBestiaryOpenSpeciesTab()
        {
            #if FIREBASE
            FBBestiaryOpenSpeciesTab(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }
        private void LogBestiaryOpenEnvironmentsTab()
        {
            #if FIREBASE
            FBBestiaryOpenEnvironmentsTab(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }
        private void LogBestiaryOpenModelsTab()
        {
            #if FIREBASE
            FBBestiaryOpenModelsTab(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogBestiarySelectSpecies(string speciesId)
        {
            #if FIREBASE
            FBBestiarySelectSpecies(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, speciesId);
            #endif
        }
        private void LogBestiarySelectEnvironment(string environmentId)
        {
            #if FIREBASE
            FBBestiarySelectEnvironment(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, environmentId);
            #endif
        }
        private void LogBestiarySelectModel(string modelId)
        {
            #if FIREBASE
            FBBestiarySelectModel(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, modelId);
            #endif
        }
        private void LogCloseBestiary()
        {
            #if FIREBASE
            FBCloseBestiary(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }
        #endregion

        #region Status App Logging
        private void LogOpenStatus()
        {
            //m_CurrentPortableStatusTabId = StatusApp.PageId.Job;

            #if FIREBASE
            FBOpenStatus(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif

            LogStatusOpenJobTab(); //Status starts by opening tasks tab
        }

        private void LogStatusOpenJobTab()
        {
            #if FIREBASE
            FBStatusOpenJobTab(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogStatusOpenItemTab()
        {
            #if FIREBASE
            FBStatusOpenItemTab(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogStatusOpenTechTab()
        {
            #if FIREBASE
            FBStatusOpenTechTab(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogCloseStatus()
        {
            #if FIREBASE
            FBCloseStatus(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }
        #endregion

        private void LogSimulationSyncAchieved()
        {
            #if FIREBASE
            FBSimulationSyncAchieved(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogGuideScriptTriggered(string nodeId)
        {
            #if FIREBASE
            FBGuideScriptTriggered(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, nodeId);
            #endif
        }

        #region Modeling Events

        private void LogStartModel()
        {
            #if FIREBASE
            FBModelingStart(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogModelPhaseChanged(byte inPhase)
        {
            m_CurrentModelPhase = ((ModelPhases)inPhase).ToString();

            #if FIREBASE
            FBModelPhaseChanged(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelPhase);
            #endif
        }

        private void LogModelEcosystemSelected(string ecosystem)
        {
            m_CurrentModelEcosystem = ecosystem;

            #if FIREBASE
            FBModelEcosystemSelected(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelEcosystem);
            #endif
        }

        private void LogModelConceptStarted()
        {
            #if FIREBASE
            FBModelConceptStarted(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelEcosystem);
            #endif
        }

        private void LogModelConceptUpdated(ConceptualModelState.StatusId status)
        {
            #if FIREBASE
            FBModelConceptUpdated(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelEcosystem, status.ToString());
            #endif
        }

        private void LogModelConceptExported()
        {
            #if FIREBASE
            FBModelConceptExported(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelEcosystem);
            #endif
        }

        private void LogModelSyncError(int sync)
        {
            #if FIREBASE
            FBModelSyncError(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelEcosystem, sync);
            #endif
        }

        private void LogModelPredictCompleted()
        {
            #if FIREBASE
            FBModelPredictCompleted(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelEcosystem);
            #endif
        }

        private void LogModelInterveneUpdate(InterveneUpdateData data)
        {
            #if FIREBASE
            FBModelInterveneUpdate(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelEcosystem, data.Organism, data.DifferenceValue);
            #endif
        }

        private void LogModelInterveneError()
        {
            #if FIREBASE
            FBModelInterveneError(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelEcosystem);
            #endif
        }

        private void LogModelInterveneCompleted()
        {
            #if FIREBASE
            FBModelInterveneCompleted(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelEcosystem);
            #endif
        }

        private void LogEndModel()
        {
            #if FIREBASE
            FBModelingEnd(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentModelPhase, m_CurrentModelEcosystem);
            #endif

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

                #if FIREBASE
                FBPurchaseUpgrade(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, inUpgradeId.ToString(), name, cost);
                #endif
            }
        }

        private void LogInsufficientFunds(StringHash32 inUpgradeId)
        {
            InvItem item = Services.Assets.Inventory.Get(inUpgradeId);
            string name = item.name;
            int cost = item.CashCost();

            #if FIREBASE
            FBInsufficientFunds(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, inUpgradeId.ToString(), name, cost);
            #endif
        }

        private void LogTalkToShopkeep()
        {
            #if FIREBASE
            FBTalkToShopkeep(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        #endregion // Shop Events

        #region Experimentation Events

        private void SetCurrentTankType(TankType inTankType)
        {
            m_CurrentTankType = inTankType.ToString();
        }

        private void LogAddEnvironment(StringHash32 inEnvironmentId)
        {
            string environment = Services.Assets.Bestiary.Get(inEnvironmentId).name;
            m_CurrentEnvironment = environment;

            #if FIREBASE
            FBAddEnvironment(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentTankType, environment);
            #endif
        }

        private void LogRemoveEnvironment(StringHash32 inEnvironmentId)
        {
            string environment = Services.Assets.Bestiary.Get(inEnvironmentId).ToString();
            m_CurrentEnvironment = string.Empty;

            #if FIREBASE
            FBRemoveEnvironment(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentTankType, environment);
            #endif
        }

        private void LogEnvironmentCleared()
        {
            m_CurrentEnvironment = string.Empty;

            #if FIREBASE
            FBEnvironmentCleared(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentTankType);
            #endif
        }

        private void LogAddCritter(StringHash32 inCritterId)
        {
            string critter = Services.Assets.Bestiary.Get(inCritterId).name;
            m_CurrentCritters.Add(critter);

            #if FIREBASE
            FBAddCritter(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentTankType, m_CurrentEnvironment, critter);
            #endif
        }

        private void LogRemoveCritter(StringHash32 inCritterId)
        {
            string critter = Services.Assets.Bestiary.Get(inCritterId).name;
            m_CurrentCritters.Remove(critter);

            #if FIREBASE
            FBRemoveCritter(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentTankType, m_CurrentEnvironment, critter);
            #endif
        }

        private void LogCrittersCleared()
        {
            string critters = String.Join(",", m_CurrentCritters.ToArray());
            m_CurrentCritters.Clear();

            #if FIREBASE
            FBCrittersCleared(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, m_CurrentTankType, m_CurrentEnvironment, critters);
            #endif
        }

        private void LogBeginExperiment(TankType inTankType)
        {
            string tankType = inTankType.ToString();
            string critters = String.Join(",", m_CurrentCritters.ToArray());

            #if FIREBASE
            FBBeginExperiment(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, tankType, m_CurrentEnvironment, critters);
            #endif
        }

        #endregion Experimentation Events

        #region Argumentation Events

        private void LogBeginArgument(StringHash32 id)
        {
            #if FIREBASE
            FBBeginArgument(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogFactSubmitted(StringHash32 inFactId)
        {
            string factId = Assets.Fact(inFactId).name;

            #if FIREBASE
            FBFactSubmitted(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, factId);
            #endif
        }

        private void LogFactRejected(StringHash32 inFactId)
        {
            string factId = Assets.Fact(inFactId).name;
            
            #if FIREBASE
            FBFactRejected(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName, factId);
            #endif
        }

        private void LogCompleteArgument(StringHash32 id)
        {
            #if FIREBASE
            FBCompleteArgument(m_UserCode, m_AppVersion, m_LogVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        #endregion // Argumentation

        #endregion // Log Events
    }
}
