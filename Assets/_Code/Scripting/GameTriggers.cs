using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    static public class GameTriggers
    {
        static public readonly StringHash32 PartnerTalk = "PartnerTalk";
        static public readonly StringHash32 RequestPartnerHelp = "RequestPartnerHelp";
        static public readonly StringHash32 InspectObject = "InspectObject";
        static public readonly StringHash32 SceneStart = "SceneStart";
        static public readonly StringHash32 JobStarted = "JobStarted";
        static public readonly StringHash32 JobSwitched = "JobSwitched";
        static public readonly StringHash32 JobCompleted = "JobCompleted";
        static public readonly StringHash32 JobTaskCompleted = "JobTaskCompleted";
    }
}