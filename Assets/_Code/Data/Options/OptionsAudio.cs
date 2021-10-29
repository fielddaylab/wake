using System;
using AquaAudio;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua.Option
{
    /// <summary>
    /// Audio options settings.
    /// </summary>
    public struct OptionsAudio : ISerializedObject, ISerializedVersion
    {
        static private readonly AudioBusId[] BusIds = new AudioBusId[] { AudioBusId.Master, AudioBusId.SFX, AudioBusId.Music, AudioBusId.Voice };
        static public ListSlice<AudioBusId> Sources() { return BusIds; }

        public OptionAudioBus Master;
        public OptionAudioBus SFX;
        public OptionAudioBus Music;
        public OptionAudioBus Voice;
        public bool Mono;

        public OptionAudioBus this[AudioBusId inBusId]
        {
            get
            {
                switch(inBusId)
                {
                    case AudioBusId.Master:
                        return Master;
                    case AudioBusId.SFX:
                        return SFX;
                    case AudioBusId.Music:
                        return Music;
                    case AudioBusId.Voice:
                        return Voice;
                    default:
                        throw new ArgumentOutOfRangeException("inBusId");
                }
            }
            set
            {
                switch(inBusId)
                {
                    case AudioBusId.Master:
                        Master = value;
                        break;
                    case AudioBusId.SFX:
                        SFX = value;
                        break;
                    case AudioBusId.Music:
                        Music = value;
                        break;
                    case AudioBusId.Voice:
                        Voice = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("inBusId");
                }
            }
        }

        public ushort Version { get { return 3; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Object("master", ref Master);
            ioSerializer.Object("sfx", ref SFX);
            ioSerializer.Object("music", ref Music);
            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.Object("voice", ref Voice);
            }
            else
            {
                Voice.SetDefaults(0.8f);
            }

            if (ioSerializer.ObjectVersion >= 3)
            {
                ioSerializer.Serialize("mono", ref Mono);
            }
        }

        public void SetDefaults()
        {
            Master.SetDefaults(0.9f);
            SFX.SetDefaults(0.8f);
            Music.SetDefaults(0.8f);
            Voice.SetDefaults(0.8f);
            Mono = false;
        }
    }

    /// <summary>
    /// Audio bus options.
    /// </summary>
    public struct OptionAudioBus : ISerializedObject
    {
        public float Volume;
        public bool Mute;

        public void SetDefaults(float inVolume)
        {
            Volume = inVolume;
            Mute = false;
        }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("volume", ref Volume);
            ioSerializer.Serialize("mute", ref Mute);
        }

        static public OptionAudioBus Create(in AudioPropertyBlock inPropertyBlock)
        {
            OptionAudioBus audio;
            audio.Volume = inPropertyBlock.Volume;
            audio.Mute = inPropertyBlock.Mute;
            return audio;
        }

        public void Apply(ref AudioPropertyBlock ioPropertyBlock)
        {
            ioPropertyBlock.Volume = Volume;
            ioPropertyBlock.Mute = Mute;
        }
    }
}
