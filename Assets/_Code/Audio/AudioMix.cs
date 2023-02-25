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
using System.Runtime.CompilerServices;

namespace AquaAudio
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct AudioBusParameterSet {
        [Range(0, 1)] public float Master;

        [Range(0, 1)] public float SFX;
        [Range(0, 1)] public float Music;
        [Range(0, 1)] public float Ambient;
        [Range(0, 1)] public float Voice;
        [Range(0, 1)] public float Cinematic;

        static public readonly AudioBusParameterSet Default = new AudioBusParameterSet() {
            Master = 1,
            SFX = 1,
            Music = 1,
            Ambient = 1,
            Voice = 1,
            Cinematic = 1,
        };

        public float this[AudioBusId bus] {
            get {
                fixed(float* m = &Master) {
                    return *(m + (int) bus);
                }
            }
            set {
                fixed(float* m = &Master) {
                    *(m + (int) bus) = value;
                }
            }
        }

        public float this[int bus] {
            get {
                fixed(float* m = &Master) {
                    return *(m + bus);
                }
            }
            set {
                fixed(float* m = &Master) {
                    *(m + bus) = value;
                }
            }
        }

        static public void Adjust(ref AudioBusParameterSet set, float factor) {
            if (factor <= 0) {
                set = AudioBusParameterSet.Default;
            } else if (factor >= 1) {
                set.SFX *= set.Master;
                set.Music *= set.Master;
                set.Ambient *= set.Master;
                set.Voice *= set.Master;
                set.Cinematic *= set.Master;
            } else {
                set.Master = MixVal(set.Master, factor);
                set.SFX = set.Master * MixVal(set.SFX, factor);
                set.Music = set.Master * MixVal(set.Music, factor);
                set.Ambient = set.Master * MixVal(set.Ambient, factor);
                set.Voice = set.Master * MixVal(set.Voice, factor);
                set.Cinematic = set.Master * MixVal(set.Cinematic, factor);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float MixVal(float val, float t) {
            return 1 + (val - 1) * t;
        }
    }

    [Serializable]
    public class AudioMixLayerData {
        public AudioBusParameterSet Volume = AudioBusParameterSet.Default;
        public AudioBusParameterSet Pitch = AudioBusParameterSet.Default;

        [Range(0, 1)] public float Factor;
    }
}