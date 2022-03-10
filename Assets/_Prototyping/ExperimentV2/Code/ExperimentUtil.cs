using System.Collections;
using Aqua;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.ExperimentV2 {
    static public class ExperimentUtil {
        static public ExperimentResult Evaluate(RunningExperimentData inExperiment, ActorWorld inWorld) {
            switch (inExperiment.TankType) {
                case RunningExperimentData.Type.Measurement:
                    return MeasurementTank.Evaluate(inExperiment, inWorld);
                default:
                    Assert.Fail("Unhandled experiment type {0}", inExperiment.TankType);
                    return null;
            }
        }

        static public ExperimentFactResult NewFact(StringHash32 inFactId) {
            var bestiaryData = Save.Bestiary;
            if (bestiaryData.HasFact(inFactId)) {
                return new ExperimentFactResult(inFactId, ExperimentFactResultType.Known, BFDiscoveredFlags.None);
            } else {
                return new ExperimentFactResult(inFactId, ExperimentFactResultType.NewFact, BFDiscoveredFlags.Base);
            }
        }

        static public ExperimentFactResult NewFactFlags(StringHash32 inFactId, BFDiscoveredFlags inFlags) {
            var bestiaryData = Save.Bestiary;
            if (!bestiaryData.HasFact(inFactId))
                return default(ExperimentFactResult);
            if ((bestiaryData.GetDiscoveredFlags(inFactId) & inFlags) != inFlags)
                return new ExperimentFactResult(inFactId, ExperimentFactResultType.UpgradedFact, inFlags);
            return new ExperimentFactResult(inFactId, ExperimentFactResultType.Known, BFDiscoveredFlags.None);
        }

        static public void TriggerExperimentScreenViewed(SelectableTank inTank, StringHash32 inScreenId) {
            using (var table = TempVarTable.Alloc()) {
                table.Set("tankType", inTank.Type.ToString());
                table.Set("tankId", inTank.Id);
                table.Set("screenId", inScreenId);
                Services.Script.TriggerResponse(ExperimentTriggers.ExperimentScreenViewed, table);
            }
        }

        static public void TriggerExperimentScreenExited(SelectableTank inTank, StringHash32 inScreenId) {
            using (var table = TempVarTable.Alloc()) {
                table.Set("tankType", inTank.Type.ToString());
                table.Set("tankId", inTank.Id);
                table.Set("screenId", inScreenId);
                Services.Script.TriggerResponse(ExperimentTriggers.ExperimentScreenExited, table);
            }
        }

        static public Future<StringHash32> DisplaySummaryPopup(ExperimentResult result) {
            if (result.Facts.Length > 0) {
                return Services.UI.Popup.PresentFacts(
                    Loc.Find("experiment.summary.header"),
                    null, null,
                    ArrayUtils.MapFrom(result.Facts, (f) => Assets.Fact(f.Id)), ArrayUtils.MapFrom(result.Facts, (f) => f.Flags), 0
                );
            } else {
                TempList16<TextId> hints = default;
                if ((result.Feedback & ExperimentFeedbackFlags.NoNewObservations) != 0) {
                    hints.Add("experiment.summary.noFacts");
                }
                if ((result.Feedback & ExperimentFeedbackFlags.MissedObservations) != 0) {
                    hints.Add("experiment.summary.missedFacts");
                }
                if ((result.Feedback & ExperimentFeedbackFlags.ChemistryCategory) != 0) {
                    hints.Add("experiment.summary.measure.water");
                }
                if ((result.Feedback & ExperimentFeedbackFlags.ReproduceCategory) != 0) {
                    hints.Add("experiment.summary.measure.repro");
                }
                if ((result.Feedback & ExperimentFeedbackFlags.EatCategory) != 0) {
                    hints.Add("experiment.summary.measure.eat");
                }
                if ((result.Feedback & ExperimentFeedbackFlags.DeadOrganisms) != 0) {
                    hints.Add("experiment.summary.deadOrganisms");
                }
                if ((result.Feedback & ExperimentFeedbackFlags.SingleOrganism) != 0) {
                    hints.Add("experiment.summary.singleOrganism");
                }
                if ((result.Feedback & ExperimentFeedbackFlags.DeadMatter) != 0) {
                    hints.Add("experiment.summary.deadMatter");
                }
                if ((result.Feedback & ExperimentFeedbackFlags.DeadMatterEatPair) != 0) {
                    hints.Add("experiment.summary.deadMatterPair");
                }
                if ((result.Feedback & ExperimentFeedbackFlags.EatNeedsObserve) != 0) {
                    hints.Add("experiment.summary.eatNeedsObserveFirst");
                }
                TextId noteBase = "experiment.summary.noteHeader";
                if ((result.Feedback & ExperimentFeedbackFlags.NoInteraction) != 0) {
                    noteBase = "experiment.summary.noInteractionHeader";
                }
                TextId hint = RNG.Instance.Choose(hints);
                return Services.UI.Popup.Display(
                    Loc.Find("experiment.summary.header.fail"),
                    Loc.Format(noteBase, hint), null, 0
                );
            }
        }
    }
}