using System;
using BeauUtil;

namespace ProtoAqua.Experiment
{
    [Serializable]
    public struct SpawnCount : IKeyValuePair<StringHash32, int>
    {
        public string ActorId;
        public int ActorCount;

        public StringHash32 Key { get { return ActorId; } }
        public int Value { get { return ActorCount; } }
    }
}