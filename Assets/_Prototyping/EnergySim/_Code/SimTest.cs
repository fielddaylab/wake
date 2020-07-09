using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BeauData;
using BeauUtil;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProtoAqua.Energy
{
    public class SimTest : MonoBehaviour
    {
        public SimLoader loader;
        public SimDisplay display;
        public SimConfig config;

        [Header("Debug")]
        public bool debug = true;
        public string debugUrl = null;

        private EnergySimContext simContext;
        private EnergySimContext dataContext;
        private EnergySim sim;

        [NonSerialized] private int m_SimDBVersion;
        [NonSerialized] private int m_DataDBVersion;
        [NonSerialized] private int m_ScenarioVersion;

        [NonSerialized] private ScenarioPackage m_ScenarioPackage;
        [NonSerialized] private ISimDatabase m_BaseDatabase;
        [NonSerialized] private ISimDatabase m_DatabaseOverride;

        public void Start()
        {
            m_ScenarioPackage = loader.LoadStartingScenario(GetQueryParams());
            m_BaseDatabase = loader.LoadDatabase(m_ScenarioPackage.Header.DatabaseId);

            m_DatabaseOverride = new SimDatabaseOverride(m_BaseDatabase);
            RulesConfigPanel.RandomizeDatabase(m_DatabaseOverride, 1);

            config.Initialize(m_ScenarioPackage, m_DatabaseOverride);

            simContext.Database = m_DatabaseOverride;
            simContext.Scenario = m_ScenarioPackage.Scenario;

            dataContext.Database = m_BaseDatabase;
            dataContext.Scenario = m_ScenarioPackage.Scenario;

            if (debug)
            {
                simContext.Logger = new BatchedUnityDebugLogger();
                dataContext.Logger = new BatchedUnityDebugLogger();
            }

            sim = new EnergySim();

            sim.Setup(ref simContext);
            sim.Setup(ref dataContext);
            display.Sync(simContext, dataContext);

            display.Ticker.OnTickChanged += UpdateTick;
        }

        private void OnDestroy()
        {
            Ref.Dispose(ref m_DatabaseOverride);
        }

        private void Update()
        {
            if (config.IsPaused())
                return;

            bool bScenarioRefresh = m_ScenarioPackage.Scenario.HasChanged(ref m_ScenarioVersion);
            bool bSimRequestsRefresh = simContext.Database.HasChanged(ref m_SimDBVersion) || bScenarioRefresh;
            bool bDataRequestsRefresh = dataContext.Database.HasChanged(ref m_DataDBVersion) || bScenarioRefresh;

            if (bSimRequestsRefresh || bDataRequestsRefresh)
            {
                uint tick = simContext.Current.Timestamp;

                if (bSimRequestsRefresh)
                {
                    Debug.Log("resimulating sim");
                    
                    sim.Setup(ref simContext);
                    sim.Scrub(ref simContext, tick);
                }

                if (bDataRequestsRefresh)
                {
                    Debug.Log("resimulating data");

                    sim.Setup(ref dataContext);
                    sim.Scrub(ref dataContext, tick);
                }

                display.Sync(simContext, dataContext);
            }
        }

        private void UpdateTick(uint inTick)
        {
            Stopwatch watch = Stopwatch.StartNew();
            {
                sim.Scrub(ref simContext, inTick);
                sim.Scrub(ref dataContext, inTick);
            }
            watch.Stop();
            Debug.LogFormat("Simulation took {0}ms", watch.ElapsedMilliseconds);
            
            display.Sync(simContext, dataContext);
        }

        private QueryParams GetQueryParams()
        {
            string url;
            #if UNITY_EDITOR
            url = debugUrl;
            #else
            url = Application.absoluteURL;
            #endif // UNITY_EDITOR

            QueryParams qp = new QueryParams();
            qp.TryParse(url);
            return qp;
        }
    }
}