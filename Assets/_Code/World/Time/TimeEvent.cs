using System;

namespace Aqua
{
    [Flags]
    public enum TimeEvent : byte
    {
        Tick = 0x01,
        Transitioning = 0x02,
        DayNightChanged = 0x04
    }
}