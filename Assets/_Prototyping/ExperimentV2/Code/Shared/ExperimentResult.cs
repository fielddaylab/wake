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
        static public readonly StringHash32 MoreThanOneSpecies = "MoreThanOneSpecies";
        static public readonly StringHash32 LessThanTwoSpecies = "LessThanTwoSpecies";
        static public readonly StringHash32 MoreThanTwoSpecies = "MoreThanTwoSpecies";
        static public readonly StringHash32 AutoFeederEnabled = "AutoFeederEnabled";
        static public readonly StringHash32 AutoFeederDisabled = "AutoFeederDisabled";
        static public readonly StringHash32 StabilizerEnabled = "StabilizerEnabled";
        static public readonly StringHash32 StabilizerDisabled = "StabilizerDisabled";
        static public readonly StringHash32 DeadCritters = "DeadCritters";

        public StringHash32 Category;
        public StringHash32 Id;

        public ExperimentFeedback(StringHash32 inCategory, StringHash32 inId)
        {
            Category = inCategory;
            Id = inId;
        }
    }
}