#if UNITY_WEBGL && !UNITY_EDITOR
#define FIREBASE
#endif // UNITY_WEBGL && !UNITY_EDITOR

using Aqua.Portable;
using Aqua.Scripting;
using BeauUtil;
using BeauUtil.Services;
using FieldDay;
using ProtoAqua.ExperimentV2;
using ProtoAqua.Modeling;
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
        [SerializeField] private int m_AppVersion = 2;
        
        #endregion // Inspector

        #region Firebase JS Functions

        //Init
        [DllImport("__Internal")]
        public static extern void FBStartGameWithUserCode(string userCode);

        //Progression
        [DllImport("__Internal")]
        public static extern void FBAcceptJob(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBSwitchJob(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBReceiveFact(string userCode, int appVersion, int jobId, string jobName, string factId);
        [DllImport("__Internal")]
        public static extern void FBCompleteJob(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBCompleteTask(string userCode, int appVersion, int jobId, string jobName, string taskId);

        //Player Actions
        [DllImport("__Internal")]
        public static extern void FBSceneChanged(string userCode, int appVersion, int jobId, string jobName,string sceneName);
        [DllImport("__Internal")]
        public static extern void FBRoomChanged(string userCode, int appVersion, int jobId, string jobName, string roomName);
        [DllImport("__Internal")]
        public static extern void FBBeginExperiment(string userCode, int appVersion, int jobId, string jobName, string tankType);
        [DllImport("__Internal")]
        public static extern void FBBeginDive(string userCode, int appVersion, int jobId, string jobName, string siteId);
        [DllImport("__Internal")]
        public static extern void FBBeginArgument(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBeginModel(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBeginSimulation(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBAskForHelp(string userCode, int appVersion, int jobId, string jobName, string nodeId);
        [DllImport("__Internal")]
        public static extern void FBTalkWithGuide(string userCode, int appVersion, int jobId, string jobName, string nodeId);
            //Bestiary Events
        [DllImport("__Internal")]
        public static extern void FBOpenBestiary(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBestiaryOpenSpeciesTab(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBestiaryOpenEnvironmentsTab(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBestiaryOpenModelsTab(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBBestiarySelectSpecies(string userCode, int appVersion, int jobId, string jobName, string speciesId);
        [DllImport("__Internal")]
        public static extern void FBBestiarySelectEnvironment(string userCode, int appVersion, int jobId, string jobName, string environmentId);
        [DllImport("__Internal")]
        public static extern void FBBestiarySelectModel(string userCode, int appVersion, int jobId, string jobName, string modelId);
        [DllImport("__Internal")]
        public static extern void FBCloseBestiary(string userCode, int appVersion, int jobId, string jobName);

            //Status Events
        [DllImport("__Internal")]
        public static extern void FBOpenStatus(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBStatusOpenJobTab(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBStatusOpenItemTab(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBStatusOpenTechTab(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBCloseStatus(string userCode, int appVersion, int jobId, string jobName);

        //Game Feedback
        [DllImport("__Internal")]
        public static extern void FBSimulationSyncAchieved(string userCode, int appVersion, int jobId, string jobName);
        [DllImport("__Internal")]
        public static extern void FBGuideScriptTriggered(string userCode, int appVersion, int jobId, string jobName, string nodeId);

        #endregion // Firebase JS Functions

        #region Logging Variables

        private string m_UserCode = string.Empty;
        private int m_CurrentJobId = -1;
        private string m_CurrentJobName = "no-active-job";
        private string m_PreviousJobName = "no-active-job";
        private PortableAppId m_CurrentPortableAppId = PortableAppId.NULL;
        private BestiaryDescCategory? m_CurrentPortableBestiaryTabId = null;

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
                .Register(GameEvents.BeginArgument, LogBeginArgument, this)
                .Register(SimulationConsts.Event_Model_Begin, LogBeginModel, this)
                .Register(SimulationConsts.Event_Simulation_Begin, LogBeginSimulation, this)
                .Register(SimulationConsts.Event_Simulation_Complete, LogSimulationSyncAchieved, this)
                .Register<string>(GameEvents.ProfileStarting, OnTitleStart, this)
                .Register<PortableAppId>(GameEvents.PortableAppOpened, PortableAppOpenedHandler, this)
                .Register<PortableAppId>(GameEvents.PortableAppClosed, PortableAppClosedHandler, this)
                // .Register<BestiaryDescCategory>(GameEvents.PortableBestiaryTabSelected, PortableBestiaryTabSelectedHandler, this)
                .Register<BestiaryDesc> (GameEvents.PortableEntrySelected, PortableBestiaryEntrySelectedhandler, this)
                .Register(GameEvents.ScenePreloading, ClearSceneState, this)
                .Register(GameEvents.PortableClosed, PortableClosed, this);

            Services.Script.OnTargetedThreadStarted += GuideHandler;
            SceneHelper.OnSceneLoaded += LogSceneChanged;
        }

        private void SetUserCode(string userCode)
        {
            m_UserCode = userCode;
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
            FBSceneChanged(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, sceneName);
            #endif
        }

        private void LogRoomChanged(string roomName)
        {
            #if FIREBASE
            FBRoomChanged(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, roomName);
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
            FBAcceptJob(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
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
            FBSwitchJob(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogReceiveFact(BestiaryUpdateParams inParams)
        {
            if (inParams.Type == BestiaryUpdateParams.UpdateType.Fact)
            {
                string parsedFactId = Assets.Fact(inParams.Id).name;

                #if FIREBASE
                FBReceiveFact(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, parsedFactId);
                #endif
            }
        }

        private void LogCompleteJob(StringHash32 jobId)
        {
            string parsedJobName = Assets.Job(jobId).name;

            #if FIREBASE
            FBCompleteJob(m_UserCode, m_AppVersion, m_CurrentJobId, parsedJobName);
            #endif

            m_PreviousJobName = m_CurrentJobName;
            m_CurrentJobName = "no-active-job";
            m_CurrentJobId = -1;
        }

        private void LogCompleteTask(StringHash32 inTaskId)
        {
            string taskId = inTaskId.ToString();

            #if FIREBASE
            FBCompleteTask(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, taskId);
            #endif
        }

        private void LogBeginExperiment(TankType inTankType)
        {
            string tankType = inTankType.ToString();

            #if FIREBASE
            FBBeginExperiment(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName,  tankType);
            #endif
        }

        private void LogBeginDive(string inTargetScene)
        {
            #if FIREBASE
            FBBeginDive(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, inTargetScene);
            #endif
        }

        private void LogBeginArgument()
        {
            #if FIREBASE
            FBBeginArgument(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogBeginModel()
        {
            #if FIREBASE
            FBBeginModel(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogBeginSimulation()
        {
            #if FIREBASE
            FBBeginSimulation(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogAskForHelp(string nodeId)
        {
            #if FIREBASE
            FBAskForHelp(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, nodeId);
            #endif
        }

        private void LogTalkWithGuide(string nodeId)
        {
            #if FIREBASE
            FBTalkWithGuide(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, nodeId);
            #endif
        }

        #region Bestiary App Logging
        private void LogOpenBestiary()
        {
            m_CurrentPortableBestiaryTabId = BestiaryDescCategory.Critter;

            #if FIREBASE
            FBOpenBestiary(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif

            LogBestiaryOpenSpeciesTab(); //Bestiary starts by opening Critters tab
        }

        private void LogBestiaryOpenSpeciesTab()
        {
            #if FIREBASE
            FBBestiaryOpenSpeciesTab(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }
        private void LogBestiaryOpenEnvironmentsTab()
        {
            #if FIREBASE
            FBBestiaryOpenEnvironmentsTab(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }
        private void LogBestiaryOpenModelsTab()
        {
            #if FIREBASE
            FBBestiaryOpenModelsTab(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogBestiarySelectSpecies(string speciesId)
        {
            #if FIREBASE
            FBBestiarySelectSpecies(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, speciesId);
            #endif
        }
        private void LogBestiarySelectEnvironment(string environmentId)
        {
            #if FIREBASE
            FBBestiarySelectEnvironment(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, environmentId);
            #endif
        }
        private void LogBestiarySelectModel(string modelId)
        {
            #if FIREBASE
            FBBestiarySelectModel(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, modelId);
            #endif
        }
        private void LogCloseBestiary()
        {
            #if FIREBASE
            FBCloseBestiary(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }
        #endregion

        #region Status App Logging
        private void LogOpenStatus()
        {
            //m_CurrentPortableStatusTabId = StatusApp.PageId.Job;

            #if FIREBASE
            FBOpenStatus(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif

            LogStatusOpenJobTab(); //Status starts by opening tasks tab
        }

        private void LogStatusOpenJobTab()
        {
            #if FIREBASE
            FBStatusOpenJobTab(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogStatusOpenItemTab()
        {
            #if FIREBASE
            FBStatusOpenItemTab(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogStatusOpenTechTab()
        {
            #if FIREBASE
            FBStatusOpenTechTab(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogCloseStatus()
        {
            #if FIREBASE
            FBCloseStatus(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }
        #endregion

        private void LogSimulationSyncAchieved()
        {
            #if FIREBASE
            FBSimulationSyncAchieved(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName);
            #endif
        }

        private void LogGuideScriptTriggered(string nodeId)
        {
            #if FIREBASE
            FBGuideScriptTriggered(m_UserCode, m_AppVersion, m_CurrentJobId, m_CurrentJobName, nodeId);
            #endif
        }

        #endregion // Log Events
    }
}
