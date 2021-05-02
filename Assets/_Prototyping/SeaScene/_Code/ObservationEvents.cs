using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation
{
    static public class ObservationEvents
    {
        static public readonly StringHash32 ScannerOn = "scanner:turn-on";
        static public readonly StringHash32 ScannerOff = "scanner:turn-off";
        static public readonly StringHash32 ScannerSetState = "scanner:set-state"; //bool
    }

    static public class ObservationTriggers
    {
        static public readonly StringHash32 PlayerEnterRegion = "PlayerEnterRegion";
        static public readonly StringHash32 PlayerExitRegion = "PlayerExitRegion";
    }
}