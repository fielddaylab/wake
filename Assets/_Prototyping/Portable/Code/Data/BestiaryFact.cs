using System;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Portable
{
    [Serializable]
    public class BestiaryFactBase
    {
        public SerializedHash32 Id;
        public string Label;
        public Sprite Icon;
    }
}