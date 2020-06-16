using System;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public interface IEnergySimScenario
    {
        ushort TotalTicks();
        int TickActionCount();
        int TickScale();
    }
}