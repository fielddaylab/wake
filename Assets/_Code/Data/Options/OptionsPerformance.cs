using System;
using AquaAudio;
using BeauData;
using BeauUtil;

namespace Aqua.Option
{
    /// <summary>
    /// Performance options settings.
    /// </summary>
    public struct OptionsPerformance : ISerializedObject, ISerializedVersion
    {
        public enum FramerateMode : byte
        {
            Stable,
            High
        }

        public FramerateMode Framerate;

        public ushort Version { get { return 1; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Enum("framerate", ref Framerate);
        }

        public void SetDefaults()
        {
            Framerate = FramerateMode.Stable;
        }
    }
}
