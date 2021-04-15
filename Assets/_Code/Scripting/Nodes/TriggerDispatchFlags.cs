using System;

namespace Aqua.Scripting
{
    [Flags]
    public enum TriggerDispatchFlags : byte
    {
        AllowRepetition = 0x01
    }
}