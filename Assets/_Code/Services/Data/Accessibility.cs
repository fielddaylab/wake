using System;
using Aqua.Option;
using AquaAudio;
using BeauUtil;
using BeauUtil.Services;
using Leaf;
using UnityEngine;

namespace Aqua
{
    static public class Accessibility
    {
        static public OptionsAccessibility.TTSMode TTSMode
        {
            get { return Services.Data.Options.Accessibility.TTS; }
        }

        static public bool TTSEnabled
        {
            get { return Services.Data.Options.Accessibility.TTS > 0; }
        }

        static public bool ReduceCameraMovement
        {
            get { return Services.Data.Options.Accessibility.HasFlag(OptionAccessibilityFlags.ReduceCameraMovement); }
        }

        static public bool Photosensitive
        {
            get { return Services.Data.Options.Accessibility.HasFlag(OptionAccessibilityFlags.ReduceFlashing); }
        }
    }
}