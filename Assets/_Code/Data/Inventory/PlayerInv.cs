using UnityEngine;
using BeauUtil;
using System.Collections.Generic;
using System;
using BeauData;

namespace Aqua
{
    public struct PlayerInv : IKeyValuePair<StringHash32, PlayerInv>, ISerializedObject
    {
        public StringHash32 ItemId;
        public uint Count;

        [NonSerialized] public InvItem Descriptor;

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, PlayerInv>.Key { get { return ItemId; } }
        PlayerInv IKeyValuePair<StringHash32, PlayerInv>.Value { get { return this; } }

        #endregion // KeyValue

        public PlayerInv(StringHash32 inId, uint inCount, InvItem inDescriptor)
        {
            ItemId = inId;
            Count = inCount;
            Descriptor = inDescriptor;
        }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32Proxy("id", ref ItemId);
            ioSerializer.Serialize("value", ref Count, 1);
        }
    }
    
}