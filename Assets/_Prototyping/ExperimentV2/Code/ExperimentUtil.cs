using Aqua;
using BeauUtil;
using BeauUtil.Debugger;

namespace ProtoAqua.ExperimentV2
{
    static public class ExperimentUtil
    {
        static public ExperimentResult Evaluate(InProgressExperimentData inExperiment)
        {
            switch(inExperiment.TankType)
            {
                case InProgressExperimentData.Type.Measurement:
                    return MeasurementTank.Evaluate(inExperiment);
                default:
                    Assert.Fail("Unhandled experiment type {0}", inExperiment.TankType);
                    return null;
            }
        }
        
        static public bool AnyDead(InProgressExperimentData inExperiment)
        {
            BestiaryDB bestiaryDB = Services.Assets.Bestiary;
            WaterPropertyBlockF32 envProperties = bestiaryDB[inExperiment.EnvironmentId].GetEnvironment();
            foreach(var critterType in inExperiment.CritterIds)
            {
                if (bestiaryDB[critterType].EvaluateActorState(envProperties, out var _) == ActorStateId.Dead)
                    return true;
            }

            return false;
        }

        static public ExperimentFactResult NewFact(StringHash32 inFactId)
        {
            var bestiaryData = Services.Data.Profile.Bestiary;
            if (bestiaryData.HasFact(inFactId))
            {
                return new ExperimentFactResult(inFactId, ExperimentFactResultType.Known, BFDiscoveredFlags.None);
            }
            else
            {
                return new ExperimentFactResult(inFactId, ExperimentFactResultType.NewFact, BFDiscoveredFlags.Base);
            }
        }

        static public ExperimentFactResult NewFactFlags(StringHash32 inFactId, BFDiscoveredFlags inFlags)
        {
            var bestiaryData = Services.Data.Profile.Bestiary;
            if (!bestiaryData.HasFact(inFactId))
                return default(ExperimentFactResult);
            if ((bestiaryData.GetDiscoveredFlags(inFactId) & inFlags) != inFlags)
                return new ExperimentFactResult(inFactId, ExperimentFactResultType.UpgradedFact, inFlags);
            return new ExperimentFactResult(inFactId, ExperimentFactResultType.Known, BFDiscoveredFlags.None);
        }
    }
}