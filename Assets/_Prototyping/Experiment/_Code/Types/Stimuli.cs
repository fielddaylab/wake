using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ProtoAqua.Experiment
{
    /// <summary>
    /// A single stimuli.
    /// </summary>
    public struct Stimuli
    {
        public StringHash Id;
        public float Intensity;
        public StimuliArgs Arguments;
        
        [StructLayout(LayoutKind.Explicit)]
        public struct StimuliArgs
        {
            [FieldOffset(0)]
            public StringHash MemoryId;

            [FieldOffset(0)]
            public Vector2 SensorPosition;

            [FieldOffset(4)]
            public float SensorAccuracy;
        }
    }
}