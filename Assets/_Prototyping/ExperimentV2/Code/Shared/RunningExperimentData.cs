using System;
using BeauData;
using BeauUtil;

namespace ProtoAqua.ExperimentV2 {
    public class RunningExperimentData {
        public enum Type : byte {
            Observation,
            Stress,
            Measurement
        }

        [Flags]
        public enum Flags : byte {
            Stabilizer = 0x01,
            Feeder = 0x02,

            ALL = Stabilizer | Feeder
        }

        public StringHash32 TankId;
        public Type TankType;
        public StringHash32[] CritterIds;
        public StringHash32 EnvironmentId;
        public Flags Settings;
        public int CustomData;
    }
}