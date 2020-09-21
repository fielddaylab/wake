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
        static public readonly StringHash Sleepy = "Sleepy";
        static public readonly StringHash LowOxygen = "LowOxygen";
        static public readonly StringHash LowEnergy = "LowEnergy";
        static public readonly StringHash SensedObject = "SensedObject";
        static public readonly StringHash LostTrackOfObject = "LostTrackOfObject";
    }
}