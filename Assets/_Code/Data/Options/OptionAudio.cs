using Aqua;
using UnityEngine;
using System;
using AquaAudio;
using BeauUtil;

namespace Aqua.Option
{   
    public struct OptionAudio {
        public AudioPropertyBlock Master;
        public AudioPropertyBlock SFX;
        public AudioPropertyBlock Music;
    }

    public static class AudioSettings 
    {
        public static AudioBusId[] Sources() 
        {
            AudioBusId[] m_Ids = new AudioBusId[]
            {
                AudioBusId.Master,
                AudioBusId.Music,
                AudioBusId.SFX
            };

            return m_Ids;
        }

    }
}
