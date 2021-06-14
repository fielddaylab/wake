using System;
using UnityEngine;

namespace Aqua
{
    static public class TimeUtils
    {
        public const float FadeInStart = 1f / 3;
        public const float FadeInScale = 3;
        public const float FadeOutStart = 2f / 3;
        public const float FadeOutScale = 3;

        public struct LerpResult<T> where T : struct
        {
            public T Min;
            public T Max;
            public float Lerp;
        }

        static public LerpResult<T> DetermineLerp<T>(float inValue, in T inLeft, in T inMid, in T inRight) where T : struct
        {
            if (inValue < FadeInStart)
            {
                return new LerpResult<T>()
                {
                    Min = inLeft,
                    Max = inMid,
                    Lerp = inValue * FadeInScale
                };
            }
            else if (inValue >= FadeOutStart)
            {
                return new LerpResult<T>()
                {
                    Min = inMid,
                    Max = inRight,
                    Lerp = (inValue - FadeOutStart) * FadeOutScale
                };
            }
            else
            {
                return new LerpResult<T>()
                {
                    Min = inMid,
                    Max = inMid,
                    Lerp = 0
                };
            }
        }
    }
}