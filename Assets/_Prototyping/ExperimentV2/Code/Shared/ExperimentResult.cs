using Aqua;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using System;

namespace ProtoAqua.ExperimentV2
{
    public class ExperimentResult
    {
        public ExperimentFactResult[] Facts;
        public ExperimentFeedbackFlags Feedback;
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

    [Flags]
    public enum ExperimentFeedbackFlags : ushort
    {
        NoNewObservations = 2 << 0,
        MissedObservations = 2 << 1,
        ReproduceCategory = 2 << 2,
        EatCategory = 2 << 3,
        ChemistryCategory = 2 << 4,
        DeadOrganisms = 2 << 5,
        SingleOrganism = 2 << 6,
        DeadMatter = 2 << 7,
        DeadMatterEatPair = 2 << 8,
        NoInteraction = 2 << 9,
        EatNeedsObserve = 2 << 10,
    }
}