using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using UnityEngine;

namespace ProtoAqua.ExperimentV2
{
    static public class ExperimentEvents
    {        
        static public readonly StringHash32 ExperimentBegin = "experiment:begin";
    }

    static public class ExperimentTriggers
    {
        static public readonly StringHash32 ExperimentFinished = "ExperimentFinished";
        static public readonly StringHash32 NewBehaviorObserved = "NewBehaviorObserved";
        static public readonly StringHash32 BehaviorAlreadyObserved = "BehaviorAlreadyObserved";
        static public readonly StringHash32 ExperimentIdle = "ExperimentIdle";
    }

    static public class ExperimentVars
    {
        static public readonly TableKeyPair SetupPanelOn = TableKeyPair.Parse("experiment:setup.on");
        static public readonly TableKeyPair SetupPanelScreen = TableKeyPair.Parse("experiment:setup.screen");
        static public readonly TableKeyPair SetupPanelTankType = TableKeyPair.Parse("experiment:setup.tankType");
        static public readonly TableKeyPair SetupPanelEcoType = TableKeyPair.Parse("experiment:setup.ecoType");
        static public readonly TableKeyPair SetupPanelLastActorType = TableKeyPair.Parse("experiment:setup.lastActorType");

        static public readonly TableKeyPair TankType = TableKeyPair.Parse("experiment:tankType");
        static public readonly TableKeyPair TankTypeLabel = TableKeyPair.Parse("experiment:tankTypeLabel");
        static public readonly TableKeyPair EcoType = TableKeyPair.Parse("experiment:ecoType");
        static public readonly TableKeyPair EcoTypeLabel = TableKeyPair.Parse("experiment:ecoTypeLabel");

        static public readonly TableKeyPair ExperimentRunning = TableKeyPair.Parse("experiment:running");
        static public readonly TableKeyPair ExperimentDuration = TableKeyPair.Parse("experiment:experimentDuration");
        static public readonly TableKeyPair ExperimentBehaviorCount = TableKeyPair.Parse("experiment:observedBehaviorCount");
    }
}