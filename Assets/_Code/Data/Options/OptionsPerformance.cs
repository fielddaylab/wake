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

        public enum ResolutionMode : byte
        {
            Minimum,
            Moderate,
            High
        }

        public FramerateMode Framerate;
        public ResolutionMode Resolution;

        // v2: added resolution mode
        public ushort Version { get { return 2; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Enum("framerate", ref Framerate);
            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.Enum("resolution", ref Resolution);
            }
            else
            {
                Resolution = ResolutionMode.Moderate;
            }
        }

        public void SetDefaults()
        {
            Framerate = FramerateMode.High;
            #if UNITY_EDITOR
            Resolution = ResolutionMode.High;
            #else
            Resolution = ResolutionMode.Moderate;
            #endif // UNITY_EDITOR
        }
    }
}
