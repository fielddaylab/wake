using System;
using System.Collections.Generic;
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
        #region Inspector

        [SerializeField, AutoEnum] private WaterPropertyId m_Index = WaterPropertyId.Oxygen;
        [SerializeField, AutoEnum] private WaterPropertyFlags m_Flags = 0;
        
        [Header("Display")]
        [SerializeField] private TextId m_LabelId = default;
        [SerializeField] private TextId m_ShortLabelId = default;
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField, StreamingImagePath] private string m_HiResIconPath = null;
        [SerializeField] private ColorPalette4 m_Palette = new ColorPalette4(ColorBank.White, ColorBank.Gray);

        [Header("Text")]
        [SerializeField] private string m_Units = "";
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

        #endregion

        public WaterPropertyId Index() { return m_Index; }
        public WaterPropertyFlags Flags() { return m_Flags; }
        public bool HasFlags(WaterPropertyFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(WaterPropertyFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public TextId LabelId() { return m_LabelId; }
        public TextId ShortLabelId() { return m_ShortLabelId.IsEmpty ? m_LabelId : m_ShortLabelId; }
        public Sprite Icon() { return m_Icon; }
        public StreamedImageSet ImageSet() { return new StreamedImageSet(m_HiResIconPath, m_Icon); }
        public Color Color() { return m_Palette.Background; }
        public ColorPalette4 Palette() { return m_Palette; }
        
        public TextId EnvironmentFactFormat() { return m_EnvironmentFactFormat; }
        public TextId EnvironmentHistoryFactFormat() { return m_EnvironmentHistoryFormat; }
        public TextId StateChangeFormat() { return m_StateChangeFormat; }
        public TextId StateChangeStressOnlyFormat() { return m_StateChangeStressOnlyFormat; }
        public TextId StateChangeUnaffectedFormat() { return m_StateChangeUnaffectedFormat; }
        
        public string FormatValue(float inValue)
        {
            double value = inValue;
            int sign = Math.Sign(inValue);
            value = Math.Abs(value);
            int exponent = 0;
            if (value > 1000) {
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

            using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                psb.Builder.AppendNoAlloc(value, m_SignificantDigits, 0);
                if (exponent != 0) {
                    psb.Builder.Append("e").AppendNoAlloc(exponent);
                }
                psb.Builder.Append(m_Units);
                return psb.Builder.Flush();
            }
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
        IsProperty = 0x02
    }
}