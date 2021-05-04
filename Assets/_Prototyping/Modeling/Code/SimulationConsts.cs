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

        static public readonly StringHash32 Trigger_ConceptStarted = "UniversalModelStarted";
        static public readonly StringHash32 Trigger_GraphStarted = "ModelGraphStarted";
        static public readonly StringHash32 Trigger_SyncedImmediate = "ModelSyncedImmediate";
        static public readonly StringHash32 Trigger_Synced = "ModelSynced";
        static public readonly StringHash32 Trigger_PredictImmediate = "ModelPredictImmediate";
        static public readonly StringHash32 Trigger_GraphCompleted = "ModelCompleted";

        static public readonly StringHash32 ModelPhase_Universal = "universal";
        static public readonly StringHash32 ModelPhase_Model = "model";
        static public readonly StringHash32 ModelPhase_Predict = "predict";
        static public readonly StringHash32 ModelPhase_Completed = "complete";
    }
}