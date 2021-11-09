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
        static public readonly TextId DoesNotReproduce = "experiment.measure.feedback.noRepro";
        static public readonly TextId NoWaterChemistry = "experiment.measure.feedback.noWaterChem";
        static public readonly TextId CannotMeasureWaterChem = "experiment.measure.feedback.cannotMeasureWaterChem";
        static public readonly TextId DeadCritters = "experiment.measure.feedback.dead";
        static public readonly TextId IsDeadMatter = "experiment.measure.feedback.isDeadMatter";

        public const uint FailureFlag = 0x01;
        public const uint NotUnlockedFlag = 0x02;

        public StringHash32 Category;
        public StringHash32 Id;
        public uint Flags;

        public ExperimentFeedback(StringHash32 inCategory, StringHash32 inId, uint inFlags)
        {
            Category = inCategory;
            Id = inId;
            Flags = inFlags;
        }
    }
}