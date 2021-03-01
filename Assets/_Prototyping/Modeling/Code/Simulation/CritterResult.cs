using System;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Critter stats after a simulator tick.
    /// </summary>
    public struct CritterResult
    {
        public StringHash32 Id;
        public uint Population;
        public ActorStateId State;
    }
}