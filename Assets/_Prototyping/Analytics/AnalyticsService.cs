using System.Collections.Generic;
using System.Runtime.InteropServices;
using Aqua.Scripting;
using BeauUtil;
using BeauUtil.Services;
using FieldDay;
using ProtoAqua.Experiment;
using ProtoAqua.Modeling;
using UnityEngine;

namespace Aqua
{
    [ServiceDependency(typeof(EventService), typeof(ScriptingService))]
    public partial class AnalyticsService : ServiceBehaviour, ISurveyHandler
    {
        #region Inspector

        [SerializeField, Required] private string m_AppId = "AQUALAB";
        [SerializeField] private int m_AppVersion = 1;
        [SerializeField] private GameObject m_SurveyPrefab = null;
        [SerializeField] private TextAsset m_DefaultSurveyJSON = null;
        
        #endregion // Inspector

        #region Firebase JS Functions

        [DllImport("__Internal")]
        public static extern void FBStartGameWithUserCode(string userCode);
        [DllImport("__Internal")]
        public static extern void FBAcceptJob(string jobId);
        [DllImport("__Internal")]
        public static extern void FBReceiveFact(string factId);
        [DllImport("__Internal")]
        public static extern void FBCompleteJob(string jobId);
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
            receive_fact,
            complete_job,
            begin_experiment,
            begin_dive,
            begin_argument,
            begin_model,
            begin_simulation,
            ask_for_help,
            talk_with_guide,
            simulation_sync_achieved,
            guide_script_triggered
        }

        private string m_CurrentJobId = string.Empty;

        #endregion // Logging Variables

        #region IService

        protected override void Initialize()
        {
            m_Logger = new SimpleLog(m_AppId, m_AppVersion);
            Services.Events.Register<StringHash32>(GameEvents.JobStarted, LogAcceptJob)
                .Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, LogReceiveFact)
                .Register<StringHash32>(GameEvents.JobCompleted, LogCompleteJob)
                .Register<TankType>(ExperimentEvents.ExperimentBegin, LogBeginExperiment)
                .Register<string>(GameEvents.BeginDive, LogBeginDive)
                .Register(GameEvents.BeginArgument, LogBeginArgument)
                .Register(SimulationConsts.Event_Model_Begin, LogBeginModel)
                .Register(SimulationConsts.Event_Simulation_Begin, LogBeginSimulation)
                .Register(SimulationConsts.Event_Simulation_Complete, LogSimulationSyncAchieved)
                .Register<string>(TitleController.Event_StartGame, OnTitleStart)
                .Register<string>(GameEvents.BeginSurvey, DisplaySurvey);

            Services.Script.OnTargetedThreadStarted += GuideHandler;
        }

        protected override void Shutdown()
        {
            m_Logger?.Flush();
            m_Logger = null;
        }

        #endregion // IService

        #region ISurveyHandler

        void ISurveyHandler.HandleSurveyResponse(Dictionary<string, string> surveyResponses)
        {
            Debug.Log("handled");
        }

        #endregion // ISurveyHandler

        private void DisplaySurvey(string inSurveyName)
        {
            GameObject go = Instantiate(m_SurveyPrefab);
            DontDestroyOnLoad(go);
            Survey survey = go.GetComponent<Survey>();
            survey.Initialize(inSurveyName, m_DefaultSurveyJSON, this);
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

        private void LogAcceptJob(StringHash32 jobId)
        {
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
            if (jobId.IsEmpty)
            {
                m_CurrentJobId = string.Empty;
            }
            else
            {
                m_CurrentJobId = Services.Assets.Jobs.Get(jobId).name;
            }
        }

        private void LogReceiveFact(BestiaryUpdateParams inParams)
        {
            if (inParams.Type == BestiaryUpdateParams.UpdateType.Fact)
            {
                string parsedFactId = Services.Assets.Bestiary.Fact(inParams.Id).name;
                
                Dictionary<string, string> data = new Dictionary<string ,string>()
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

            m_CurrentJobId = string.Empty;
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
