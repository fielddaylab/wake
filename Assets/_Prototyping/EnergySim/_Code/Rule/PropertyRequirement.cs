using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace ProtoAqua.Energy
{
    [Serializable]
    public struct PropertyRequirement
    {
        [VarTypeId] public FourCC PropertyId;
        public CompareOp Comparison;
        public float BaseValue;
        public float MassValue;

        public bool Evaluate(ushort inMass, in EnergySimContext inContext)
        {
            int propIdx = inContext.Database.Properties.IdToIndex(PropertyId);
            return Comparison.Evaluate(inContext.CachedCurrent.Environment.Properties[propIdx], (BaseValue + MassValue * inMass));
        }

        static public bool Any(IReadOnlyList<PropertyRequirement> inRequirements, ushort inMass, in EnergySimContext inContext)
        {
            for (int reqIdx = 0; reqIdx < inRequirements.Count; ++reqIdx)
            {
                PropertyRequirement req = inRequirements[reqIdx];
                if (req.Evaluate(inMass, inContext))
                    return true;
            }

            return false;
        }

        static public bool All(IReadOnlyList<PropertyRequirement> inRequirements, ushort inMass, in EnergySimContext inContext)
        {
            for (int reqIdx = 0; reqIdx < inRequirements.Count; ++reqIdx)
            {
                PropertyRequirement req = inRequirements[reqIdx];
                if (!req.Evaluate(inMass, inContext))
                    return false;
            }

            return true;
        }
    }
}