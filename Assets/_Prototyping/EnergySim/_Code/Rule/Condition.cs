using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauData;
using BeauUtil;

namespace ProtoAqua.Energy
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Condition
    {
        #region Types

        public struct ActorMassData
        {
            public FourCC ActorId;
            public CompareOp Comparison;
            public uint BaseValue;
            public float MassValue;

            public bool Evaluate(ushort inMass, in EnergySimContext inContext)
            {
                int actorIdx = inContext.Database.Actors.IdToIndex(ActorId);
                return Comparison.Evaluate(inContext.Current.Masses[actorIdx], BaseValue + MassValue * inMass);
            }
        }

        public struct StarvationData
        {
            public FourCC VarId;
            public byte Threshold;

            public bool EvaluateResource(in ActorState ioState, in EnergySimContext inContext)
            {
                int resIdx = inContext.Database.Resources.IdToIndex(VarId);
                return ioState.ResourceStarvation[resIdx] >= Threshold;
            }

            public bool EvaluateProperty(in ActorState ioState, in EnergySimContext inContext)
            {
                int propIdx = inContext.Database.Properties.IdToIndex(VarId);
                return ioState.PropertyStarvation[propIdx] >= Threshold;
            }
        }

        #endregion // Types

        [FieldOffset(0)]
        public readonly ConditionType Type;

        [FieldOffset(1)]
        public readonly ResourceRequirement EnvironmentResource;

        [FieldOffset(1)]
        public readonly PropertyRequirement EnvironmentProperty;

        [FieldOffset(1)]
        public readonly ActorMassData ActorMass;

        [FieldOffset(1)]
        public readonly StarvationData ResourceStarvation;

        [FieldOffset(1)]
        public readonly StarvationData PropertyStarvation;

        public bool Evaluate(in ActorState ioState, in EnergySimContext inContext)
        {
            switch(Type)
            {
                case ConditionType.EnvironmentResource:
                    return EnvironmentResource.Evaluate(ioState.Mass, inContext);
                case ConditionType.EnvironmentProperty:
                    return EnvironmentProperty.Evaluate(ioState.Mass, inContext);
                case ConditionType.ActorMass:
                    return ActorMass.Evaluate(ioState.Mass, inContext);
                case ConditionType.ResourceStarvation:
                    return ResourceStarvation.EvaluateResource(ioState, inContext);
                case ConditionType.PropertyStarvation:
                    return PropertyStarvation.EvaluateProperty(ioState, inContext);

                case ConditionType.None:
                default:
                    return true;
            }
        }
    }

    public enum ConditionType : byte
    {
        None = 0,

        EnvironmentResource,
        EnvironmentProperty,
        ActorMass,
        ResourceStarvation,
        PropertyStarvation
    }
}