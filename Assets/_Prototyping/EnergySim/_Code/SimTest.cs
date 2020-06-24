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
        public EnergySimScenario scenario2;
        public SimDisplay display;

        private EnergySimContext simContext;
        private EnergySimContext dataContext;
        private EnergySim sim;

        public bool debug = true;

        public void Start()
        {
            LogSize<ActorState>();
            LogSize<EnvironmentState>();
            LogSize<VarPair>();
            LogSize<VarPairF>();
            LogSize<ActorCount>();
            LogSize<VarState<ushort>>();
            LogSize<VarState<float>>();

            simContext.Database = database;
            simContext.Scenario = scenario;

            dataContext.Database = database;
            dataContext.Scenario = scenario2;

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

        static private void LogSize<T>()
        {
            Debug.LogFormat("sizeof({0}) = {1}", typeof(T).FullName, Marshal.SizeOf<T>());
        }
    }
}