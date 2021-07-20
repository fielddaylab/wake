using System;
using BeauData;
using BeauUtil;

namespace Aqua
{
    public class InProgressExperimentData : ISerializedObject, ISerializedVersion, IKeyValuePair<StringHash32, InProgressExperimentData>
    {
        public enum Type : byte
        {
            Observation,
            Stress,
            Measurement
        }

        [Flags]
        public enum Flags : byte
        {
            Stabilizer = 0x01,
            Feeder = 0x02
        }

        public StringHash32 TankId;
        public Type TankType;
        public StringHash32[] CritterIds;
        public StringHash32 EnvironmentId;
        public Flags Settings;
        public GTDate Start;
        public GTTimeSpan Duration;

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, InProgressExperimentData>.Key { get { return TankId; } }

        InProgressExperimentData IKeyValuePair<StringHash32, InProgressExperimentData>.Value { get { return this; } }

        #endregion // KeyValue

        #region ISerializedObject

        public ushort Version { get { return 1; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32Proxy("tankId", ref TankId);
            ioSerializer.Enum("type", ref TankType);
            ioSerializer.UInt32ProxyArray("critters", ref CritterIds);
            ioSerializer.UInt32Proxy("environment", ref EnvironmentId);
            ioSerializer.Enum("flags'", ref Settings);
            ioSerializer.Int64Proxy("start", ref Start);
            ioSerializer.Int64Proxy("duration", ref Duration);
        }

        #endregion // ISerializedObject
    }
}