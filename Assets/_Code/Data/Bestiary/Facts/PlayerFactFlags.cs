using System;
using BeauData;
using BeauUtil;
using BeauUtil.Variants;

namespace Aqua
{
    [Flags]
    public enum PlayerFactFlags : uint
    {
        KnowState = 0x001,
        KnowQualitative = 0x002,
        KnowQuantitative = 0x004,
        KnowVariants = 0x008,

        [Hidden]
        All = KnowState | KnowQualitative | KnowQuantitative | KnowVariants
    }
}