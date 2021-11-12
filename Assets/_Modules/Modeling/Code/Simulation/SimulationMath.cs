using System;

namespace Aqua.Modeling {
    
    /// <summary>
    /// Simulation math.
    /// </summary>
    static public class SimulationMath {
        private const int FixedShift = 12;
        private const ulong FixedOne = 1 << FixedShift;

        /// <summary>
        /// Converts the given value to fixed-point notation
        /// </summary>
        static public long ToFixed(float inValue) {
            return (long)Math.Round(inValue * FixedOne);
        }

        /// <summary>
        /// Converts the given value to fixed-point notation
        /// </summary>
        static public long ToFixed(uint inValue) {
            return (long)inValue << FixedShift;
        }

        /// <summary>
        /// Converts the given fixed-point value to an unsigned integer
        /// </summary>
        static public uint ToUInt(long inFixed) {
            return (uint)(inFixed >> FixedShift);
        }

        /// <summary>
        /// Converts the given fixed-point value to a float
        /// </summary>
        static public float ToFloat(long inFixed) {
            return (float) inFixed / FixedShift;
        }

        /// <summary>
        /// Fixed-point multiplication.
        /// </summary>
        static public uint FixedMultiply(uint inValue, float inMultiply) {
            long fixedA = ToFixed(inValue);
            long fixedB = ToFixed(inMultiply);
            long fixedC = (fixedA * fixedB) >> FixedShift;
            return ToUInt(fixedC);
        }

        /// <summary>
        /// Fixed-point division.
        /// </summary>
        static public uint FixedDivide(uint inValue, float inMultiply) {
            long fixedA = ToFixed(inValue);
            long fixedB = ToFixed(inMultiply);
            long fixedC = (fixedA << FixedShift) / fixedB;
            return ToUInt(fixedC);
        }
    }
}