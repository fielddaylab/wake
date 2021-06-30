using System;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class TimeAnimatedFog : TimeAnimatedObject
    {
        #region Inspector

        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette Palette;

        #endregion // Inspector

        public override void OnTimeChanged(GTDate inGameTime)
        {
            RenderSettings.fogColor = Palette.Evaluate(inGameTime);
        }

        public override TimeEvent EventMask()
        {
            return TimeEvent.Transitioning;
        }
    }
}