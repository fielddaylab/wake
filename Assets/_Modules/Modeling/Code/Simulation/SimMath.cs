using System;
using BeauUtil;
using UnityEngine;

namespace Aqua.Modeling {
    
    /// <summary>
    /// Simulation math.
    /// </summary>
    static public class SimMath {
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

        #region Graph

        /// <summary>
        /// Remaps the given data for the given bounds.
        /// </summary>
        static public void Scale(Vector2[] data, int count, Rect bounds) {
            float minX = bounds.xMin, minY = bounds.yMin, invWidth = 1f / bounds.width, invHeight = 1f / bounds.height;
            for(int i = 0; i < count; i++) {
                ref Vector2 vec = ref data[i];
                vec.x = (vec.x - minX) * invWidth;
                vec.y = (vec.y - minY) * invHeight;
            }
        }

        /// <summary>
        /// Remaps the given data for the given bounds.
        /// </summary>
        static public unsafe void Scale(Vector2* data, int count, Rect bounds) {
            float minX = bounds.xMin, minY = bounds.yMin, invWidth = 1f / bounds.width, invHeight = 1f / bounds.height;
            for(int i = 0; i < count; i++) {
                ref Vector2 vec = ref data[i];
                vec.x = (vec.x - minX) * invWidth;
                vec.y = (vec.y - minY) * invHeight;
            }
        }

        /// <summary>
        /// Remaps the given data for the inverse of the given bounds.
        /// </summary>
        static public void InvScale(Vector2[] data, int count, Rect bounds) {
            float minX = bounds.xMin, minY = bounds.yMin, width = bounds.width, height = bounds.height;
            for(int i = 0; i < count; i++) {
                ref Vector2 vec = ref data[i];
                vec.x = minX + (vec.x * width);
                vec.y = minY + (vec.y * height);
            }
        }

        /// <summary>
        /// Remaps the given data for the inverse of the given bounds.
        /// </summary>
        static public unsafe void InvScale(Vector2* data, int count, Rect bounds) {
            float minX = bounds.xMin, minY = bounds.yMin, width = bounds.width, height = bounds.height;
            for(int i = 0; i < count; i++) {
                ref Vector2 vec = ref data[i];
                vec.x = minX + (vec.x * width);
                vec.y = minY + (vec.y * height);
            }
        }

        /// <summary>
        /// Calculates the bounds for a given set of data.
        /// </summary>
        static public Rect CalculateBounds(Vector2[] data, int count) {
            float xMin = 0, xMax = 0, yMin = 0, yMax = 0;
            for(int i = 0; i < count; i++) {
                Vector2 vec = data[i];
                xMin = Math.Min(vec.x, xMin);
                yMin = Math.Min(vec.y, yMin);
                xMax = Math.Max(vec.x, xMax);
                yMax = Math.Max(vec.y, yMax);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// Calculates the bounds for a given set of data.
        /// </summary>
        static public unsafe Rect CalculateBounds(Vector2* data, int count) {
            float xMin = 0, xMax = 0.001f, yMin = 0, yMax = 0.001f;
            for(int i = 0; i < count; i++) {
                Vector2 vec = data[i];
                xMin = Math.Min(vec.x, xMin);
                yMin = Math.Min(vec.y, yMin);
                xMax = Math.Max(vec.x, xMax);
                yMax = Math.Max(vec.y, yMax);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// Calculates the bounds for a given set of data.
        /// </summary>
        static public unsafe Rect CalculatePositionBounds(Vector2* data, int count) {
            if (count == 0) {
                return Rect.MinMaxRect(0, 0, 0.001f, 0.001f);
            }
            Vector2 vec = data[0];
            float xMin = vec.x, xMax = vec.x, yMin = vec.y, yMax = vec.y;
            for(int i = 1; i < count; i++) {
                vec = data[i];
                xMin = Math.Min(vec.x, xMin);
                yMin = Math.Min(vec.y, yMin);
                xMax = Math.Max(vec.x, xMax);
                yMax = Math.Max(vec.y, yMax);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// Calculates the bounds for a given set of data.
        /// </summary>
        static public void CalculateBounds(ref Rect rect, Vector2[] data, int count) {
            float xMin = rect.xMin, xMax = rect.xMax, yMin = rect.yMin, yMax = rect.yMax;
            for(int i = 0; i < count; i++) {
                Vector2 vec = data[i];
                xMin = Math.Min(vec.x, xMin);
                yMin = Math.Min(vec.y, yMin);
                xMax = Math.Max(vec.x, xMax);
                yMax = Math.Max(vec.y, yMax);
            }
            rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// Calculates the bounds for a given set of data.
        /// </summary>
        static public unsafe void CalculateBounds(ref Rect rect, Vector2* data, int count) {
            float xMin = rect.xMin, xMax = rect.xMax, yMin = rect.yMin, yMax = rect.yMax;
            for(int i = 0; i < count; i++) {
                Vector2 vec = data[i];
                xMin = Math.Min(vec.x, xMin);
                yMin = Math.Min(vec.y, yMin);
                xMax = Math.Max(vec.x, xMax);
                yMax = Math.Max(vec.y, yMax);
            }
            rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        /// <summary>
        /// Expands the given boundaries.
        /// </summary>
        static public void ScaleBounds(ref Rect rect, float borders, float multiplier) {
            Vector2 center = rect.center;
            Vector2 size = rect.size;
            center.x *= multiplier;
            center.y *= multiplier;
            size.x = (size.x + borders * 2f) * multiplier;
            size.y = (size.y + borders * 2f) * multiplier;
            rect = new Rect(center - size / 2, size);
        }

        /// <summary>
        /// Combines two rectangle bounds.
        /// </summary>
        static public void CombineBounds(ref Rect rect, Rect combineWith) {
            Geom.Encapsulate(ref rect, combineWith);
        }

        /// <summary>
        /// Finalizes boundaries.
        /// </summary>
        static public void FinalizeBounds(ref Rect rect) {
            GraphingUtils.AxisRangePair pair;
            pair.X = new GraphingUtils.AxisRangeInfo() { Min = 0, Max = (int) rect.width, TickCount = (uint) rect.width + 1, TickInterval = 1 };
            pair.Y = GraphingUtils.CalculateAxis(rect.yMin, rect.yMax, 8);
            pair.Y.SetMinAtOrigin();
            rect = pair.ToRect();
        }

        /// <summary>
        /// Returns if two sets of points diverge.
        /// </summary>
        static public bool HasDivergence(Vector2[] a, Vector2[] b, int count) {
            for(int i = 0; i < count; i++) {
                if (!Mathf.Approximately(a[i].y, b[i].y)) {
                    return true;
                }
            }
            return false;
        }

        #endregion // Graph
    }
}