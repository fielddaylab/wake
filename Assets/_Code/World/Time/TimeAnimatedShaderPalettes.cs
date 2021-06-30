using System;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class TimeAnimatedShaderPalettes : TimeAnimatedObject
    {
        #region Inspector

        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette WorldLightPalette;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette WorldShadowPalette;
        [Space]
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette ActorLightPalette;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette ActorShadowPalette;
        [Space]
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette SeaColor0Palette;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette SeaColor1Palette;
        [Space]
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette SkyColor0Palette;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette SkyColor1Palette;

        #endregion // Inspector

        public override void OnTimeChanged(GTDate inGameTime)
        {
            Shader.SetGlobalColor(ShaderPalettes.WorldLightColor, WorldLightPalette.Evaluate(inGameTime));
            Shader.SetGlobalColor(ShaderPalettes.WorldShadowColor, WorldShadowPalette.Evaluate(inGameTime));
            Shader.SetGlobalColor(ShaderPalettes.ActorLightColor, ActorLightPalette.Evaluate(inGameTime));
            Shader.SetGlobalColor(ShaderPalettes.ActorShadowColor, ActorShadowPalette.Evaluate(inGameTime));
            Shader.SetGlobalColor(ShaderPalettes.SeaColor0, SeaColor0Palette.Evaluate(inGameTime));
            Shader.SetGlobalColor(ShaderPalettes.SeaColor1, SeaColor1Palette.Evaluate(inGameTime));
            Shader.SetGlobalColor(ShaderPalettes.SkyColor0, SkyColor0Palette.Evaluate(inGameTime));
            Shader.SetGlobalColor(ShaderPalettes.SkyColor1, SkyColor1Palette.Evaluate(inGameTime));
        }

        public override TimeEvent EventMask()
        {
            return TimeEvent.Transitioning;
        }
    }
}