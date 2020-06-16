using System;
using BeauData;
using BeauPools;
using BeauUtil;

namespace ProtoAqua.Energy
{
    [Serializable]
    public struct VarPairF : IKeyValuePair<FourCC, float>
    {
        [VarTypeId] public FourCC Id;
        public float Value;

        #region KeyValuePair

        FourCC IKeyValuePair<FourCC, float>.Key { get { return Id; }}

        float IKeyValuePair<FourCC, float>.Value { get { return Value; } }

        #endregion // KeyValuePair
    }
}