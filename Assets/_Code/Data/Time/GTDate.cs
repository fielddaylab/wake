using System;
using BeauData;
using BeauPools;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public struct GTDate : IEquatable<GTDate>, IComparable<GTDate>, ISerializedProxy<long>
    {
        #region Consts

        public const DayName FirstDayName = DayName.Tuesday;
        public const ushort TicksPerHour = 64;
        public const ushort TicksPerDay = TicksPerHour * 24;
        public const int MaxDayNames = 7;

        // phase thresholds

        private const ushort DayStartTicks = TicksPerHour * 6; // 6am
        private const ushort DayEndTicks = TicksPerHour * 18; // 6pm

        private const ushort PhaseMorningStart = TicksPerHour * 5; // 5am
        private const ushort PhaseDayStart = TicksPerHour * 10; // 10am
        private const ushort PhaseEveningStart = TicksPerHour * 18; // 6pm
        private const ushort PhaseNightStart = TicksPerHour * 21; // 9pm

        // derived phase lengths

        private const ushort PhaseMorningLength = PhaseDayStart - PhaseMorningStart;
        private const ushort PhaseDayLength = PhaseEveningStart - PhaseDayStart;
        private const ushort PhaseEveningLength = PhaseNightStart - PhaseEveningStart;
        private const ushort PhaseNightDayEndLength = TicksPerDay - PhaseNightStart;
        private const ushort PhaseNightLength = PhaseMorningStart + PhaseNightDayEndLength;
        private const float PhaseNightDayEndBaseProgress = (float) PhaseNightDayEndLength / PhaseNightLength;

        // phase transitions

        private const float TransitionFadeInEnd = 1f / 3;
        private const float TransitionFadeInScale = 3;
        private const float TransitionFadeOutStart = 2f / 3;
        private const float TransitionFadeOutScale = 3;

        #endregion // Consts

        private ushort m_Ticks;
        private ushort m_Day;

        public DayName DayName;
        public DayPhase Phase;
        public DaySubPhase SubPhase;
        public float SubPhaseProgress;

        #region Constructors

        public GTDate(ushort inTicks, ushort inDay)
        {
            m_Ticks = inTicks;
            m_Day = inDay;

            DayName = FindName(m_Day);
            Phase = FindPhase(inTicks, out SubPhase, out SubPhaseProgress);
        }

        public GTDate(int inHours, int inMinutes, ushort inDay)
        {
            m_Ticks = ClockToTicks(inHours, inMinutes);
            m_Day = inDay;

            DayName = FindName(m_Day);
            Phase = FindPhase(m_Ticks, out SubPhase, out SubPhaseProgress);
        }

        public GTDate(long inTicks)
        {
            m_Day = (ushort) (inTicks / TicksPerDay);
            m_Ticks = (ushort) (inTicks % TicksPerDay);

            DayName = FindName(m_Day);
            Phase = FindPhase(m_Ticks, out SubPhase, out SubPhaseProgress);
        }

        #endregion // Constructors

        /// <summary>
        /// Default time.
        /// </summary>
        static public readonly GTDate Zero = new GTDate(0, 0);

        /// <summary>
        /// Time for the start of the given day.
        /// </summary>
        static public GTDate StartOfDay(ushort inDay)
        {
            return new GTDate((ushort) 0, inDay);
        }

        #region Accessors

        /// <summary>
        /// Number of ticks.
        /// </summary>
        public int Ticks { get { return m_Ticks; } }

        /// <summary>
        /// Total number of ticks.
        /// </summary>
        public long TotalTicks { get { return m_Ticks + ((uint) m_Day * TicksPerDay); } }

        /// <summary>
        /// Cumulative day index.
        /// </summary>
        public int Day { get { return m_Day; } }

        /// <summary>
        /// Hour of the day.
        /// </summary>
        public int Hour { get { return m_Ticks / TicksPerHour; } }
        
        /// <summary>
        /// Minute of the hour.
        /// </summary>
        public int Minute { get { return (m_Ticks % TicksPerHour) * 60 / TicksPerHour; } }

        /// <summary>
        /// Hour and fraction part of the hour.
        /// </summary>
        public float HourF { get { return Hour + Minute / 60f; } }
        
        /// <summary>
        /// Progress through the phase of the day.
        /// </summary>
        public float PhaseProgress { get { return SubPhaseProgress; }}

        /// <summary>
        /// If this is considered day.
        /// </summary>
        public bool IsDay { get { return m_Ticks >= DayStartTicks && m_Ticks < DayEndTicks; } }
        
        /// <summary>
        /// If this is considered night.
        /// </summary>
        public bool IsNight { get { return m_Ticks < DayStartTicks || m_Ticks >= DayEndTicks; } }

        #endregion // Accessors

        #region Calculate

        static public DayName FindName(ushort inDayIndex)
        {
            return (DayName) (((int) FirstDayName + inDayIndex) % MaxDayNames);
        }

        /// <summary>
        /// Calculates the phase for a given tick value.
        /// </summary>
        static public DayPhase FindPhase(ushort inTicks)
        {
            return FindPhase(inTicks, out var _, out var __);
        }

        /// <summary>
        /// Calculates the phase and progress for a given tick value.
        /// </summary>
        static public DayPhase FindPhase(ushort inTicks, out DaySubPhase outSubPhase, out float outSubPhaseProgress)
        {
            if (inTicks < PhaseMorningStart)
            {
                outSubPhase = DaySubPhase.Night;
                outSubPhaseProgress = PhaseNightDayEndBaseProgress + (float) inTicks / PhaseMorningStart;
                return DayPhase.Night;
            }
            if (inTicks < PhaseDayStart)
            {
                float progress = (float) (inTicks - PhaseMorningStart) / PhaseMorningLength;
                outSubPhase = MapSubPhase(progress, DaySubPhase.Morning, out outSubPhaseProgress);
                return DayPhase.Morning;
            }
            if (inTicks < PhaseEveningStart)
            {
                outSubPhase = DaySubPhase.Day;
                outSubPhaseProgress = (float) (inTicks - PhaseDayStart) / PhaseDayLength;
                return DayPhase.Day;
            }
            if (inTicks < PhaseNightStart)
            {
                float progress = (float) (inTicks - PhaseEveningStart) / PhaseEveningLength;
                outSubPhase = MapSubPhase(progress, DaySubPhase.Evening, out outSubPhaseProgress);
                return DayPhase.Evening;
            }

            outSubPhase = DaySubPhase.Night;
            outSubPhaseProgress = (float) (inTicks - PhaseNightStart) / PhaseNightLength;
            return DayPhase.Night;
        }

        static private DaySubPhase MapSubPhase(float inProgress, DaySubPhase inBasePhase, out float outSubProgress)
        {
            if (inProgress < TransitionFadeInEnd)
            {
                outSubProgress = inProgress * TransitionFadeInScale;
                return (DaySubPhase) (inBasePhase - 1);
            }
            else if (inProgress >= TransitionFadeOutStart)
            {
                outSubProgress = (inProgress - TransitionFadeOutStart) * TransitionFadeOutScale;
                return (DaySubPhase) (inBasePhase + 1);
            }
            else
            {
                outSubProgress = (inProgress - TransitionFadeInEnd) / (TransitionFadeOutStart - TransitionFadeInEnd);
                return inBasePhase;
            }
        }

        /// <summary>
        /// Calculates the number of ticks for the given number of real seconds.
        /// </summary>
        static public float RealSecondsToTicks(float inRealSeconds, float inMinutesPerDay)
        {
            return (inRealSeconds * GTDate.TicksPerDay) / (60 * inMinutesPerDay);
        }

        /// <summary>
        /// Calculates the tick value that represents the given hour and minute.
        /// </summary>
        static public ushort ClockToTicks(int inHours, int inMinutes)
        {
            Assert.True(inHours >= 0 && inHours < 24, "Invalid hours {0}", inHours);
            Assert.True(inMinutes >= 0 && inMinutes < 60, "Invalid minutes {0}", inMinutes);

            return (ushort) (inHours + (inMinutes / 60f) * TicksPerHour);
        }

        /// <summary>
        /// Calculates the tick value that represents the given hour.
        /// </summary>
        static public ushort HoursToTicks(float inTotalHours)
        {
            Assert.True(inTotalHours >= 0 && inTotalHours < 24, "Invalid hours {0}", inTotalHours);
            
            return (ushort) (inTotalHours * TicksPerHour);
        }

        /// <summary>
        /// Calculates progress through a time period based on the current time.
        /// </summary>
        static public float Progress(GTDate inStart, GTTimeSpan inDuration, GTDate inCurrent)
        {
            return Mathf.Clamp01((inCurrent - inStart) / inDuration);
        }

        #endregion // Calculate

        #region Interfaces

        public int CompareTo(GTDate other)
        {
            if (m_Day < other.m_Day)
                return -1;
            if (m_Day > other.m_Day)
                return 1;
            return m_Ticks.CompareTo(other.m_Ticks);
        }

        public bool Equals(GTDate other)
        {
            return m_Ticks == other.m_Ticks && m_Day == other.m_Day && DayName == other.DayName && Phase == other.Phase && SubPhaseProgress == other.SubPhaseProgress;
        }

        public long GetProxyValue(ISerializerContext inContext)
        {
            return TotalTicks;
        }

        public void SetProxyValue(long inValue, ISerializerContext inContext)
        {
            this = new GTDate(inValue);
        }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("ticks", ref m_Ticks);
            ioSerializer.Serialize("days", ref m_Day);

            if (ioSerializer.IsReading)
            {
                DayName = FindName(m_Day);
                Phase = FindPhase(m_Ticks, out SubPhase, out SubPhaseProgress);
            }
        }

        #endregion // Interfaces

        #region Overrides

        public override bool Equals(object obj)
        {
            if (obj is GTDate)
            {
                return Equals((GTDate) obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return m_Day.GetHashCode() << 5 ^ m_Ticks.GetHashCode();
        }

        public string ToTimeString()
        {
            using(PooledStringBuilder psb = PooledStringBuilder.Create())
            {
                int hour = Hour;
                int minute = Minute;

                int displayHour = Hour;
                if (displayHour == 0)
                    displayHour = 12;
                else if (displayHour > 12)
                    displayHour -= 12;

                int displayMinute = (minute / 5) * 5;

                psb.Builder.Append(displayHour).Append(':');
                if (displayMinute < 10)
                    psb.Builder.Append('0');
                psb.Builder.Append(displayMinute);

                if (hour < 12)
                    psb.Builder.Append(" AM");
                else
                    psb.Builder.Append(" PM");
                return psb.ToString();
            }
        }

        public override string ToString()
        {
            using(PooledStringBuilder psb = PooledStringBuilder.Create())
            {
                int hour = Hour;
                int minute = Minute;

                int displayHour = Hour;
                if (displayHour == 0)
                    displayHour = 12;
                else if (displayHour > 12)
                    displayHour -= 12;

                int displayMinute = (minute / 5) * 5;

                psb.Builder.Append(displayHour).Append(':');
                if (displayMinute < 10)
                    psb.Builder.Append('0');
                psb.Builder.Append(displayMinute);

                if (hour < 12)
                    psb.Builder.Append(" AM");
                else
                    psb.Builder.Append(" PM");

                psb.Builder.Append(" (").Append(Phase).Append(" ").Append(Math.Round(SubPhaseProgress * 100)).Append("%)");
                psb.Builder.Append(", Day ").Append(m_Day).Append(", ").Append(DayName);
                return psb.ToString();
            }
        }

        static public bool operator==(GTDate left, GTDate right)
        {
            return left.Equals(right);
        }

        static public bool operator!=(GTDate left, GTDate right)
        {
            return !left.Equals(right);
        }

        static public bool operator<(GTDate left, GTDate right)
        {
            return left.CompareTo(right) == -1;
        }

        static public bool operator<=(GTDate left, GTDate right)
        {
            return left.CompareTo(right) <= 0;
        }

        static public bool operator>(GTDate left, GTDate right)
        {
            return left.CompareTo(right) == 1;
        }

        static public bool operator>=(GTDate left, GTDate right)
        {
            return left.CompareTo(right) >= 0;
        }

        static public GTDate operator+(GTDate left, GTTimeSpan right)
        {
            return new GTDate(left.TotalTicks + right.Ticks);
        }

        static public GTDate operator-(GTDate left, GTTimeSpan right)
        {
            return new GTDate(left.TotalTicks - right.Ticks);
        }

        static public GTTimeSpan operator-(GTDate left, GTDate right)
        {
            return new GTTimeSpan(left.TotalTicks - right.TotalTicks);
        }

        #endregion // Overrides
    }
}