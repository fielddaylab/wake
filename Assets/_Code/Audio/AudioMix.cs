using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using Aqua;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauRoutine;
using Aqua.Debugging;
using BeauUtil.Debugger;
using Leaf.Runtime;
using UnityEngine.Scripting;
using BeauUWT;
using System.Runtime.InteropServices;

namespace AquaAudio
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AudioBusParameterSet {
        public float Master;
        public float SFX;
        public float Music;
        public float Ambient;
        public float Voice;

        static public readonly AudioBusParameterSet Default = new AudioBusParameterSet() {
            Master = 1,
            SFX = 1,
            Music = 1,
            Ambient = 1,
            Voice = 1
        };
    }

    public struct AudioMixState {
        public float Timer;
        public float Factor;
    }

    public class AudioBusMix {
        public AudioBusParameterSet Volume = AudioBusParameterSet.Default;
        public AudioBusParameterSet Pitch = AudioBusParameterSet.Default;
    }
}