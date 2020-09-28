using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BeauData;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace ProtoAqua.Energy
{
    public class SimTest : MonoBehaviour, ISceneLoadHandler
    {
        public SimLoader loader;
        public SimDisplay display;

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

        [NonSerialized] private EnergyConfig m_GlobalSettings;

        [NonSerialized] private bool m_Complete = true;
        [NonSerialized] private bool m_QueuedReset = false;
        [NonSerialized] private bool m_QueuedRuleRegen = false;

        [NonSerialized] private BatchedUnityDebugLogger m_Logger = new BatchedUnityDebugLogger();

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            m_GlobalSettings = Services.Tweaks.Get<EnergyConfig>();

            Services.Audio.PostEvent("energy_bgm").SetVolume(0).SetVolume(1, 3f);

            if (inContext != null && inContext.GetType() == typeof(QueryParams)) 
            {
                m_ScenarioPackage = loader.LoadStartingScenario((QueryParams)inContext);
            } 
            else
            {
                m_ScenarioPackage = loader.LoadStartingScenario(Services.Data.PopQueryParams());
            }

            m_BaseDatabase = loader.LoadDatabase(m_ScenarioPackage.Header.DatabaseId);

            m_DatabaseOverride = new SimDatabaseOverride(m_BaseDatabase);

            m_ScenarioPackage.ApplyRules(m_DatabaseOverride);

            display.Menus.Initialize(m_ScenarioPackage, m_DatabaseOverride);
            display.Rules.Populate(m_ScenarioPackage, m_DatabaseOverride);

            simContext.Database = m_DatabaseOverride;
            simContext.Scenario = m_ScenarioPackage;

            dataContext.Database = m_BaseDatabase;
            dataContext.Scenario = m_ScenarioPackage;

            if (debug)
            {
                simContext.Logger = m_Logger;
                dataContext.Logger = m_Logger;
            }

            sim = new EnergySim();

            sim.Setup(ref simContext);
            sim.Setup(ref dataContext);

            display.Sync(m_ScenarioPackage.Header, simContext, dataContext);

            m_CalculateTotalRoutine.Replace(this, CalculateTotalSync(dataContext, simContext, m_ScenarioPackage.Data.Duration));

            display.Ticker.OnTickChanged += UpdateTick;

            display.Menus.EditPanel.OnRuleParametersChanged += QueueRuleRegen;
            display.Menus.EditPanel.OnRequestLevelRestart += QueueReset;

            display.Menus.ShowIntro();
        }

        private void QueueRuleRegen()
        {
            m_QueuedRuleRegen = true;
        }

        private void QueueReset()
        {
            m_QueuedReset = true;
        }

        private void ResetScenario()
        {
            m_QueuedReset = false;
            m_QueuedRuleRegen = false;

            m_ScenarioPackage.ApplyRules(m_DatabaseOverride);
            display.Rules.Populate(m_ScenarioPackage, m_DatabaseOverride);

            sim.Setup(ref simContext);
            sim.Setup(ref dataContext);

            m_ScenarioPackage.Data.Sync(ref m_ScenarioVersion);
            m_BaseDatabase.Sync(ref m_DataDBVersion);
            m_DatabaseOverride.Sync(ref m_SimDBVersion);

            display.Sync(m_ScenarioPackage.Header, simContext, dataContext);
            display.Ticker.UpdateTickSync(null, 0);

            m_CalculateTotalRoutine.Replace(this, CalculateTotalSync(dataContext, simContext, m_ScenarioPackage.Data.Duration));
        }

        private void OnDestroy()
        {
            Ref.Dispose(ref m_DatabaseOverride);
        }

        private void Update()
        {
            if (sim == null)
                return;

            if (display.Menus.IsOpen())
            {
                m_CalculateTotalRoutine.Pause();
                return;
            }

            if (m_QueuedReset)
            {
                ResetScenario();
                return;
            }

            if (m_QueuedRuleRegen)
            {
                display.Rules.Populate(m_ScenarioPackage, m_DatabaseOverride);
                m_QueuedRuleRegen = false;
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

        private void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote)) 
            {
                debug = !debug;
                if (debug)
                {
                    simContext.Logger = dataContext.Logger = m_Logger;
                    Debug.Log("[SimTest] Debugging turned on");
                }
                else
                {
                    simContext.Logger = dataContext.Logger = null;
                    Debug.Log("[SimTest] Debugging turned off");
                }
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
            // inData.Logger = null;
            // inModel.Logger = null;

            float syncAccum = 0;

            int syncedFrameCount = 0;
            int framesWithErrors = 0;
            float[] allSyncs = new float[inTotalTicks + 1];

            yield return Routine.ForAmortize(0, inTotalTicks + 1, (i) => {
                sim.Scrub(ref inData, (ushort) i, EnergySim.SimFlags.DoNotWriteToCache);
                sim.Scrub(ref inModel, (ushort) i, EnergySim.SimFlags.DoNotWriteToCache);
                float sync = m_GlobalSettings.CalculateSync(inData, inModel);
                if (sync < 100)
                {
                    ++framesWithErrors;
                }
                else if (framesWithErrors == 0 && i > 0)
                {
                    ++syncedFrameCount;
                }
                syncAccum += sync;
                allSyncs[i] = sync;
            }, 8);

            float progress = (float) Math.Floor(100f * syncedFrameCount / inTotalTicks);
            display.Ticker.UpdateTickSync(allSyncs, progress);

            if (progress == 100)
            {
                if (!m_Complete)
                {
                    m_Complete = true;
                    Services.Audio.PostEvent("full_sync");
                    display.Menus.ShowComplete();
                }
            }
            else
            {
                m_Complete = false;
            }
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