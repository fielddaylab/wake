using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BeauData;
using BeauRoutine;
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

        [NonSerialized] private Routine m_CalculateTotalRoutine;

        public void Start()
        {
            m_ScenarioPackage = loader.LoadStartingScenario(GetQueryParams());
            m_BaseDatabase = loader.LoadDatabase(m_ScenarioPackage.Header.DatabaseId);

            m_DatabaseOverride = new SimDatabaseOverride(m_BaseDatabase);
            RulesConfigPanel.RandomizeDatabase(m_DatabaseOverride, m_ScenarioPackage.Header.ContentAreas, m_ScenarioPackage.Data.StartingActorIds(), m_ScenarioPackage.Header.Difficulty);

            config.Initialize(m_ScenarioPackage, m_DatabaseOverride);

            simContext.Database = m_DatabaseOverride;
            simContext.Scenario = m_ScenarioPackage;

            dataContext.Database = m_BaseDatabase;
            dataContext.Scenario = m_ScenarioPackage;

            if (debug)
            {
                simContext.Logger = new BatchedUnityDebugLogger();
                dataContext.Logger = new BatchedUnityDebugLogger();
            }

            sim = new EnergySim();

            sim.Setup(ref simContext);
            sim.Setup(ref dataContext);
            display.Sync(m_ScenarioPackage.Header, simContext, dataContext);

            m_CalculateTotalRoutine.Replace(this, CalculateTotalSync(dataContext, simContext, m_ScenarioPackage.Data.Duration));

            display.Ticker.OnTickChanged += UpdateTick;
        }

        private void OnDestroy()
        {
            Ref.Dispose(ref m_DatabaseOverride);
        }

        private void Update()
        {
            if (config.IsPaused())
            {
                m_CalculateTotalRoutine.Pause();
                return;
            }

            m_CalculateTotalRoutine.Resume();

            bool bScenarioRefresh = m_ScenarioPackage.Data.HasChanged(ref m_ScenarioVersion);
            bool bSimRequestsRefresh = simContext.Database.HasChanged(ref m_SimDBVersion) || bScenarioRefresh;
            bool bDataRequestsRefresh = dataContext.Database.HasChanged(ref m_DataDBVersion) || bScenarioRefresh;

            if (bSimRequestsRefresh || bDataRequestsRefresh)
            {
                ushort tick = simContext.CachedCurrent.Timestamp;

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

                display.Sync(m_ScenarioPackage.Header, simContext, dataContext);
                m_CalculateTotalRoutine.Replace(this, CalculateTotalSync(dataContext, simContext, m_ScenarioPackage.Data.Duration));
            }
        }

        private void UpdateTick(ushort inTick)
        {
            Stopwatch watch = Stopwatch.StartNew();
            {
                sim.Scrub(ref simContext, inTick);
                sim.Scrub(ref dataContext, inTick);
            }
            watch.Stop();
            Debug.LogFormat("Simulation took {0}ms", watch.ElapsedMilliseconds);
            
            display.Sync(m_ScenarioPackage.Header, simContext, dataContext);
        }

        private IEnumerator CalculateTotalSync(EnergySimContext inData, EnergySimContext inModel, ushort inTotalTicks)
        {
            display.Syncer.HideTotalSync();

            inData.Logger = null;
            inModel.Logger = null;

            float syncAccum = 0;

            int startingFrameCount = Time.frameCount;

            yield return Routine.ForAmortize(0, inTotalTicks + 1, (i) => {
                sim.Scrub(ref inData, (ushort) i, EnergySim.SimFlags.DoNotWriteToCache);
                sim.Scrub(ref inModel, (ushort) i, EnergySim.SimFlags.DoNotWriteToCache);
                syncAccum += display.Syncer.CalculateSync(inData, inModel);
            }, 8);

            float avgSync = (float) Math.Round(syncAccum / (inTotalTicks + 1));
            display.Syncer.ShowTotalSync(avgSync);

            int endingFrameCount = Time.frameCount;

            Debug.LogFormat("[SimTest] Updated total sync ({0}%), took {1} frames", avgSync, endingFrameCount - startingFrameCount);
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
    
        #if UNITY_EDITOR

        [ContextMenu("Dump Cache Info")]
        private void DumpCaches()
        {
            simContext.Cache.Dump();
            dataContext.Cache.Dump();
        }

        #endif // UNITY_EDITOR
    }
}