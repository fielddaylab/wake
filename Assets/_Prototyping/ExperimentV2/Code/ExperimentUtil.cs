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

        static public bool IsNew(ExperimentResult result, StringHash32 factId) {
            for(int i = 0; i < result.Facts.Length; i++) {
                if (result.Facts[i].Id == factId) {
                    return result.Facts[i].Type != ExperimentFactResultType.Known;
                }
            }

            return false;
        }

        static public bool IsAnyNew(ExperimentResult result) {
            for(int i = 0; i < result.Facts.Length; i++) {
                if (result.Facts[i].Type != ExperimentFactResultType.Known) {
                    return true;
                }
            }

            return false;
        }

        static public Future<StringHash32> DisplaySummaryPopup(ExperimentResult result) {

            bool bHasNew = IsAnyNew(result);
            bool bHasFacts = result.Facts.Length > 0;
            string hintText = null;

            if (!bHasNew || !bHasFacts) {
                var hints = GetHints(result, bHasFacts && !bHasNew, out TextId hintBase);
                hintText = Loc.Format(hintBase, RNG.Instance.Choose(hints));
            }

            if (bHasFacts) {
                PopupFacts factSet = new PopupFacts(ArrayUtils.MapFrom(result.Facts, (f) => Assets.Fact(f.Id)),
                    ArrayUtils.MapFrom(result.Facts, (f) => f.Flags));
                factSet.ShowNew = (b) => IsNew(result, b.Id);
                NamedOption[] options = null;
                if (!IsAnyNew(result)) {
                    options = PopupPanel.DefaultDismiss;
                }
                return Services.UI.Popup.PresentFacts(
                    Loc.Find("experiment.summary.header"),
                    hintText, null, factSet, 0, options
                );
            } else {
                return Services.UI.Popup.Display(
                    Loc.Find("experiment.summary.header.fail"),
                    hintText, null, 0
                );
            }
        }

        static private TempList16<TextId> GetHints(ExperimentResult result, bool noNewFacts, out TextId outHintBase) {
            TempList16<TextId> hints = default;
            if (noNewFacts) {
                hints.Add("experiment.summary.noNewFacts");
            }
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
            if ((result.Feedback & ExperimentFeedbackFlags.HadObservationsRemaining) != 0) {
                hints.Add("experiment.summary.hadRemainingObservations");
            }
            TextId noteBase = "experiment.summary.noteHeader";
            if ((result.Feedback & ExperimentFeedbackFlags.NoInteraction) != 0) {
                noteBase = "experiment.summary.noInteractionHeader";
            }
            outHintBase = noteBase;
            return hints;
        }
    }
}