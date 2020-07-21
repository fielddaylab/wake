using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace ProtoAqua.Energy
{
    [Serializable]
    public struct ResourceRequirement
    {
        [VarTypeId] public FourCC ResourceId;
        public CompareOp Comparison;
        public ushort BaseValue;
        public float MassValue;

        public bool Evaluate(ushort inMass, in EnergySimContext inContext)
        {
            int resIdx = inContext.Database.Properties.IdToIndex(ResourceId);
            return Comparison.Evaluate(inContext.CachedCurrent.Environment.OwnedResources[resIdx], (BaseValue + MassValue * inMass));
        }

        static public bool Any(IReadOnlyList<ResourceRequirement> inRequirements, ushort inMass, in EnergySimContext inContext)
        {
            for (int reqIdx = 0; reqIdx < inRequirements.Count; ++reqIdx)
            {
                ResourceRequirement req = inRequirements[reqIdx];
                if (req.Evaluate(inMass, inContext))
                    return true;
            }

            return false;
        }

        static public bool All(IReadOnlyList<ResourceRequirement> inRequirements, ushort inMass, in EnergySimContext inContext)
        {
            for (int reqIdx = 0; reqIdx < inRequirements.Count; ++reqIdx)
            {
                ResourceRequirement req = inRequirements[reqIdx];
                if (!req.Evaluate(inMass, inContext))
                    return false;
            }

            return true;
        }
    }
}