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

        void Initialize(EnergySimState ioState, EnergySimDatabase inDatabase);
        bool TryCalculateProperty(FourCC inPropertyId, IEnergySimStateReader inReader, EnergySimDatabase inDatabase, out float outValue);
    }
}