using System;
using BeauData;
using BeauUtil;
using BeauUtil.Variants;

namespace Aqua
{
    [Flags]
    public enum InvItemFlags : byte
    {
        Artifact,

        Cash,

        Gear,

        None
    }
}