using System;
using BeauUtil;
using UnityEngine;


namespace Aqua
{
    [RequireComponent(typeof(ColorGroup))]
    public class SiteDayNightShaderController : TimeAnimatedObject
    {
        #region Inspector

        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette LightPalette;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette ShadowPalette;

        #endregion // Inspector

        public override void OnTimeChanged(GTDate inGameTime)
        {
            Shader.SetGlobalColor(ShaderPalettes.LightColor, LightPalette.GetValueForTime(inGameTime));
            Shader.SetGlobalColor(ShaderPalettes.ShadowColor, ShadowPalette.GetValueForTime(inGameTime));
        }

        public override TimeEvent EventMask()
        {
            return TimeEvent.Transitioning;
        }
    }
}
