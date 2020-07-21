using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public interface IEnergySimScenario : IUpdateVersioned
    {
        ushort TotalTicks();
        int TickActionCount();
        int TickScale();

        ushort Seed();

        IEnumerable<FourCC> StartingActorIds();

        void Initialize(EnergySimState ioState, ISimDatabase inDatabase, System.Random inRandom);
        bool TryCalculateProperty(FourCC inPropertyId, IEnergySimStateReader inReader, ISimDatabase inDatabase, System.Random inRandom, out float outValue);
    }
}