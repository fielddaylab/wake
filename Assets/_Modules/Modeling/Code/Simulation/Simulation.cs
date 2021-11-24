using System;

namespace Aqua.Modeling {
    static public unsafe class Simulation {
        public const int MaxTrackedCritters = 16;
        public const float MaxEatProportion = 0.75f;
        public const float MaxReproduceProportion = 0.75f;
        public const float MaxDeathProportion = 0.5f;
        public const uint HungerPerPopulation = 32;
    }
}