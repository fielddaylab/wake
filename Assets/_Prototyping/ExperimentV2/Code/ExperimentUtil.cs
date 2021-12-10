using System.Collections;
using Aqua;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

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
            WaterPropertyBlockF32 envProperties = Assets.Bestiary(inExperiment.EnvironmentId).GetEnvironment();
            foreach(var critterType in inExperiment.CritterIds)
            {
                if (Assets.Bestiary(critterType).EvaluateActorState(envProperties, out var _) == ActorStateId.Dead)
                    return true;
            }

            return false;
        }

        static public ExperimentFactResult NewFact(StringHash32 inFactId)
        {
            var bestiaryData = Save.Bestiary;
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
            var bestiaryData = Save.Bestiary;
            if (!bestiaryData.HasFact(inFactId))
                return default(ExperimentFactResult);
            if ((bestiaryData.GetDiscoveredFlags(inFactId) & inFlags) != inFlags)
                return new ExperimentFactResult(inFactId, ExperimentFactResultType.UpgradedFact, inFlags);
            return new ExperimentFactResult(inFactId, ExperimentFactResultType.Known, BFDiscoveredFlags.None);
        }

        static public IEnumerator AnimateFeedbackItemToOn(MonoBehaviour inBehavior, float inAlpha = 1)
        {
            RectTransform transform = (RectTransform) inBehavior.transform;
            CanvasGroup fader = inBehavior.GetComponent<CanvasGroup>();
            Assert.NotNull(fader);
            transform.SetScale(1.05f, Axis.XY);
            fader.alpha = 0;
            return Routine.Combine(transform.ScaleTo(1, 0.15f, Axis.XY),
                fader.FadeTo(inAlpha, 0.15f)
            );
        }

        static public void TriggerExperimentScreenViewed(SelectableTank inTank, StringHash32 inScreenId)
        {
            using(var table = TempVarTable.Alloc())
            {
                table.Set("tankType", inTank.Type.ToString());
                table.Set("tankId", inTank.Id);
                table.Set("screenId", inScreenId);
                Services.Script.TriggerResponse(ExperimentTriggers.ExperimentScreenViewed, table);
            }
        }
    }
}