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
        static public readonly StringHash32 SetupInitialSubmit = "experiment:setup-initialSubmit";
        
        static public readonly StringHash32 SetupAddActor = "experiment:setup-addActor";
        static public readonly StringHash32 SetupRemoveActor = "experiment:setup-removeActor";
        
        static public readonly StringHash32 ExperimentBegin = "experiment:begin";
        static public readonly StringHash32 ExperimentTeardown = "experiment:teardown";
    }

    static public class ExperimentTriggers
    {
        static public readonly StringHash32 TrySubmitHypothesis = "TrySubmitHypothesis";
        static public readonly StringHash32 TrySubmitExperiment = "TrySubmitExperiment";
        static public readonly StringHash32 TryEndExperiment = "TryEndExperiment";
    }

    static public class ExperimentVars
    {
        static public readonly TableKeyPair SetupPanelOn = TableKeyPair.Parse("experiment:setup.on");
        static public readonly TableKeyPair SetupPanelScreen = TableKeyPair.Parse("experiment:setup.screen");
        static public readonly TableKeyPair SetupPanelTankType = TableKeyPair.Parse("experiment:setup.tankType");
        static public readonly TableKeyPair SetupPanelEcoType = TableKeyPair.Parse("experiment:setup.ecoType");
        static public readonly TableKeyPair SetupPanelLastActorType = TableKeyPair.Parse("experiment:setup.lastActorType");

        static public readonly TableKeyPair TankType = TableKeyPair.Parse("experiment:tankType");
        static public readonly TableKeyPair EcoType = TableKeyPair.Parse("experiment:ecoType");
        static public readonly TableKeyPair ExperimentRunning = TableKeyPair.Parse("experiment:running");
    }
}