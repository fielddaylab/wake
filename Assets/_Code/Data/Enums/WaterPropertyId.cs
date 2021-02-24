using UnityEngine;
using System;
using BeauUtil;
namespace Aqua
{

    public enum WaterPropertyId : byte
    {
        None,
        Oxygen,
        Temperature,
        Light,
        PH,
        CarbonDioxide,
        
        Food = 16
    }

    public enum PropertyColor {
        None,
        green,
        red,
        yellow,
        purple,
        cyan,
        brown
    }
}