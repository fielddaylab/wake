using BeauData;

namespace ProtoAqua.Energy
{
    public struct EnergySimContext
    {
        public ISimDatabase Database;
        public ScenarioPackage Scenario;
        public SimStateCache Cache;
        public ILogger Logger;

        public EnergySimState CachedCurrent;
    }
}