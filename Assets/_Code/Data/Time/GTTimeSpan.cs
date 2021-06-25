using System;
using BeauData;
using BeauPools;

namespace Aqua
{
    public struct GTTimeSpan : IEquatable<GTTimeSpan>, IComparable<GTTimeSpan>, ISerializedObject
    {
        private long m_Ticks;

        #region Constructors

        public GTTimeSpan(long inTicks)
        {
            m_Ticks = inTicks;
        }

        #endregion // Constructors

        /// <summary>
        /// Zero timespan.
        /// </summary>
        static public readonly GTTimeSpan Zero = new GTTimeSpan(0);

        #region Accessors

        /// <summary>
        /// Number of ticks.
        /// </summary>
        public long Ticks { get { return m_Ticks; } }

        /// <summary>
        /// How many whole days.
        /// </summary>
        public long Days { get { return m_Ticks / GTDate.TicksPerDay; } }

        /// <summary>
        /// How many whole hours.
        /// </summary>
        public long Hours { get { return m_Ticks / GTDate.TicksPerHour; } }

        /// <summary>
        /// How many days (fractional)
        /// </summary>
        public float TotalDays { get { return m_Ticks / (float) GTDate.TicksPerDay; } }

        /// <summary>
        /// How many hours (fractional)
        /// </summary>
        public float TotalHours { get { return m_Ticks / (float) GTDate.TicksPerHour; } }

        #endregion // Accessors

        #region Interfaces

        public int CompareTo(GTTimeSpan other)
        {
            return m_Ticks.CompareTo(other.m_Ticks);
        }

        public bool Equals(GTTimeSpan other)
        {
            return m_Ticks == other.m_Ticks;
        }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Serialize("ticks", ref m_Ticks);
        }

        #endregion // Interfaces

        #region Overrides

        public override bool Equals(object obj)
        {
            if (obj is GTTimeSpan)
            {
                return Equals((GTTimeSpan) obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return m_Ticks.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0:0.00} hours", TotalHours);
        }

        static public bool operator==(GTTimeSpan left, GTTimeSpan right)
        {
            return left.Equals(right);
        }

        static public bool operator!=(GTTimeSpan left, GTTimeSpan right)
        {
            return !left.Equals(right);
        }

        static public bool operator<(GTTimeSpan left, GTTimeSpan right)
        {
            return left.CompareTo(right) == -1;
        }

        static public bool operator<=(GTTimeSpan left, GTTimeSpan right)
        {
            return left.CompareTo(right) <= 0;
        }

        static public bool operator>(GTTimeSpan left, GTTimeSpan right)
        {
            return left.CompareTo(right) == 1;
        }

        static public bool operator>=(GTTimeSpan left, GTTimeSpan right)
        {
            return left.CompareTo(right) >= 0;
        }

        static public GTTimeSpan operator+(GTTimeSpan left, GTTimeSpan right)
        {
            return new GTTimeSpan(left.m_Ticks + right.m_Ticks);
        }

        static public GTTimeSpan operator-(GTTimeSpan left, GTTimeSpan right)
        {
            return new GTTimeSpan(left.m_Ticks - right.m_Ticks);
        }

        #endregion // Overrides
    }
}