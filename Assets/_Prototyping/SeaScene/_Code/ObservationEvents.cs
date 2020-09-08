using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation
{
    static public class ObservationEvents
    {
        static public readonly StringHash ScannerOn = "scanner:turn-on";
        static public readonly StringHash ScannerOff = "scanner:turn-off";

        /// <summary>
        /// has string argument
        /// </summary>
        static public readonly StringHash ScannableComplete = "scannable:complete";
    }
}