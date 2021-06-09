using System;
using BeauData;
using BeauPools;
using BeauUtil.Debugger;

namespace Aqua
{
    public struct InGameTime : IEquatable<InGameTime>, IComparable<InGameTime>
    {
        #region Consts

        public const ushort TicksPerHour = 32;
        public const ushort TicksPerDay = TicksPerHour * 24;
        public const int MaxDayNames = 7;

        // phase values

        private const ushort DayStartTicks = TicksPerHour * 6; // 6am
        private const ushort DayEndTicks = TicksPerHour * 18; // 6pm

        private const ushort PhaseMorningStart = TicksPerHour * 5; // 5am
        private const ushort PhaseDayStart = TicksPerHour * 10; // 10am
        private const ushort PhaseEveningStart = TicksPerHour * 17; //5pm
        private const ushort PhaseNightStart = TicksPerHour * 21; // 9pm

        private const ushort PhaseMorningLength = PhaseDayStart - PhaseMorningStart;
        private const ushort PhaseDayLength = PhaseEveningStart - PhaseDayStart;
        private const ushort PhaseEveningLength = PhaseNightStart - PhaseEveningStart;
        private const ushort PhaseNightDayEndLength = TicksPerDay - PhaseNightStart;
        private const ushort PhaseNightLength = PhaseMorningStart + PhaseNightDayEndLength;
        private const float PhaseNightDayEndBaseProgress = (float) PhaseNightDayEndLength / PhaseNightLength;

        #endregion // Consts

        private readonly ushort m_Ticks;
        private readonly ushort m_Day;

        private readonly DayName m_DayName;
        private readonly DayPhase m_DayPhase;

        #region Constructors

        public InGameTime(ushort inTicks, ushort inDay)
        {
            m_Ticks = inTicks;
            m_Day = inDay;

            m_DayName = (DayName) (inDay % MaxDayNames);
            m_DayPhase = FindPhase(inTicks);
        }

        public InGameTime(ushort inTicks, ushort inDay, DayName inDayName)
        {
            m_Ticks = inTicks;
            m_Day = inDay;

            m_DayName = inDayName;
            m_DayPhase = FindPhase(inTicks);
        }

        public InGameTime(int inHours, int inMinutes, ushort inDay)
        {
            m_Ticks = ClockToTicks(inHours, inMinutes);
            m_Day = inDay;

            m_DayName = (DayName) (inDay % MaxDayNames);
            m_DayPhase = FindPhase(m_Ticks);
        }

        public InGameTime(int inHours, int inMinutes, ushort inDay, DayName inDayName)
        {
            m_Ticks = ClockToTicks(inHours, inMinutes);
            m_Day = inDay;
            
            m_DayName = inDayName;
            m_DayPhase = FindPhase(m_Ticks);
        }

        #endregion // Constructors

        /// <summary>
        /// Default time.
        /// </summary>
        static public readonly InGameTime Zero = new InGameTime(0, 0);

        /// <summary>
        /// Time for the start of the given day.
        /// </summary>
        static public InGameTime StartOfDay(ushort inDay)
        {
            return new InGameTime((ushort) 0, inDay);
        }

        #region Accessors

        /// <summary>
        /// Number of ticks.
        /// </summary>
        public int Ticks { get { return m_Ticks; } }

        /// <summary>
        /// Cumulative day index.
        /// </summary>
        public int Day { get { return m_Day; } }
        
        /// <summary>
        /// What day of the week.
        /// </summary>
        public DayName DayName { get { return m_DayName; } }

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
        public float TotalHour { get { return Hour + Minute / 60f; } }

        /// <summary>
        /// What phase of the day.
        /// </summary>
        public DayPhase Phase { get { return m_DayPhase; } }
        
        /// <summary>
        /// Progress through the phase of the day.
        /// </summary>
        public float PhaseProgress
        {
            get
            {
                float progress;
                FindPhase(m_Ticks, out progress);
                return progress;
            }
        }

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

        /// <summary>
        /// Calculates the phase for a given tick value.
        /// </summary>
        static public DayPhase FindPhase(ushort inTicks)
        {
            float _;
            return FindPhase(inTicks, out _);
        }

        /// <summary>
        /// Calculates the phase and progress for a given tick value.
        /// </summary>
        static public DayPhase FindPhase(ushort inTicks, out float outProgress)
        {
            if (inTicks < PhaseMorningStart)
            {
                outProgress = PhaseNightDayEndBaseProgress + (float) inTicks / PhaseMorningStart;
                return DayPhase.Night;
            }
            if (inTicks < PhaseDayStart)
            {
                outProgress = (float) (inTicks - PhaseMorningStart) / PhaseMorningLength;
                return DayPhase.Morning;
            }
            if (inTicks < PhaseEveningStart)
            {
                outProgress = (float) (inTicks - PhaseDayStart) / PhaseDayLength;
                return DayPhase.Day;
            }
            if (inTicks < PhaseNightStart)
            {
                outProgress = (float) (inTicks - PhaseEveningStart) / PhaseEveningLength;
                return DayPhase.Evening;
            }

            outProgress = (float) (inTicks - PhaseNightStart) / PhaseNightLength;
            return DayPhase.Night;
        }

        /// <summary>
        /// Calculates the number of ticks for the given number of real seconds.
        /// </summary>
        static public float RealSecondsToTicks(float inRealSeconds, float inMinutesPerDay)
        {
            return (inRealSeconds * InGameTime.TicksPerDay) / (60 * inMinutesPerDay);
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

        #endregion // Calculate

        #region Interfaces

        public int CompareTo(InGameTime other)
        {
            if (m_Day < other.m_Day)
                return -1;
            if (m_Day > other.m_Day)
                return 1;
            return m_Ticks.CompareTo(other.m_Ticks);
        }

        public bool Equals(InGameTime other)
        {
            return m_Ticks == other.m_Ticks && m_Day == other.m_Day && m_DayName == other.m_DayName && m_DayPhase == other.m_DayPhase;
        }

        #endregion // Interfaces

        #region Overrides

        public override bool Equals(object obj)
        {
            if (obj is InGameTime)
            {
                return Equals((InGameTime) obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return m_Day.GetHashCode() << 5 ^ m_Ticks.GetHashCode();
        }

        public override string ToString()
        {
            using(PooledStringBuilder psb = PooledStringBuilder.Create())
            {
                int hour = Hour;
                int minute = Minute;

                int displayHour = Hour;
                if (displayHour > 12)
                    displayHour -= 12;

                psb.Builder.Append(displayHour).Append(':');
                if (minute < 10)
                    psb.Builder.Append('0');
                psb.Builder.Append(minute);

                if (hour < 12)
                    psb.Builder.Append(" AM");
                else
                    psb.Builder.Append(" PM");

                psb.Builder.Append(", Day ").Append(m_Day).Append(", ").Append(m_DayName);
                return psb.ToString();
            }
        }

        static public bool operator==(InGameTime left, InGameTime right)
        {
            return left.Equals(right);
        }

        static public bool operator!=(InGameTime left, InGameTime right)
        {
            return !left.Equals(right);
        }

        static public bool operator<(InGameTime left, InGameTime right)
        {
            return left.CompareTo(right) == -1;
        }

        static public bool operator<=(InGameTime left, InGameTime right)
        {
            return left.CompareTo(right) <= 0;
        }

        static public bool operator>(InGameTime left, InGameTime right)
        {
            return left.CompareTo(right) == 1;
        }

        static public bool operator>=(InGameTime left, InGameTime right)
        {
            return left.CompareTo(right) >= 0;
        }

        #endregion // Overrides
    }
}