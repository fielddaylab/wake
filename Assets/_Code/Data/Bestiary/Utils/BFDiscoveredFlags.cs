using System;
using BeauData;
using BeauUtil;

namespace Aqua
{
    [Flags]
    public enum BFDiscoveredFlags : byte
    {
        Base = 0x01,
        Rate = 0x02,

        [Hidden]
        All = Rate,
        [Hidden]
        None = 0
    }
}