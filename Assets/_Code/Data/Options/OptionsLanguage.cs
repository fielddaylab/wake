using System;
using AquaAudio;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua.Option
{
    /// <summary>
    /// Language options settings.
    /// </summary>
    public struct OptionsLanguage : ISerializedObject, ISerializedVersion
    {

        public FourCC LanguageCode;

        public ushort Version { get { return 7; } }


        public void Serialize(Serializer ioSerializer) {
            ioSerializer.Serialize("language", ref LanguageCode);
        }

        public void SetDefaults() {
            LanguageCode = FourCC.Parse("EN");
        }

    }
}
