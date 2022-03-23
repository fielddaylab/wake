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
    [Serializable]
    public struct TaggingManifest
    {
        public StringHash32 Id;
        public ushort TotalInScene;
        public ushort Required;
    }
}