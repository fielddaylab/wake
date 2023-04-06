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

        public enum FeatureMode : byte
        {
            Low,
            Medium,
            High
        }

        public FramerateMode Framerate;
        public ResolutionMode Resolution;
        public FeatureMode AnimationQuality;
        public FeatureMode EffectsQuality;

        // v2: added resolution mode
        // v3: added additional quality options
        public ushort Version { get { return 3; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Enum("framerate", ref Framerate);
            if (ioSerializer.ObjectVersion >= 2) {
                ioSerializer.Enum("resolution", ref Resolution);
            } else {
                Resolution = ResolutionMode.Moderate;
            }

            if (ioSerializer.ObjectVersion >= 3) {
                ioSerializer.Enum("animation", ref AnimationQuality);
                ioSerializer.Enum("effects", ref EffectsQuality);
            } else {
                AnimationQuality = FeatureMode.Medium;
                EffectsQuality = FeatureMode.High;
            }
        }

        public void SetDefaults()
        {
            Framerate = FramerateMode.High;
            
            #if UNITY_EDITOR
            Resolution = ResolutionMode.High;
            AnimationQuality = FeatureMode.High;
            EffectsQuality = FeatureMode.High;
            #else
            Resolution = ResolutionMode.Moderate;
            AnimationQuality = FeatureMode.Medium;
            EffectsQuality = FeatureMode.Medium;
            #endif // UNITY_EDITOR
        }
    }
}
