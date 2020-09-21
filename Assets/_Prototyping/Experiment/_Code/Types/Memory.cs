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
    /// <summary>
    /// A single memory.
    /// </summary>
    public struct Memory
    {
        public StringHash Id;

        public float Intensity;
        public uint Timestamp;
        public uint Expiration;

        public StringHash Tag;
        public Vector2? Location;
        public float Precision;
    }
}