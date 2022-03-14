using System;
using AquaAudio;
using BeauData;
using BeauUtil;

namespace Aqua.Option
{
    /// <summary>
    /// Accessibility options.
    /// </summary>
    public struct OptionsAccessibility : ISerializedObject, ISerializedVersion
    {
        // TODO: Add accessibility option for things that require timing?

        public enum TTSMode : byte
        {
            Off,
            Tooltips,
            Full
        }

        public TTSMode TTS;
        public float TTSRate;

        public OptionAccessibilityFlags Flags;

        public void SetFlag(OptionAccessibilityFlags inFlag, bool inbValue)
        {
            if (inbValue)
            {
                Flags |= inFlag;
            }
            else
            {
                Flags &= ~inFlag;
            }
        }

        public bool HasFlag(OptionAccessibilityFlags inFlag)
        {
            return (Flags & inFlag) == inFlag;
        }

        public ushort Version { get { return 1; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Enum("tts", ref TTS);
            ioSerializer.Serialize("ttsRate", ref TTSRate, 1);
            ioSerializer.Enum("flags", ref Flags);
        }

        public void SetDefaults()
        {
            TTS = TTSMode.Off;
            TTSRate = 1;
            Flags = 0;
        }
    }

    public enum OptionAccessibilityFlags : ulong
    {
        ReduceFlashing = 0x001,
        ReduceCameraMovement = 0x002,
    }
}
