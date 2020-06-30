using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ProtoAqua.Energy
{
    public class SimTest : MonoBehaviour
    {
        public EnergySimDatabase database;
        public EnergySimScenario scenario;
        public SimDisplay display;

        private EnergySimContext simContext;
        private EnergySimContext dataContext;
        private EnergySim sim;

        public bool debug = true;

        [NonSerialized] private int m_SimDBVersion;
        [NonSerialized] private int m_DataDBVersion;
        [NonSerialized] private int m_ScenarioVersion;
        [NonSerialized] private RuntimeSimScenario m_RuntimeScenario;
        [NonSerialized] private SimDatabaseOverride m_DatabaseOverride;

        public void Start()
        {
            m_DatabaseOverride = new SimDatabaseOverride(database);
            m_RuntimeScenario = scenario.CreateRuntime();

            simContext.Database = m_DatabaseOverride;
            simContext.Scenario = m_RuntimeScenario;

            dataContext.Database = database;
            dataContext.Scenario = m_RuntimeScenario;

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
            bool bScenarioRefresh = m_RuntimeScenario.HasChanged(ref m_ScenarioVersion);
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
    }
}