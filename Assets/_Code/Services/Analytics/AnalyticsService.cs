using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Services;
using FieldDay;
using UnityEngine;

namespace Aqua
{
    [ServiceDependency(typeof(EventService))]
    public partial class AnalyticsService : ServiceBehaviour
    {
        #region Inspector

        [SerializeField, Required] private string m_AppId = "AQUALAB";
        [SerializeField] private int m_AppVersion = 1;
        
        #endregion // Inspector

        #region Firebase JS Functions

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

        #endregion // Logging Variables

        #region IService

        protected override void Initialize()
        {
            m_Logger = new SimpleLog(m_AppId, m_AppVersion, null);
            Services.Events.Register<StringHash32>(GameEvents.JobStarted, LogAcceptJob)
                .Register<StringHash32>(GameEvents.ReceiveFact, LogReceiveFact)
                .Register<StringHash32>(GameEvents.JobCompleted, LogCompleteJob)
                .Register(GameEvents.BeginExperiment, LogBeginExperiment)
                .Register(GameEvents.BeginDive, LogBeginDive)
                .Register(GameEvents.BeginArgument, LogBeginArgument)
                .Register(GameEvents.BeginModel, LogBeginModel)
                .Register(GameEvents.BeginSimulation, LogBeginSimulation)
                .Register(GameEvents.AskForHelp, LogAskForHelp)
                .Register(GameEvents.TalkWithGuide, LogTalkWithGuide)
                .Register(GameEvents.SimulationSyncAchieved, LogSimulationSyncAchieved)
                .Register<StringHash32>(GameEvents.GuideScriptTriggered, LogGuideScriptTriggered);
        }

        protected override void Shutdown()
        {
            m_Logger?.Flush();
            m_Logger = null;
        }

        #endregion // IService

        #region Log Events

        private void LogAcceptJob(StringHash32 jobId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", jobId.ToDebugString() }
            };

            Debug.Log(jobId.ToDebugString());

            //m_Logger.Log(new LogEvent(data, m_EventCategories.accept_job));

            #if !UNITY_EDITOR
            FBAcceptJob(jobId.ToDebugString());
            #endif
        }

        private void LogReceiveFact(StringHash32 factId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "fact_id", factId.ToDebugString() }
            };

            Debug.Log(factId.ToDebugString());

            //m_Logger.Log(new LogEvent(data, m_EventCategories.receive_fact));

            #if !UNITY_EDITOR
            FBReceiveFact(jobId.ToDebugString());
            #endif
        }

        private void LogCompleteJob(StringHash32 jobId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", jobId.ToDebugString() }
            };

            Debug.Log(jobId.ToDebugString());

            //m_Logger.Log(new LogEvent(data, m_EventCategories.complete_job));

            #if !UNITY_EDITOR
            FBCompleteJob(jobId.ToDebugString());
            #endif
        }

        private void LogBeginExperiment()
        {
            /*
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", jobId.ToDebugString() },
                { "tank_type", tankType }
            };

            print(jobId.ToDebugString());
            print(tankType);
            */

            Debug.Log("Begin experiment");

            //m_Logger.Log(new LogEvent(data, m_EventCategories.begin_experiment));

            #if !UNITY_EDITOR
            FBBeginExperiment(jobId.ToDebugString());
            #endif
        }

        private void LogBeginDive()
        {
            /*
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", jobId.ToDebugString() },
                { "site_id", siteId }
            };

            Debug.Log(jobId.ToDebugString());
            Debug.Log(siteId);
            */

            Debug.Log("Begin dive");

            //m_Logger.Log(new LogEvent(data, m_EventCategories.begin_dive));

            #if !UNITY_EDITOR
            FBBeginDive(jobId.ToDebugString());
            #endif
        }

        private void LogBeginArgument()
        {
            /*
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", jobId.ToDebugString() }
            };

            Debug.Log(jobId.ToDebugString());
            */

            Debug.Log("Begin argument");

            //m_Logger.Log(new LogEvent(data, m_EventCategories.begin_argument));

            #if !UNITY_EDITOR
            FBBeginArgument(jobId.ToDebugString());
            #endif
        }

        private void LogBeginModel()
        {
            /*
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", jobId.ToDebugString() }
            };

            Debug.Log(jobId.ToDebugString());
            */

            Debug.Log("Begin model");

            //m_Logger.Log(new LogEvent(data, m_EventCategories.begin_model));

            #if !UNITY_EDITOR
            FBBeginModel(jobId.ToDebugString());
            #endif
        }

        private void LogBeginSimulation()
        {
            /*
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", jobId.ToDebugString() }
            };

            Debug.Log(jobId.ToDebugString());
            */

            Debug.Log("Begin simulation");

            //m_Logger.Log(new LogEvent(data, m_EventCategories.begin_simulation));

            #if !UNITY_EDITOR
            FBBeginSimulation(jobId.ToDebugString());
            #endif
        }

        private void LogAskForHelp()
        {
            /*
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "node_id", nodeId.ToDebugString() }
            };

            print(nodeId.ToDebugString());
            */

            Debug.Log("Ask for help");

            //m_Logger.Log(new LogEvent(data, m_EventCategories.ask_for_help));

            #if !UNITY_EDITOR
            FBAskForHelp(jobId.ToDebugString());
            #endif
        }

        private void LogTalkWithGuide()
        {
            /*
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "node_id", nodeId.ToDebugString() }
            };

            print(nodeId.ToDebugString());
            */

            Debug.Log("Talk with guide");

            //m_Logger.Log(new LogEvent(data, m_EventCategories.talk_with_guide));

            #if !UNITY_EDITOR
            FBTalkWithGuide(jobId.ToDebugString());
            #endif
        }

        private void LogSimulationSyncAchieved()
        {
            /*
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "job_id", jobId.ToDebugString() }
            };

            Debug.Log(jobId.ToDebugString());
            */

            //m_Logger.Log(new LogEvent(data, m_EventCategories.simulation_sync_achieved));

            #if !UNITY_EDITOR
            FBSimulationSyncAchieved(jobId.ToDebugString());
            #endif
        }

        private void LogGuideScriptTriggered(StringHash32 nodeId)
        {
            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "node_id", nodeId.ToDebugString() }
            };

            Debug.Log(nodeId.ToDebugString());

            //m_Logger.Log(new LogEvent(data, m_EventCategories.guide_script_triggered));

            #if !UNITY_EDITOR
            FBGuideScriptTriggered(jobId.ToDebugString());
            #endif
        }

        #endregion // Log Events
    }
}
