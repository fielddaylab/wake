using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using UnityEngine;

namespace ProtoAqua.ExperimentV2
{
    static public class ExperimentEvents
    {   
        static public readonly StringHash32 ExperimentView = "experiment:view"; // TankType type
        static public readonly StringHash32 ExperimentBegin = "experiment:begin"; // TankType type
        static public readonly StringHash32 ExperimentEnded = "experiment:end"; // TankType type
    }

    static public class ExperimentTriggers
    {
        static public readonly StringHash32 ExperimentTankViewed = "ExperimentTankViewed";
        static public readonly StringHash32 ExperimentStarted = "ExperimentStarted";
        static public readonly StringHash32 ExperimentFinished = "ExperimentFinished";
        static public readonly StringHash32 CaptureCircleVisible = "BehaviorCaptureChance";
        static public readonly StringHash32 CaptureCircleExpired = "BehaviorCaptureChanceExpired";
        static public readonly StringHash32 ExperimentScreenViewed = "ExperimentScreenViewed";
        static public readonly StringHash32 NewBehaviorObserved = "NewBehaviorObserved";
        static public readonly StringHash32 ExperimentIdle = "ExperimentIdle";
    }
}