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
        public ExperimentFactFeedbackFlags Feedback;
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
    public enum ExperimentFactFeedbackFlags : byte
    {
        MissedObservations = 0x01,
        ReproduceCategory = 0x02,
        EatCategory = 0x04,
        ChemistryCategory = 0x08
    }
}