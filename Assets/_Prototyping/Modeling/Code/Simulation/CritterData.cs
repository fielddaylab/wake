using System;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Tracked data for a critter during simulation.
    /// </summary>
    public struct CritterData
    {
        public uint Population;
        public WaterPropertyBlockF32 ToProduce;
        public WaterPropertyBlockF32 ToConsume;
        public ActorStateId State;
        public bool AttemptedEat;
    }
}