using System;
using BeauData;
using BeauPools;
using BeauUtil;

namespace ProtoAqua.Energy
{
    [Serializable]
    public struct ActorCount : IKeyValuePair<FourCC, uint>, ISerializedObject
    {
        [ActorTypeId] public FourCC Id;
        public uint Count;

        #region KeyValuePair

        FourCC IKeyValuePair<FourCC, uint>.Key { get { return Id; }}

        uint IKeyValuePair<FourCC, uint>.Value { get { return Count; } }

        #endregion // KeyValuePair

        #region ISerializedObject

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("id", ref Id);
            ioSerializer.Serialize("count", ref Count);
        }

        #endregion // ISerializedObject
    }
}