using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class SimTest : MonoBehaviour, IEnergySimScenario
    {
        public EnergySimDatabase database;
        public SimDisplay display;

        [Header("Species")]

        public int kelpCount;
        public int urchinCount;
        public int otterCount;

        [Header("Chem")]

        public int oxygen;
        public int co2;

        private EnergySimContext simContext;
        private EnergySim sim;

        public void Start()
        {
            LogSize<ActorState>();

            simContext.Database = database;
            simContext.Scenario = this;
            simContext.Start = new EnergySimState();
            simContext.Logger = new BatchedUnityDebugLogger();

            simContext.Start.AddActors(database.ActorType(ActorTypeId.Kelp), kelpCount);
            simContext.Start.AddActors(database.ActorType(ActorTypeId.Urchin), urchinCount);
            simContext.Start.AddActors(database.ActorType(ActorTypeId.SeaOtter), otterCount);
            simContext.Start.Environment.Type = EnvironmentTypeId.KelpForest;

            simContext.Start.AddResourceToEnvironment(database, VarTypeId.Oxygen, (ushort) oxygen);
            simContext.Start.AddResourceToEnvironment(database, VarTypeId.CarbonDioxide, (ushort) co2);

            sim = new EnergySim();

            sim.Setup(ref simContext);
            display.Sync(simContext);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                sim.Tick(ref simContext);
                display.Sync(simContext);
            }
        }

        static private void LogSize<T>()
        {
            Debug.LogFormat("sizeof({0}) = {1}", typeof(T).FullName, Marshal.SizeOf<T>());
        }

        ushort IEnergySimScenario.TotalTicks()
        {
            return 32;
        }

        int IEnergySimScenario.TickActionCount()
        {
            return 5;
        }

        int IEnergySimScenario.TickScale()
        {
            return 1;
        }
    }
}