using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ActorStateTransitionRange : IEquatable<ActorStateTransitionRange>
    {
        public float AliveMin;
        public float AliveMax;
        public float StressedMin;
        public float StressedMax;

        public void Reset()
        {
            AliveMin = StressedMin = float.NegativeInfinity;
            AliveMax = StressedMax = float.PositiveInfinity;
        }

        public ActorStateId Evaluate(float inValue)
        {
            if (inValue < StressedMin || inValue > StressedMax)
                return ActorStateId.Dead;
            if (inValue < AliveMin || inValue > AliveMax)
                return ActorStateId.Stressed;
            return ActorStateId.Alive;
        }

        public bool Equals(ActorStateTransitionRange other)
        {
            return AliveMin == other.AliveMin
                && AliveMax == other.AliveMax
                && StressedMin == other.StressedMin
                && StressedMax == other.StressedMax;
        }

        static ActorStateTransitionRange()
        {
            s_DefaultRange = new ActorStateTransitionRange();
            s_DefaultRange.Reset();
        }

        static private readonly ActorStateTransitionRange s_DefaultRange;

        static public ActorStateTransitionRange Default { get { return s_DefaultRange; } }
    }

    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ActorStateTransitionSet
    {
        public ActorStateTransitionRange Oxygen;
        public ActorStateTransitionRange Temperature;
        public ActorStateTransitionRange Light;
        public ActorStateTransitionRange PH;
        public ActorStateTransitionRange CarbonDioxide;

        public void Reset()
        {
            Oxygen.Reset();
            Temperature.Reset();
            Light.Reset();
            PH.Reset();
            CarbonDioxide.Reset();
        }

        public unsafe ActorStateTransitionRange this[WaterPropertyId inId]
        {
            get
            {
                Assert.True(inId >= 0 && inId < WaterPropertyId.TRACKED_COUNT);
                
                fixed(ActorStateTransitionRange* start = &this.Oxygen)
                {
                    return *((ActorStateTransitionRange*) (start + (int) inId));
                }
            }
            set
            {
                Assert.True(inId >= 0 && inId < WaterPropertyId.TRACKED_COUNT);

                fixed(ActorStateTransitionRange* start = &this.Oxygen)
                {
                    *((ActorStateTransitionRange*) (start + (int) inId)) = value;
                }
            }
        }

        public ActorStateId Evaluate(in WaterPropertyBlockF32 inEnvironment)
        {
            return Evaluate(inEnvironment, out var _);
        }

        public ActorStateId Evaluate(in WaterPropertyBlockF32 inEnvironment, out WaterPropertyMask outAffectedRange)
        {
            ActorStateId current = ActorStateId.Alive;
            ActorStateId evaluated;
            outAffectedRange = default(WaterPropertyMask);

            if ((evaluated = Oxygen.Evaluate(inEnvironment.Oxygen)) > 0)
            {
                if (evaluated > current)
                    current = evaluated;
                outAffectedRange[WaterPropertyId.Oxygen] = true;
            }

            if ((evaluated = Temperature.Evaluate(inEnvironment.Temperature)) > 0)
            {
                if (evaluated > current)
                    current = evaluated;
                outAffectedRange[WaterPropertyId.Temperature] = true;
            }

            if ((evaluated = Light.Evaluate(inEnvironment.Light)) > 0)
            {
                if (evaluated > current)
                    current = evaluated;
                outAffectedRange[WaterPropertyId.Light] = true;
            }

            if ((evaluated = PH.Evaluate(inEnvironment.PH)) > 0)
            {
                if (evaluated > current)
                    current = evaluated;
                outAffectedRange[WaterPropertyId.PH] = true;
            }

            if ((evaluated = PH.Evaluate(inEnvironment.CarbonDioxide)) > 0)
            {
                if (evaluated > current)
                    current = evaluated;
                outAffectedRange[WaterPropertyId.CarbonDioxide] = true;
            }

            return current;
        }
    }
}