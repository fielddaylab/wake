using Aqua;
using BeauUtil;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Detailed critter eating details.
    /// </summary>
    public struct CritterEatDetails
    {
        public ushort Eater;
        public ushort Eaten;
        public uint Population;

        public CritterEatDetails(ushort inEater, ushort inEaten, uint inPopulation)
        {
            Eater = inEater;
            Eaten = inEaten;
            Population = inPopulation;
        }
    }
}