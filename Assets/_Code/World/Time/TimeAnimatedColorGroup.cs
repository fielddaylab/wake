using System;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [RequireComponent(typeof(ColorGroup))]
    public class TimeAnimatedColorGroup : TimeAnimatedObject
    {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] public ColorGroup Renderer;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette Palette;

        #endregion // Inspector

        public override void OnTimeChanged(GTDate inGameTime)
        {
            Renderer.Color = Palette.Evaluate(inGameTime);
        }

        public override TimeEvent EventMask()
        {
            return TimeEvent.Transitioning;
        }
    }
}