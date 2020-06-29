using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace ProtoAqua.Energy
{
    [Serializable]
    public struct ActorConversion
    {
        [ActorTypeId] public FourCC ActorId;
        public float Conversion;
        public float Weight;
    }
}