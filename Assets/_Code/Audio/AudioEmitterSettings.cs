using System;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUWT;
using UnityEngine;
using UnityEngine.Serialization;
using Aqua;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace AquaAudio {
    
    public struct AudioEmitterSettings {
        [AutoEnum] public AudioEmitterMode Mode;
        [Range(0, 300)] public float MinDistance;
        [Range(0, 300)] public float MaxDistance;
        [AutoEnum] public AudioRolloffMode Rolloff;
        [Range(0, 1)] public float Despatialize;
    }

    [LabeledEnum(false)]
    public enum AudioEmitterMode : byte {
        [Label("2D")]
        Flat,

        [Label("3D")]
        World,

        [Label("3D (Relative to Listener)")]
        ListenerRelative
    }
}