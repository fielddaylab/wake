using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Modeling {
    // need to keep this as compact as possible - might be storing a lot in memory
    public unsafe struct SimSnapshot {
        public WaterPropertyBlockF32 Water;
        public fixed uint Populations[Simulation.MaxTrackedCritters];
        public fixed byte StressedRatio[Simulation.MaxTrackedCritters]; // stressed population estimates as a ratio of stressed / total population
    }
}