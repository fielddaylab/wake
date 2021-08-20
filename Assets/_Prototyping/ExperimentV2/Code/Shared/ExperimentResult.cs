using Aqua;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;

namespace ProtoAqua.ExperimentV2
{
    public class ExperimentResult
    {
        public ExperimentFactResult[] Facts;
        public ExperimentFeedback[] Feedback;
    }

    public struct ExperimentFactResult
    {
        public StringHash32 Id;
        public ExperimentFactResultType Type;
        public BFDiscoveredFlags Flags;

        public ExperimentFactResult(StringHash32 inFactId, ExperimentFactResultType inType, BFDiscoveredFlags inFlags)
        {
            Id = inFactId;
            Type = inType;
            Flags = inFlags;
        }
    }

    public enum ExperimentFactResultType : byte
    {
        Known,
        NewFact,
        UpgradedFact
    }

    public struct ExperimentFeedback
    {
        static public readonly TextId MoreThanOneSpecies = "experiment.measure.feedback.moreThanOne";
        static public readonly TextId LessThanTwoSpecies = "experiment.measure.feedback.lessThanTwo";
        static public readonly TextId MoreThanTwoSpecies = "experiment.measure.feedback.moreThanTwo";
        static public readonly TextId AutoFeederEnabled = "experiment.measure.feedback.feederOn";
        static public readonly TextId AutoFeederDisabled = "experiment.measure.feedback.feederOff";
        static public readonly TextId StabilizerEnabled = "experiment.measure.feedback.stabilizerOn";
        static public readonly TextId StabilizerDisabled = "experiment.measure.feedback.stabilizerOff";
        static public readonly TextId NoRelationship = "experiment.measure.feedback.noRelationship";
        static public readonly TextId NoRelationshipObserved = "experiment.measure.feedback.noRelationshipObserved";
        static public readonly TextId DeadCritters = "experiment.measure.feedback.dead";

        public StringHash32 Category;
        public StringHash32 Id;

        public ExperimentFeedback(StringHash32 inCategory, StringHash32 inId)
        {
            Category = inCategory;
            Id = inId;
        }
    }
}