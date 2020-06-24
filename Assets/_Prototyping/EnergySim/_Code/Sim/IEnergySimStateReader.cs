using System;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public interface IEnergySimStateReader
    {
        ushort GetEnvironmentResource(FourCC inResourceId, EnergySimDatabase inDatabase);
        float GetEnvironmentProperty(FourCC inPropertyId, EnergySimDatabase inDatabase);

        ushort GetActorCount(FourCC inActorId, EnergySimDatabase inDatabase);
        uint GetActorMass(FourCC inActorId, EnergySimDatabase inDatabase);

        FourCC GetEnvironmentType();
        ushort GetTickId();
    }
}