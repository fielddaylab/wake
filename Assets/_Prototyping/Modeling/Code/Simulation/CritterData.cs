using Aqua;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Tracked data for a critter during simulation.
    /// </summary>
    public struct CritterData
    {
        public uint Population;
        public WaterPropertyBlockF32 ToConsume;
        public uint Hunger;
        public ActorStateId State;
    }
}