using System;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [RequireComponent(typeof(Camera))]
    public class TimeAnimatedCameraBackground : TimeAnimatedObject
    {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] public Camera Camera;
        [Inline(InlineAttribute.DisplayType.HeaderLabel)] public TimeColorPalette Palette;

        #endregion // Inspector

        public override void OnTimeChanged(GTDate inGameTime)
        {
            Camera.backgroundColor = Palette.Evaluate(inGameTime);
        }

        public override TimeEvent EventMask()
        {
            return TimeEvent.Transitioning;
        }
    }
}