using Aqua.Portable;
using Aqua.Scripting;
using BeauUtil;
using BeauUtil.Services;
using FieldDay;
using ProtoAqua.Experiment;
using ProtoAqua.Modeling;
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
        [SerializeField] private int m_AppVersion = 1;
        
        #endregion // Inspector

        #region Firebase JS Functions

        //Init
        [DllImport("__Internal")]
        public static extern void FBStartGameWithUserCode(string userCode);

        //Progression
        [DllImport("__Internal")]
        public static extern void FBAcceptJob(string jobId);
        [DllImport("__Internal")]
        public static extern void FBSwitchJob(string jobId);
        [DllImport("__Internal")]
        public static extern void FBReceiveFact(string factId);
        [DllImport("__Internal")]
        public static extern void FBCompleteJob(string jobId);
        [DllImport("__Internal")]
        public static extern void FBCompleteTask(string factId, string taskId);

        //Player Actions
        [DllImport("__Internal")]
        public static extern void FBSceneChanged(string sceneName);
        [DllImport("__Internal")]
        public static extern void FBRoomChanged(string roomName);
        [DllImport("__Internal")]
        public static extern void FBBeginExperiment(string jobId, string tankType);
        [DllImport("__Internal")]
        public static extern void FBBeginDive(string jobId, string siteId);
        [DllImport("__Internal")]
        public static extern void FBBeginArgument(string jobId);
        [DllImport("__Internal")]
        public static extern void FBBeginModel(string jobId);
        [DllImport("__Internal")]
        public static extern void FBBeginSimulation(string jobId);
        [DllImport("__Internal")]
        public static extern void FBAskForHelp(string nodeId);
        [DllImport("__Internal")]
        public static extern void FBTalkWithGuide(string nodeId);
            //Bestiary Events
        [DllImport("__Internal")]
        public static extern void FBOpenBestiary(string jobId);
        [DllImport("__Internal")]
        public static extern void FBBestiaryOpenSpeciesTab(string jobId);
        [DllImport("__Internal")]
        public static extern void FBBestiaryOpenEnvironmentsTab(string jobId);
        [DllImport("__Internal")]
        public static extern void FBBestiaryOpenModelsTab(string jobId);
        [DllImport("__Internal")]
        public static extern void FBBestiarySelectSpecies(string jobId, string speciesId);
        [DllImport("__Internal")]
        public static extern void FBBestiarySelectEnvironment(string jobId, string environmentId);
        [DllImport("__Internal")]
        public static extern void FBBestiarySelectModel(string jobId, string modelId);
        [DllImport("__Internal")]
        public static extern void FBCloseBestiary(string jobId);

            //Status Events
        [DllImport("__Internal")]
        public static extern void FBOpenStatus(string jobId);
        [DllImport("__Internal")]
        public static extern void FBStatusOpenJobTab(string jobId);
        [DllImport("__Internal")]
        public static extern void FBStatusOpenItemTab(string jobId);
        [DllImport("__Internal")]
        public static extern void FBStatusOpenTechTab(string jobId);
        [DllImport("__Internal")]
        public static extern void FBCloseStatus(string jobId);

        //Game Feedback
        [DllImport("__Internal")]
        public static extern void FBSimulationSyncAchieved(string jobId);
        [DllImport("__Internal")]
        public static extern void FBGuideScriptTriggered(string nodeId);

        #endregion // Firebase JS Functions

        #region Logging Variables

        private SimpleLog m_Logger;
        private enum m_EventCategories
        {
            accept_job,
            switch_job,
            receive_fact,
            complete_job,
            complete_task,
            scene_changed,
            room_changed,
            begin_experiment,
            begin_dive,
            begin_argument,
            begin_model,
            begin_simulation,
            ask_for_help,
            talk_with_guide,
            open_bestiary,
            bestiary_open_species_tab,
            bestiary_open_environments_tab,
            bestiary_open_models_tab,
            bestiary_select_species,
            bestiary_select_environment,
            bestiary_select_model,
            close_bestiary,
            open_status,
            status_open_job_tab,
            status_open_item_tab,
            status_open_tech_tab,
            close_status,
            simulation_sync_achieved,
            guide_script_triggered
        }

        private string m_CurrentJobId = string.Empty;
        private string m_PreviousJobId = string.Empty;
        private string m_CurrentPortableAppId = string.Empty;
        private BestiaryDescCategory? m_CurrentPortableBestiaryTabId = null;
        private StatusApp.PageId? m_CurrentPortableStatusTabId = null;

        #endregion // Logging Variables

        #region IService

        protected override void Initialize()
        {
            m_Logger = new SimpleLog(m_AppId, m_AppVersion, null);
            Services.Events.Register<StringHash32>(GameEvents.JobStarted, LogAcceptJob, this)
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
                .Register<string>(GameEvents.PortableAppOpened, PortableAppOpenedHandler, this)
                .Register<string>(GameEvents.PortableAppClosed, PortableAppClosedHandler, this)
                .Register<BestiaryDescCategory>(GameEvents.PortableBestiaryTabSelected, PortableBestiaryTabSelectedHandler, this)
                .Register<StatusApp.PageId>(GameEvents.PortableStatusTabSelected, PortableStatusTabSelectedHandler, this)
                .Register<BestiaryDesc> (GameEvents.PortableEntrySelected, PortableBestiaryEntrySelectedhandler, this)
                .Register(GameEvents.ScenePreloading, ClearSceneState, this)
                .Register(GameEvents.PortableClosed, PortableClosed, this);

            Services.Script.OnTargetedThreadStarted += GuideHandler;
            SceneHelper.OnSceneLoaded += LogSceneChanged;
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);
            m_Logger?.Flush();
            m_Logger = null;
        }
        #endregion // IService

        private void ClearSceneState()
        {
            m_CurrentPortableAppId = string.Empty;
            m_CurrentPortableBestiaryTabId = null;
            m_CurrentPortableStatusTabId = null;
        }

        #region Log Events
        private void OnTitleStart(string inUserCode)
        {
            if (!string.IsNullOrEmpty(inUserCode))
            {
                #if !UNITY_EDITOR
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

            if (inThread.TriggerId() == GameTriggers.PartnerTalk)
            {
                LogTalkWithGuide(nodeId);
            }
            else if (inThread.TriggerId() == GameTriggers.RequestPartnerHelp)
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
            
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "scene_name", sceneName }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.scene_changed));

            #if !UNITY_EDITOR
            FBSceneChanged(sceneName);
            #endif
        }

        private void LogRoomChanged(string roomName)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "room_name", roomName }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.room_changed));

            #if !UNITY_EDITOR
            FBRoomChanged(roomName);
            #endif
        }

        #region bestiary handlers
        private void PortableAppOpenedHandler(string appId)
        {

            if (m_CurrentPortableAppId != string.Empty)
                PortableAppClosedHandler(m_CurrentPortableAppId);

            m_CurrentPortableAppId = appId;
            if (appId == "Bestiary")
            {
                LogOpenBestiary();
            }
            else if (appId == "Status")
            {
                LogOpenStatus();
            }
        }

        private void PortableAppClosedHandler(string appId)
        {
            if (m_CurrentPortableAppId != appId)
                return;

            m_CurrentPortableAppId = string.Empty;
            if (appId == "Bestiary")
            {
                m_CurrentPortableBestiaryTabId = null;
                LogCloseBestiary();
            }
            else if (appId == "Status")
            {
                m_CurrentPortableStatusTabId = null;
                LogCloseStatus();
            }
        }

        private void PortableClosed()
        {
            if (m_CurrentPortableAppId != string.Empty)
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
                case (BestiaryDescCategory.Model): //Models Tab
                    {
                        LogBestiaryOpenModelsTab();
                        break;
                    }
            }
        }

        private void PortableStatusTabSelectedHandler(StatusApp.PageId tabName)
        {
            if (tabName == m_CurrentPortableStatusTabId) //Tab already open, don't send another log
                return;
            else
                m_CurrentPortableStatusTabId = tabName;

            switch (tabName)
            {
                case (StatusApp.PageId.Job): //Tasks Tab
                    {
                        LogStatusOpenJobTab();
                        break;
                    }
                case (StatusApp.PageId.Item): //Items Tab
                    {
                        LogStatusOpenItemTab();
                        break;
                    }
                case (StatusApp.PageId.Tech): //Tech Tab
                    {
                        LogStatusOpenTechTab();
                        break;
                    }
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
                case (BestiaryDescCategory.Model): //Model Selected
                    {
                        LogBestiarySelectModel(selectedData.name);
                        break;
                    }
            }
        }
        #endregion

        private void LogAcceptJob(StringHash32 jobId)
        {
            m_PreviousJobId = m_CurrentJobId;

            if (jobId.IsEmpty)
            {
                m_CurrentJobId = string.Empty;
            }
            else
            {
                m_CurrentJobId = Services.Assets.Jobs.Get(jobId).name;
            }

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.accept_job));

            #if !UNITY_EDITOR
            FBAcceptJob(m_CurrentJobId);
            #endif
        }

        private void LogSwitchJob(StringHash32 jobId)
        {
            // ignore case where GameEvents.JobSwitched is dispatched when accepting a new job with no current one selected
            if (m_PreviousJobId.Equals(string.Empty)) return;

            m_PreviousJobId = m_CurrentJobId;

            if (jobId.IsEmpty)
            {
                m_CurrentJobId = string.Empty;
            }
            else
            {
                m_CurrentJobId = Services.Assets.Jobs.Get(jobId).name;
            }

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.switch_job));

            #if !UNITY_EDITOR
            FBSwitchJob(m_CurrentJobId);
            #endif
        }

        private void LogReceiveFact(BestiaryUpdateParams inParams)
        {
            if (inParams.Type == BestiaryUpdateParams.UpdateType.Fact)
            {
                string parsedFactId = Services.Assets.Bestiary.Fact(inParams.Id).name;
                
                Dictionary<string, string> data = new Dictionary<string, string>()
                {
                    { "fact_id", parsedFactId }
                };

                m_Logger.Log(new LogEvent(data, m_EventCategories.receive_fact));

                #if !UNITY_EDITOR
                FBReceiveFact(parsedFactId);
                #endif
            }
        }

        private void LogCompleteJob(StringHash32 jobId)
        {
            string parsedJobId = Services.Assets.Jobs.Get(jobId).name;

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", parsedJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.complete_job));

            #if !UNITY_EDITOR
            FBCompleteJob(parsedJobId);
            #endif

            m_PreviousJobId = m_CurrentJobId;
            m_CurrentJobId = string.Empty;
        }

        private void LogCompleteTask(StringHash32 inTaskId)
        {
            string taskId = inTaskId.ToString();

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId },
                { "task_id", taskId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.complete_task));

            #if !UNITY_EDITOR
            FBCompleteTask(m_CurrentJobId, taskId);
            #endif
        }

        private void LogBeginExperiment(TankType inTankType)
        {
            string tankType = inTankType.ToString();

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId },
                { "tank_type", tankType }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.begin_experiment));

            #if !UNITY_EDITOR
            FBBeginExperiment(m_CurrentJobId, tankType);
            #endif
        }

        private void LogBeginDive(string inTargetScene)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId },
                { "site_id", inTargetScene }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.begin_dive));

            #if !UNITY_EDITOR
            FBBeginDive(m_CurrentJobId, inTargetScene);
            #endif
        }

        private void LogBeginArgument()
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.begin_argument));

            #if !UNITY_EDITOR
            FBBeginArgument(m_CurrentJobId);
            #endif
        }

        private void LogBeginModel()
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.begin_model));

            #if !UNITY_EDITOR
            FBBeginModel(m_CurrentJobId);
            #endif
        }

        private void LogBeginSimulation()
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.begin_simulation));

            #if !UNITY_EDITOR
            FBBeginSimulation(m_CurrentJobId);
            #endif
        }

        private void LogAskForHelp(string nodeId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "node_id", nodeId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.ask_for_help));

            #if !UNITY_EDITOR
            FBAskForHelp(nodeId);
            #endif
        }

        private void LogTalkWithGuide(string nodeId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "node_id", nodeId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.talk_with_guide));

            #if !UNITY_EDITOR
            FBTalkWithGuide(nodeId);
            #endif
        }

        #region Bestiary App Logging
        private void LogOpenBestiary()
        {
            m_CurrentPortableBestiaryTabId = BestiaryDescCategory.Critter;

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.open_bestiary));

            #if !UNITY_EDITOR
            FBOpenBestiary(m_CurrentJobId);
            #endif

            LogBestiaryOpenSpeciesTab(); //Bestiary starts by opening Critters tab
        }

        private void LogBestiaryOpenSpeciesTab()
        {
            //Debug.Log("LOG: opened species tab");
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.bestiary_open_species_tab));

            #if !UNITY_EDITOR
            FBBestiaryOpenSpeciesTab(m_CurrentJobId);
            #endif
        }
        private void LogBestiaryOpenEnvironmentsTab()
        {
            //Debug.Log("LOG: opened environment tab");
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.bestiary_open_environments_tab));

            #if !UNITY_EDITOR
            FBBestiaryOpenEnvironmentsTab(m_CurrentJobId);
            #endif
        }
        private void LogBestiaryOpenModelsTab()
        {
            //Debug.Log("LOG: opened model tab");
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.bestiary_open_models_tab));

            #if !UNITY_EDITOR
            FBBestiaryOpenModelsTab(m_CurrentJobId);
            #endif
        }

        private void LogBestiarySelectSpecies(string speciesId)
        {
            //Debug.Log("LOG: selected a species");
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId },
                { "species_id", speciesId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.bestiary_select_species));

            #if !UNITY_EDITOR
            FBBestiarySelectSpecies(m_CurrentJobId, speciesId);
            #endif
        }
        private void LogBestiarySelectEnvironment(string environmentId)
        {
            //Debug.Log("LOG: selected an environment");
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId },
                { "environment_id", environmentId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.bestiary_select_environment));

            #if !UNITY_EDITOR
            FBBestiarySelectEnvironment(m_CurrentJobId,environmentId);
            #endif
        }
        private void LogBestiarySelectModel(string modelId)
        {
            //Debug.Log("LOG: selected an environment");
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId },
                { "model_id", modelId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.bestiary_select_model));

            #if !UNITY_EDITOR
            FBBestiarySelectModel(m_CurrentJobId,modelId);
            #endif
        }
        private void LogCloseBestiary()
        {
            //Debug.Log("LOG: closed bestiary");
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.close_bestiary));

            #if !UNITY_EDITOR
            FBCloseBestiary(m_CurrentJobId);
            #endif
        }
        #endregion

        #region Status App Logging
        private void LogOpenStatus()
        {
            m_CurrentPortableStatusTabId = StatusApp.PageId.Job;

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.open_status));

            #if !UNITY_EDITOR
            FBOpenStatus(m_CurrentJobId);
            #endif

            LogStatusOpenJobTab(); //Status starts by opening tasks tab
        }

        private void LogStatusOpenJobTab()
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.status_open_job_tab));

            #if !UNITY_EDITOR
            FBStatusOpenJobTab(m_CurrentJobId);
            #endif
        }

        private void LogStatusOpenItemTab()
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.status_open_item_tab));

            #if !UNITY_EDITOR
            FBStatusOpenItemTab(m_CurrentJobId);
            #endif
        }

        private void LogStatusOpenTechTab()
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.status_open_tech_tab));

            #if !UNITY_EDITOR
            FBStatusOpenTechTab(m_CurrentJobId);
            #endif
        }

        private void LogCloseStatus()
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.close_status));

            #if !UNITY_EDITOR
            FBCloseStatus(m_CurrentJobId);
            #endif
        }
        #endregion

        private void LogSimulationSyncAchieved()
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", m_CurrentJobId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.simulation_sync_achieved));

            #if !UNITY_EDITOR
            FBSimulationSyncAchieved(m_CurrentJobId);
            #endif
        }

        private void LogGuideScriptTriggered(string nodeId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "node_id", nodeId }
            };

            m_Logger.Log(new LogEvent(data, m_EventCategories.guide_script_triggered));

            #if !UNITY_EDITOR
            FBGuideScriptTriggered(nodeId);
            #endif
        }

        #endregion // Log Events
    }
}
