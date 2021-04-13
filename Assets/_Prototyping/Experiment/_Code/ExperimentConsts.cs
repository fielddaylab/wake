using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using UnityEngine;

namespace ProtoAqua.Experiment
{
    static public class ExperimentEvents
    {
        static public readonly StringHash32 SetupPanelOn = "experiment:setup-panel-on";
        static public readonly StringHash32 SetupPanelOff = "experiment:setup-panel-off";

        static public readonly StringHash32 MeasurementCritX = "experiment:critterx-fill";
        static public readonly StringHash32 OnMeasurementChange = "experiment:measurement-slider-change";

        static public readonly StringHash32 SetupTank = "experiment:setup-tank";
        static public readonly StringHash32 SetupInitialSubmit = "experiment:setup-initialSubmit";
        
        static public readonly StringHash32 SetupAddActor = "experiment:setup-addActor";
        static public readonly StringHash32 SetupRemoveActor = "experiment:setup-removeActor";

        static public readonly StringHash32 SetupAddWaterProperty = "experiment:setup-addProperty";
        static public readonly StringHash32 SetupRemoveWaterProperty = "experiment:setup-removeProperty";

        static public readonly StringHash32 StressorColor = "experiment:stressor-color";

        static public readonly StringHash32 ExperimentBegin = "experiment:begin";
        static public readonly StringHash32 ExperimentRequestSummary = "experiment:request-summary";
        static public readonly StringHash32 ExperimentTeardown = "experiment:teardown";

        static public readonly StringHash32 SubscreenBack = "experiment:subscreen-back";

        static public readonly StringHash32 AttemptObserveBehavior = "experiment:attempt-observe-behavior";
        static public readonly StringHash32 BehaviorAddedToLog = "experiment:behavior-added-to-log";
    }

    static public class ExperimentTriggers
    {
        static public readonly StringHash32 TrySubmitHypothesis = "TrySubmitHypothesis";
        static public readonly StringHash32 TrySubmitExperiment = "TrySubmitExperiment";
        static public readonly StringHash32 TryEndExperiment = "TryEndExperiment";
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