using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Water Property/Water Property Desc", fileName = "NewWaterProp")]
    public class WaterPropertyDesc : DBObject
    {
        #region Inspector

        [SerializeField, AutoEnum] private WaterPropertyId m_Index = WaterPropertyId.Oxygen;
        [SerializeField, AutoEnum] private WaterPropertyFlags m_Flags = 0;
        
        [Header("Display")]
        [SerializeField] private SerializedHash32 m_LabelId = null;
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField] private Color m_Color = ColorBank.White;
        [SerializeField] private string m_Format = "{0}";
        [SerializeField] private float m_DisplayScale = 1;

        [Header("Ranges")]
        [SerializeField] private float m_MinValue = 0;
        [SerializeField] private float m_MaxValue = 0;

        #endregion

        public WaterPropertyId Index() { return m_Index; }
        public WaterPropertyFlags Flags() { return m_Flags; }
        public bool HasFlags(WaterPropertyFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(WaterPropertyFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public StringHash32 LabelId() { return m_LabelId; }
        public Sprite Icon() { return m_Icon; }
        public Color Color() { return m_Color; }
        
        public string FormatValue(float inValue)
        {
            return string.Format(m_Format, inValue * m_DisplayScale);
        }

        public float DisplayValue(float inValue)
        {
            // TODO: Move all this to some kind of UnitFormatter?
            return inValue * m_DisplayScale;
        }

        public float MinValue() { return m_MinValue; }
        public float MaxValue() { return m_MaxValue; }

        /// <summary>
        /// Remaps the given value to a 0-1 range from the minimum property value to the maximum.
        /// </summary>
        public float RemapValue(float inValue)
        {
            return (inValue - m_MinValue) / (m_MaxValue - m_MinValue);
        }
    }

    [Flags]
    public enum WaterPropertyFlags : ushort
    {
        HideAlways = 0x001,
        HideIfZero = 0x002,
        TransferThroughEating = 0x004
    }
}