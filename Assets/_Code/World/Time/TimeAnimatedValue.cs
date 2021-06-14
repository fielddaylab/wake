using System;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public interface ITimeAnimatedValue<T> where T : struct
    {
        T GetValueForTime(InGameTime inGameTime);
    }

    [Serializable]
    public struct TimeColorPalette : ITimeAnimatedValue<Color32>
    {
        public Color32 Morning;
        public Color32 Day;
        public Color32 Evening;
        public Color32 Night;

        public Color32 GetValueForTime(InGameTime inGameTime)
        {
            switch(inGameTime.Phase)
            {
                case DayPhase.Day:
                    {
                        return Day;
                    }

                case DayPhase.Night:
                    {
                        return Night;
                    }

                case DayPhase.Morning:
                    {
                        var lerp = TimeUtils.DetermineLerp(inGameTime.PhaseProgress, Night, Morning, Day);
                        return Color32.Lerp(lerp.Min, lerp.Max, lerp.Lerp);
                    }

                case DayPhase.Evening:
                    {
                        var lerp = TimeUtils.DetermineLerp(inGameTime.PhaseProgress, Day, Evening, Night);
                        return Color32.Lerp(lerp.Min, lerp.Max, lerp.Lerp);
                    }

                default:
                    Assert.Fail("Unknown phase {0}", inGameTime.Phase);
                    return default(Color32);
            }
        }
    }

    [Serializable]
    public struct TimeFloatPalette : ITimeAnimatedValue<float>
    {
        public float Morning;
        public float Day;
        public float Evening;
        public float Night;

        public float GetValueForTime(InGameTime inGameTime)
        {
            switch(inGameTime.Phase)
            {
                case DayPhase.Day:
                    {
                        return Day;
                    }

                case DayPhase.Night:
                    {
                        return Night;
                    }

                case DayPhase.Morning:
                    {
                        var lerp = TimeUtils.DetermineLerp(inGameTime.PhaseProgress, Night, Morning, Day);
                        return Mathf.Lerp(lerp.Min, lerp.Max, lerp.Lerp);
                    }

                case DayPhase.Evening:
                    {
                        var lerp = TimeUtils.DetermineLerp(inGameTime.PhaseProgress, Day, Evening, Night);
                        return Mathf.Lerp(lerp.Min, lerp.Max, lerp.Lerp);
                    }

                default:
                    Assert.Fail("Unknown phase {0}", inGameTime.Phase);
                    return default(float);
            }
        }
    }
}