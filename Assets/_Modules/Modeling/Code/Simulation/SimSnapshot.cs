using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Modeling {
    public unsafe struct SimSnapshot {
        public uint Timestamp;
        public WaterPropertyBlockF32 Water;
        public fixed uint Populations[Simulation.MaxTrackedCritters];
        public fixed byte StressedRatio[Simulation.MaxTrackedCritters]; // stressed population estimates as a ratio of stressed / total population
    }

    public unsafe struct SimProduceConsumeSnapshot {
        public float Oxygen;
        public float CarbonDioxide;
    }
}