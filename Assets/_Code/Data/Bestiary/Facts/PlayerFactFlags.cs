using System;
using BeauData;
using BeauUtil;
using BeauUtil.Variants;

namespace Aqua
{
    [Flags]
    public enum PlayerFactFlags : byte
    {
        KnowValue = 0x01,
        
        [Hidden]
        All = KnowValue
    }
}