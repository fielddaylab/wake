using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Observation
{
    static public class ObservationEvents
    {
        static public readonly PropertyName ScannerOn = "scanner:turn-on";
        static public readonly PropertyName ScannerOff = "scanner:turn-off";

        /// <summary>
        /// has string argument
        /// </summary>
        static public readonly PropertyName ScannableComplete = "scannable:complete";
    }
}