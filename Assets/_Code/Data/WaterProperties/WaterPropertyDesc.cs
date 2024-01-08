using System;
using System.Collections.Generic;
using System.Text;
using BeauPools;
using BeauUtil;
using EasyAssetStreaming;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab System/Water Property Description", fileName = "NewWaterProp")]
    public class WaterPropertyDesc : DBObject, IEditorOnlyData
    {
        [Flags]
        public enum AllowedUnitConversions {
            Kilo = 0x01,
            Milli = 0x02
        }

        public const string RateUnit = "t";

        #region Inspector

        [SerializeField, AutoEnum] private WaterPropertyId m_Index = WaterPropertyId.Oxygen;
        [SerializeField, AutoEnum] private WaterPropertyFlags m_Flags = 0;
        
        [Header("Display")]
        [SerializeField] private TextId m_LabelId = default;
        [SerializeField] private TextId m_ShortLabelId = default;
        [SerializeField] private TextId m_GenderId = default;
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField, StreamingImagePath] private string m_HiResIconPath = null;
        [SerializeField] private ColorPalette4 m_Palette = new ColorPalette4(ColorBank.White, ColorBank.Gray);

        [Header("Text")]
        [SerializeField] private string m_Units = "";
        [SerializeField] private string m_RateUnits = null;
        [SerializeField] private int m_SignificantDigits = 1;

        [Header("Facts")]
        [SerializeField] private TextId m_EnvironmentFactFormat = default;
        [SerializeField] private TextId m_EnvironmentHistoryFormat = default;
        [SerializeField] private TextId m_StateChangeFormat = default;
        [SerializeField] private TextId m_StateChangeStressOnlyFormat = default;
        [SerializeField] private TextId m_StateChangeUnaffectedFormat = default;

        [Header("Ranges")]
        [SerializeField] private float m_MinValue = 0;
        [SerializeField] private float m_MaxValue = 0;
        [SerializeField] private float m_DefaultValue = 0;
        [SerializeField] private float m_ValueScale = 1;

        #endregion

        public WaterPropertyId Index() { return m_Index; }
        public WaterPropertyFlags Flags() { return m_Flags; }
        public bool HasFlags(WaterPropertyFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(WaterPropertyFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public TextId LabelId() { return m_LabelId; }
        public TextId ShortLabelId() { return m_ShortLabelId.IsEmpty ? m_LabelId : m_ShortLabelId; }
        public TextId GenderId() { return m_GenderId; }
        public Sprite Icon() { return m_Icon; }
        public StreamedImageSet ImageSet() { return new StreamedImageSet(m_HiResIconPath, m_Icon); }
        public Color Color() { return m_Palette.Background; }
        public ColorPalette4 Palette() { return m_Palette; }
        
        public TextId EnvironmentFactFormat() { return m_EnvironmentFactFormat; }
        public TextId EnvironmentHistoryFactFormat() { return m_EnvironmentHistoryFormat; }
        public TextId StateChangeFormat() { return m_StateChangeFormat; }
        public TextId StateChangeStressOnlyFormat() { return m_StateChangeStressOnlyFormat; }
        public TextId StateChangeUnaffectedFormat() { return m_StateChangeUnaffectedFormat; }
        
        public string FormatValue(float inValue, string prefix = null)
        {
            inValue *= m_ValueScale;
            AdjustScale(ref inValue, GetAllowedConversions(), out string unitPrefix, out string unitOverride);

            using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                if (!string.IsNullOrEmpty(prefix)) {
                    psb.Builder.Append(prefix);
                }
                FormatValue(psb.Builder, inValue, m_SignificantDigits, unitPrefix, unitOverride ?? m_Units);
                return psb.Builder.Flush();
            }
        }

        public string FormatRate(float inValue, string prefix = null, string additionalUnits = null)
        {
            inValue *= m_ValueScale;
            AdjustScale(ref inValue, GetAllowedConversions(), out string unitPrefix, out string unitOverride);

            using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                if (!string.IsNullOrEmpty(prefix)) {
                    psb.Builder.Append(prefix);
                }
                FormatValue(psb.Builder, inValue, m_SignificantDigits, unitPrefix, unitOverride ?? (!string.IsNullOrEmpty(m_RateUnits) ? m_RateUnits : m_Units));
                if (additionalUnits != null) {
                    psb.Builder.Append('/').Append(additionalUnits);
                }
                psb.Builder.Append('/').Append(RateUnit);
                return psb.Builder.Flush();
            }
        }

        private AllowedUnitConversions GetAllowedConversions() {
            AllowedUnitConversions units = 0;
            if (HasFlags(WaterPropertyFlags.AllowKilo)) {
                units |= AllowedUnitConversions.Kilo;
            }
            if (HasFlags(WaterPropertyFlags.AllowMilli)) {
                units |= AllowedUnitConversions.Milli;
            }
            return units;
        }

        static public void AdjustScale(ref float val, AllowedUnitConversions allowedConversions, out string unitPrefix, out string unitOverride) {
            float abs = Math.Abs(val);
            if ((allowedConversions & AllowedUnitConversions.Kilo) != 0 && abs >= 1100) {
                val /= 1000;
                unitPrefix = "K";
                unitOverride = null;
            } else if ((allowedConversions & AllowedUnitConversions.Milli) != 0 && abs < 0.01) {
                val *= 1000;
                unitPrefix = "m";
                unitOverride = null;
            } else {
                unitPrefix = null;
                unitOverride = null;
            }
        }

        static public void FormatValue(StringBuilder sb, float valueF, int significantDigits, string unitPrefix, string units) {
            double value = valueF;
            int sign = Math.Sign(value);
            value = Math.Abs(value);
            int exponent = 0;
            if (value > 10000) {
                while(value >= 10) {
                    value /= 10;
                    exponent++;
                }
            } else if (value > 0 && value < 0.1) {
                while(value < 0.95) {
                    value *= 10;
                    exponent--;
                }
            }

            value *= sign;

            sb.AppendNoAlloc(value, significantDigits, 0);
            if (exponent != 0) {
                sb.Append("e").AppendNoAlloc(exponent);
            }
            if (unitPrefix != null) {
                sb.Append(unitPrefix);
            }
            sb.Append(units);
        }

        public float MinValue() { return m_MinValue; }
        public float MaxValue() { return m_MaxValue; }
        public float DefaultValue() { return m_DefaultValue; }

        /// <summary>
        /// Remaps the given value to a 0-1 range from the minimum property value to the maximum.
        /// </summary>
        public float RemapValue(float inValue)
        {
            return (inValue - m_MinValue) / (m_MaxValue - m_MinValue);
        }

        /// <summary>
        /// Remaps the given fraction to a value between the min and max property values.
        /// </summary>
        public float InverseRemap(float inFraction)
        {
            return m_MinValue + (m_MaxValue - m_MinValue) * inFraction;
        }

        #if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorOnlyData() {
            ValidationUtils.StripDebugInfo(ref m_LabelId);
            ValidationUtils.StripDebugInfo(ref m_EnvironmentFactFormat);
            ValidationUtils.StripDebugInfo(ref m_EnvironmentHistoryFormat);
            ValidationUtils.StripDebugInfo(ref m_StateChangeFormat);
            ValidationUtils.StripDebugInfo(ref m_StateChangeStressOnlyFormat);
            ValidationUtils.StripDebugInfo(ref m_StateChangeUnaffectedFormat);
        }

        #endif // UNITY_EDITOR
    }

    [Flags]
    public enum WaterPropertyFlags : ushort
    {
        IsResource = 0x01,
        IsProperty = 0x02,
        AllowKilo = 0x04,
        AllowMilli = 0x08
    }
}