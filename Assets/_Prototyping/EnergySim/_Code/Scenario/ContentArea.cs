using System;

namespace ProtoAqua.Energy
{
    [Flags]
    public enum ContentArea : byte
    {
        Photosynthesis  = 0x001,
        FoodWeb         = 0x002,
        Adaptation      = 0x004
    }
}