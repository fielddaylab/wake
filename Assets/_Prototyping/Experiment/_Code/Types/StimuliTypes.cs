using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using System.Collections.Generic;

namespace ProtoAqua.Experiment
{
    static public class StimuliTypes
    {
        static public readonly StringHash32 Sleepy = "Sleepy";
        static public readonly StringHash32 LowOxygen = "LowOxygen";
        static public readonly StringHash32 LowEnergy = "LowEnergy";
        static public readonly StringHash32 SensedObject = "SensedObject";
        static public readonly StringHash32 LostTrackOfObject = "LostTrackOfObject";
    }
}