using System;
using BeauUtil;
using UnityEngine;


namespace Aqua
{
    [RequireComponent(typeof(ColorGroup))]
    public class SiteDayNightShaderController : TimeAnimatedObject
    {
        /*private void Start()
        {
            Shader.SetGlobalColor("_LightColor", LightPalette.Evening);
            Shader.SetGlobalColor("_ShadowColor", ShadowPalette.Night);
        }*/

        #region Inspector

        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette LightPalette;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette ShadowPalette;

        #endregion // Inspector

        public override void OnTimeChanged(InGameTime inGameTime)
        {
            Shader.SetGlobalColor("_LightColor", LightPalette.GetValueForTime(inGameTime));
            Shader.SetGlobalColor("_ShadowColor", ShadowPalette.GetValueForTime(inGameTime));
        }
    }
}
