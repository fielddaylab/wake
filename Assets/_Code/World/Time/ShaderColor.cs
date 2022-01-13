using System;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [Serializable]
    public struct ShaderColor
    {
        [FormerlySerializedAs("Day")] public Color32 Color;

        static public ShaderColor Darken(ShaderColor inPalette, float inRatio)
        {
            return new ShaderColor()
            {
                Color = Darken(inPalette.Color, inRatio),
            };
        }

        static private Color32 Darken(Color32 inColor, float inRatio)
        {
            Color newColor = (Color) inColor * inRatio;
            newColor.a = inColor.a;
            return newColor;
        }
    }
}