using System;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    static public class GraphingUtils
    {
        #region Axis

        /// <summary>
        /// Two-axis range.
        /// </summary>
        public struct AxisRangePair
        {
            public AxisRangeInfo X;
            public AxisRangeInfo Y;

            public Rect ToRect()
            {
                return new Rect(X.Min, Y.Min, X.Max - X.Min, Y.Max - Y.Min);
            }
        }

        /// <summary>
        /// Range information for a given axis.
        /// </summary>
        public struct AxisRangeInfo
        {
            public float Min;
            public float Max;
            public uint TickCount;
            public float TickInterval;

            public void SetMinAtOrigin()
            {
                Max -= Min;
                Min = 0;
            }
        }

        static private readonly double[] GoodNormalizedTicks = new double[] { 1, 1.5, 2, 2.5, 5, 7.5, 10 };
        static private readonly int GoodNormalizedTickCount = 7;

        /// <summary>
        /// Calculates good range and ticks for displaying values within a given range.
        /// </summary>
        static public AxisRangeInfo CalculateAxis(float inMin, float inMax, uint inTargetTickCount)
        {
            // algorithm adapted from: https://stackoverflow.com/a/49911176

            if (inTargetTickCount < 2)
                throw new ArgumentOutOfRangeException("inTargetTickCount", "Target ticks should be at least 2");

            AxisRangeInfo info;
            double min = inMin, max = inMax;
            double epsilon = (max - min) / 1e6;
            max += epsilon;
            min -= epsilon;
            double range = max - min;

            double roughStep = range / (inTargetTickCount - 1);
            double absRoughStep = Math.Abs(roughStep);

            double stepPower = Math.Pow(10, -Math.Floor(Math.Log10(absRoughStep)));
            double normalizedStep = roughStep * stepPower;
            double goodNormalizedStep = normalizedStep;
            for(int i = 0; i < GoodNormalizedTickCount; ++i)
            {
                if (GoodNormalizedTicks[i] >= normalizedStep)
                {
                    goodNormalizedStep = GoodNormalizedTicks[i];
                    break;
                }
            }

            double step = goodNormalizedStep / stepPower;

            double rangeMin = Math.Floor(min / step) * step;
            double rangeMax = Math.Ceiling(max / step) * step;
            uint tickCount = 1 + (uint) ((rangeMax - rangeMin) / step);

            info.Min = (float) rangeMin;
            info.Max = (float) rangeMax;
            info.TickCount = tickCount;
            info.TickInterval = (float) step;

            return info;
        }

        /// <summary>
        /// Calculates the axis information for the given rectangle.
        /// </summary>
        static public AxisRangePair CalculateAxisPair(Rect inRange, uint inTargetTickCountX, uint inTargetTickCountY)
        {
            var rangeX = GraphingUtils.CalculateAxis(inRange.xMin, inRange.xMax, inTargetTickCountX);
            var rangeY = GraphingUtils.CalculateAxis(inRange.yMin, inRange.yMax, inTargetTickCountY);

            return new AxisRangePair()
            {
                X = rangeX,
                Y = rangeY
            };
        }

        #endregion // Axis

        #region Error

        /// <summary>
        /// Calculates the relative percent deviation between two values.
        /// </summary>
        static public float RPD(float inA, float inB)
        {
            float delta = Math.Abs(inA - inB);
            float avg = (Math.Abs(inA) + Math.Abs(inB)) / 2;
            return avg == 0 ? 0 : delta / avg;
        }

        #endregion // Error
    }
}