using System;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class SerializedRule : ISerializedObject
    {
        public SerializedRule() { }

        public SerializedRule(string inId)
        {
            Id = inId;
        }

        public string Id;
        public SerializedRuleFlags Flags;
        public float Delta;

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref Id);
            ioSerializer.Enum("flags", ref Flags, (SerializedRuleFlags) 0);
            ioSerializer.Serialize("delta", ref Delta, 0f);
        }

        public bool CanBeExcluded()
        {
            return Flags == 0 && Delta == 0;
        }
    }

    [Flags]
    public enum SerializedRuleFlags : byte
    {
        Hidden = 0x01,
        Locked = 0x02
    }
}