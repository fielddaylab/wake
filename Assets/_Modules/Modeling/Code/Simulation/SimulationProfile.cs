using System;
using BeauUtil;

namespace Aqua.Modeling {
    public class SimulationProfile {

        // information about an actor
        public struct ActorInfo {
            public StringHash32 Id;
            public uint MassPerPopulation;
            public uint PopulationCap;
            public uint ScarcityLevel;
            public ActorFlags Flags;
            public ActorStateTransitionSet StateTransitions;
            public BehaviorInfo AliveBehavior;
            public BehaviorInfo StressedBehavior;
        }

        // information about an organism's behavior for a particular state
        public struct BehaviorInfo {
            public float ProduceOxygen;
            public float ProduceCarbonDioxide;
            public float ConsumeOxygen;
            public float ConsumeCarbonDioxide;
            public float ConsumeLight;
            public ushort EatOffset;
            public ushort EatCount;
            public uint Growth;
            public float Reproduce;
            public float Death;
        }

        [Flags]
        public enum ActorFlags : byte {
            Herd = 0x01,
            IgnoreStarvation = 0x02,
        }
    }
}