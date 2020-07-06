using System;
using BeauData;
using BeauPools;
using BeauUtil;

namespace ProtoAqua.Energy
{
    [Serializable]
    public struct VarPairF : IKeyValuePair<FourCC, float>, ISerializedObject
    {
        [VarTypeId] public FourCC Id;
        public float Value;

        #region KeyValuePair

        FourCC IKeyValuePair<FourCC, float>.Key { get { return Id; }}

        float IKeyValuePair<FourCC, float>.Value { get { return Value; } }

        #endregion // KeyValuePair

        #region ISerializedObject

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref Id);
            ioSerializer.Serialize("value", ref Value);
        }

        #endregion // ISerializedObject
    }
}