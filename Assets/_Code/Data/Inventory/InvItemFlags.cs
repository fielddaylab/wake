using System;
using BeauUtil;

namespace Aqua
{
    [Flags]
    public enum InvItemFlags : byte
    {
        [Hidden]
        None = 0x00,

        Currency = 0x01,
    }
}