using BeauData;

namespace ProtoAqua.Energy
{
    public struct EnergySimContext
    {
        public EnergySimDatabase Database;
        public IEnergySimScenario Scenario;
        public EnergySimState Start;
        public ILogger Logger;

        public EnergySimState Current;
        public System.Random RNG;
    }
}