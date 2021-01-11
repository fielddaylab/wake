using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Qualitative Mapping")]
    public class QualitativeMapping : ScriptableObject
    {
        #region Inspector

        [SerializeField] private bool m_AllowNone = true;
        [SerializeField] private float m_LowValue = 0;
        [SerializeField] private float m_MediumValue = 0;
        [SerializeField] private float m_HighValue = 0;

        #endregion // Inspector

        public bool AllowNone() { return m_AllowNone; }

        public float LowValue() { return m_LowValue; }
        public int LowValueI() { return (int) m_LowValue; }

        public float MediumValue() { return m_MediumValue; }
        public int MediumValueI() { return (int) m_MediumValue; }

        public float HighValue() { return m_HighValue; }
        public int HighValueI() { return (int) m_HighValue; }

        public float Value(QualitativeValue inValue)
        {
            switch(inValue)
            {
                case QualitativeValue.None:
                    if (!m_AllowNone)
                        throw new ArgumentException("None value is unsupported for this mapping", "inValue");
                    return 0;

                case QualitativeValue.Low:
                    return m_LowValue;

                case QualitativeValue.Medium:
                    return m_MediumValue;

                case QualitativeValue.High:
                    return m_HighValue;

                default:
                    throw new ArgumentOutOfRangeException("inValue");
            }
        }

        public int ValueI(QualitativeValue inValue)
        {
            return (int) Value(inValue);
        }

        public QualitativeValue Closest(float inValue)
        {
            QualitativeValue val = QualitativeValue.None;
            float minDist = m_AllowNone ? inValue : float.MaxValue;

            float dist;
            if ((dist = Math.Abs(inValue - m_LowValue)) < minDist)
            {
                minDist = dist;
                val = QualitativeValue.Low;
            }
            if ((dist = Math.Abs(inValue - m_MediumValue)) < minDist)
            {
                minDist = dist;
                val = QualitativeValue.Medium;
            }
            if ((dist = Math.Abs(inValue - m_HighValue)) < minDist)
            {
                minDist = dist;
                val = QualitativeValue.High;
            }
            return val;
        }

        public void CalculateRange(QualitativeValue inMin, QualitativeValue inMax, out float outMin, out float outMax)
        {
            if (inMin == inMax)
            {
                CalculateRange(inMin, out outMin, out outMax);
                return;
            }

            float minA, maxA, minB, maxB;
            CalculateRange(inMin, out minA, out maxA);
            CalculateRange(inMax, out minB, out maxB);

            outMin = Math.Min(minA, minB);
            outMax = Math.Max(maxA, maxB);
        }

        public void CalculateRange(QualitativeValue inValue, out float outMin, out float outMax)
        {
            switch(inValue)
            {
                case QualitativeValue.None:
                    {
                        if (!m_AllowNone)
                            throw new ArgumentException("None value is unsupported for this mapping", "inValue");
                        
                        outMin = float.NegativeInfinity;
                        outMax = m_LowValue / 2;
                        break;
                    }

                case QualitativeValue.Low:
                    {
                        outMin = m_AllowNone ? (m_LowValue / 2) : float.MinValue;
                        outMax = (m_MediumValue + m_LowValue) / 2;
                        break;
                    }

                case QualitativeValue.Medium:
                    {
                        outMin = (m_MediumValue + m_LowValue) / 2;
                        outMax = (m_HighValue + m_MediumValue) / 2;
                        break;
                    }

                case QualitativeValue.High:
                    {
                        outMin = (m_HighValue + m_MediumValue) / 2;
                        outMax = float.MaxValue;
                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException("inValue");
            }
        }
    }
}