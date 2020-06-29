using System;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public interface IEnergySimStateReader
    {
        ushort GetEnvironmentResource(FourCC inResourceId, ISimDatabase inDatabase);
        float GetEnvironmentProperty(FourCC inPropertyId, ISimDatabase inDatabase);

        ushort GetActorCount(FourCC inActorId, ISimDatabase inDatabase);
        uint GetActorMass(FourCC inActorId, ISimDatabase inDatabase);

        FourCC GetEnvironmentType();
        ushort GetTickId();
    }
}