using System;
using System.Collections.Generic;
using Aqua.Debugging;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public enum TimeMode : byte
    {
        Normal,
        Realtime,
        
        FreezeAt0 = 16,
        FreezeAt2,
        FreezeAt4,
        FreezeAt6,
        FreezeAt8,
        FreezeAt10,
        FreezeAt12,
        FreezeAt14,
        FreezeAt16,
        FreezeAt18,
        FreezeAt20,
        FreezeAt22,
    }
}