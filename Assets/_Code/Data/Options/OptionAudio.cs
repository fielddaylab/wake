using System;
using AquaAudio;
using BeauData;
using BeauUtil;

namespace Aqua.Option
{
    /// <summary>
    /// Audio options settings.
    /// </summary>
    public struct OptionAudio : ISerializedObject
    {
        static private readonly AudioBusId[] BusIds = new AudioBusId[] { AudioBusId.Master, AudioBusId.SFX, AudioBusId.Music };
        static public ListSlice<AudioBusId> Sources() { return BusIds; }

        public OptionAudioBus Master;
        public OptionAudioBus SFX;
        public OptionAudioBus Music;

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
                    default:
                        throw new ArgumentOutOfRangeException("inBusId");
                }
            }
        }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Object("master", ref Master);
            ioSerializer.Object("sfx", ref SFX);
            ioSerializer.Object("music", ref Music);
        }

        public void SetDefaults()
        {
            Master.SetDefaults(1);
            SFX.SetDefaults(0.8f);
            Music.SetDefaults(0.8f);
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
