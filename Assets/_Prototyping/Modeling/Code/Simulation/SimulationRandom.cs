namespace ProtoAqua.Modeling
{
    public struct SimulationRandom
    {
        public uint Seed;

        public SimulationRandom(uint inSeed)
        {
            Seed = inSeed;
        }

        public uint Next()
        {
            uint next = Seed;
            next ^= next << 13;
            next ^= next >> 7;
            next ^= next << 17;
            return Seed = next;
        }

        public uint Next(uint inMax)
        {
            return Next() % inMax;
        }

        public float NextFloat()
        {
            return Next() / (float) uint.MaxValue;
        }
    }
}