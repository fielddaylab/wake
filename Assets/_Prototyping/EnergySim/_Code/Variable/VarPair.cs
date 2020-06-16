using System;
using BeauData;
using BeauPools;
using BeauUtil;

namespace ProtoAqua.Energy
{
    [Serializable]
    public struct VarPair : IKeyValuePair<FourCC, short>
    {
        [VarTypeId] public FourCC Id;
        public short Value;

        #region KeyValuePair

        FourCC IKeyValuePair<FourCC, short>.Key { get { return Id; }}

        short IKeyValuePair<FourCC, short>.Value { get { return Value; } }

        #endregion // KeyValuePair
    }
}