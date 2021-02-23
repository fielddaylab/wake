using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public struct ActorStateTransitionRange
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

        public void Encompass(BFStateRange inRange)
        {
            if (inRange.TargetState() == ActorStateId.Stressed)
            {
                AliveMin = Mathf.Max(AliveMin, inRange.MinSafe());
                AliveMax = Mathf.Min(AliveMax, inRange.MaxSafe());
            }
            else
            {
                StressedMin = Mathf.Max(StressedMin, inRange.MinSafe());
                StressedMax = Mathf.Min(StressedMax, inRange.MaxSafe());
            }
        }

        public ActorStateId Evaluate(float inValue)
        {
            if (inValue < StressedMin || inValue > StressedMax)
                return ActorStateId.Dead;
            if (inValue < AliveMin || inValue > AliveMax)
                return ActorStateId.Stressed;
            return ActorStateId.Alive;
        }
    }
}