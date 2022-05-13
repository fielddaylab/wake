using System;
using AquaAudio;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua.Option
{
    /// <summary>
    /// Performance options settings.
    /// </summary>
    public struct OptionsPerformance : ISerializedObject, ISerializedVersion
    {
        private enum FramerateMode_Deprecated : byte
        {
            Stable,
            High
        }

        public enum QualityMode : byte
        {
            Low,
            Medium,
            High
        }

        public QualityMode Resolution;

        public ushort Version { get { return 3; } }

        public void Serialize(Serializer ioSerializer)
        {
            if (ioSerializer.ObjectVersion < 3)
            {
                FramerateMode_Deprecated fr = default;
                ioSerializer.Enum("framerate", ref fr);
            }
            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.Enum("resolution", ref Resolution);
            }
            else if (ioSerializer.IsReading)
            {
                Resolution = QualityMode.High;
            }
        }

        public void SetDefaults()
        {
            Resolution = QualityMode.Medium;
        }
    }
}
