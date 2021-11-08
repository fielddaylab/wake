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
        Parasites,

        Death,
        Grow,
        Reproduce,

        Population,
        PopulationHistory,

        WaterProperty,
        WaterPropertyHistory,
        
        Model,

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