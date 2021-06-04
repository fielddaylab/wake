using System;
using System.Collections;
using Aqua;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace AquaAudio
{
    [LabeledEnum]
    public enum AudioBusId : byte
    {
        [Hidden]
        Master,
        SFX,
        Music,
        Ambient,
        Voice,

        [Hidden]
        LENGTH
    }
}