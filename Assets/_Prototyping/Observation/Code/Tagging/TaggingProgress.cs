using System;
using UnityEngine;
using BeauRoutine;
using System.Collections;
using UnityEngine.UI;
using Aqua;
using BeauUtil;
using BeauPools;
using System.Collections.Generic;
using Aqua.Debugging;
using Aqua.Cameras;
using Aqua.Profile;

namespace ProtoAqua.Observation
{
    public struct TaggingProgress
    {
        public StringHash32 Id;
        public ushort TotalInScene;
        public ushort Tagged;
        public Fraction16 Proportion;
    }
}