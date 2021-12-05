using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua.Modeling {
    public unsafe struct SimSnapshot {
        public WaterPropertyBlockF32 Water;
        public fixed uint Populations[Simulation.MaxTrackedCritters];
        public fixed byte StressedRatio[Simulation.MaxTrackedCritters]; // stressed population estimates as a ratio of stressed / total population

        static internal string Dump(SimSnapshot snapshot, SimProfile profile, uint inTimestamp) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Tick ").Append(inTimestamp);
            sb.Append("\nWater: ").Append(snapshot.Water.ToString());
            sb.Append("\nPopulations:");
            for(int i = 0; i < profile.ActorCount; i++) {
                SimProfile.ActorInfo* actorInfo = &profile.Actors[i];
                sb.Append("\n\t").Append(actorInfo->Id.ToDebugString()).Append(": ");
                uint totalPopulation = snapshot.Populations[i];
                float stressedRatio = snapshot.StressedRatio[i] / 128f;
                uint stressedPopulation = SimMath.FixedMultiply(totalPopulation, stressedRatio);
                uint alivePopulation = totalPopulation - stressedPopulation;
                sb.Append(alivePopulation).Append(" alive, ").Append(stressedPopulation).Append(" dead");
            }
            return sb.Flush();
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public unsafe struct SimProduceConsumeSnapshot {
        public float Oxygen;
        public float CarbonDioxide;
    }
}