using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [Serializable]
    public struct ActorCount : IKeyValuePair<StringHash32, uint>
    {
        public SerializedHash32 Id;
        public uint Population;

        public ActorCount(StringHash32 inId, uint inPopulation)
        {
            Id = inId;
            Population = inPopulation;
        }

        public StringHash32 Key { get { return Id; } }
        public uint Value { get { return Population; } }
    }
}