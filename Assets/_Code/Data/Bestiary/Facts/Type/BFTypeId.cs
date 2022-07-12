using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public enum BFTypeId : ushort
    {
        Body,
        State,

        Eat,
        Consume,
        Produce,
        Parasite,

        Death,
        Grow,
        Reproduce,

        Population,
        PopulationHistory,

        WaterProperty,
        WaterPropertyHistory,
        
        Model,

        Sim,

        _COUNT
    }

    public enum BFShapeId : ushort {
        None,
        Behavior,
        State,
        WaterProperty,
        WaterPropertyHistory,
        Population,
        PopulationHistory,
        Model
    }
}