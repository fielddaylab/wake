using System;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public interface ITimeAnimatedValue<T> where T : struct
    {
        T Evaluate(GTDate inGameTime);
    }

    [Serializable]
    public struct TimeColorPalette : ITimeAnimatedValue<Color32>
    {
        public Color32 Morning;
        public Color32 Day;
        public Color32 Evening;
        public Color32 Night;

        public Color32 Evaluate(GTDate inGameTime)
        {
            switch(inGameTime.SubPhase)
            {
                case DaySubPhase.NightToMorning:
                    return Color32.Lerp(Night, Morning, inGameTime.SubPhaseProgress);
                case DaySubPhase.Morning:
                    return Morning;
                case DaySubPhase.MorningToDay:
                    return Color32.Lerp(Morning, Day, inGameTime.SubPhaseProgress);
                case DaySubPhase.Day:
                    return Day;
                case DaySubPhase.DayToEvening:
                    return Color32.Lerp(Day, Evening, inGameTime.SubPhaseProgress);
                case DaySubPhase.Evening:
                    return Evening;
                case DaySubPhase.EveningToNight:
                    return Color32.Lerp(Evening, Night, inGameTime.SubPhaseProgress);
                case DaySubPhase.Night:
                    return Night;
                
                default:
                    Assert.Fail("Unknown phase {0}", inGameTime.SubPhase);
                    return default(Color32);
            }
        }

        static public TimeColorPalette Darken(TimeColorPalette inPalette, float inRatio)
        {
            return new TimeColorPalette()
            {
                Morning = Darken(inPalette.Morning, inRatio),
                Day = Darken(inPalette.Day, inRatio),
                Evening = Darken(inPalette.Evening, inRatio),
                Night = Darken(inPalette.Night, inRatio),
            };
        }

        static private Color32 Darken(Color32 inColor, float inRatio)
        {
            Color newColor = (Color) inColor * inRatio;
            newColor.a = inColor.a;
            return newColor;
        }
    }

    [Serializable]
    public struct TimeFloatPalette : ITimeAnimatedValue<float>
    {
        public float Morning;
        public float Day;
        public float Evening;
        public float Night;

        public float Evaluate(GTDate inGameTime)
        {
            switch(inGameTime.SubPhase)
            {
                case DaySubPhase.NightToMorning:
                    return Mathf.Lerp(Night, Morning, inGameTime.SubPhaseProgress);
                case DaySubPhase.Morning:
                    return Morning;
                case DaySubPhase.MorningToDay:
                    return Mathf.Lerp(Morning, Day, inGameTime.SubPhaseProgress);
                case DaySubPhase.Day:
                    return Day;
                case DaySubPhase.DayToEvening:
                    return Mathf.Lerp(Day, Evening, inGameTime.SubPhaseProgress);
                case DaySubPhase.Evening:
                    return Evening;
                case DaySubPhase.EveningToNight:
                    return Mathf.Lerp(Evening, Night, inGameTime.SubPhaseProgress);
                case DaySubPhase.Night:
                    return Night;
                
                default:
                    Assert.Fail("Unknown phase {0}", inGameTime.SubPhase);
                    return default(float);
            }
        }
    }
}