using BeauUtil;
using BeauUtil.Variants;

namespace ProtoAqua.Modeling
{
    static public class SimulationConsts
    {
        static public readonly TableKeyPair Var_HasScenario = TableKeyPair.Parse("modeling:hasScenario");
        static public readonly TableKeyPair Var_ModelSync = TableKeyPair.Parse("modeling:modelSync");
        static public readonly TableKeyPair Var_PredictSync = TableKeyPair.Parse("modeling:predictSync");
        static public readonly TableKeyPair Var_ModelPhase = TableKeyPair.Parse("modeling:phase");

        static public readonly StringHash32 Trigger_Started = "ModelingStarted";
        static public readonly StringHash32 Trigger_Synced = "ModelingSynced";
        static public readonly StringHash32 Trigger_Completed = "ModelingCompleted";

        static public readonly StringHash32 ModelPhase_Universal = "universal";
        static public readonly StringHash32 ModelPhase_Model = "model";
        static public readonly StringHash32 ModelPhase_Predict = "predict";
        static public readonly StringHash32 ModelPhase_Completed = "complete";

        static public readonly StringHash32 Event_Model_Begin = "modeling:model-begin";
        static public readonly StringHash32 Event_Simulation_Begin = "simulation:simulation-begin";
        static public readonly StringHash32 Event_Simulation_Complete = "simulation:simulation-complete";
    }
}