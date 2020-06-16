using System;
using BeauData;
using BeauPools;
using BeauUtil;

namespace ProtoAqua.Energy
{
    [Serializable]
    public struct ActorCount : IKeyValuePair<FourCC, uint>
    {
        [ActorTypeId] public FourCC Id;
        public uint Count;

        #region KeyValuePair

        FourCC IKeyValuePair<FourCC, uint>.Key { get { return Id; }}

        uint IKeyValuePair<FourCC, uint>.Value { get { return Count; } }

        #endregion // KeyValuePair
    }
}