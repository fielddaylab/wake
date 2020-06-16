using System;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public interface IEnergySimStateReader
    {
        ushort GetEnvironmentResource(FourCC inResourceId);
        float GetEnvironmentProperty(FourCC inPropertyId);

        ushort GetActorCount(FourCC inActorId);

        FourCC GetEnvironmentType();
        ushort GetTickId();
    }
}