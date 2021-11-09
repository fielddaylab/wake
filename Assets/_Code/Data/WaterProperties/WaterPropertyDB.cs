using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab System/Water Property Database", fileName = "WaterPropertyDB")]
    public class WaterPropertyDB : DBObjectCollection<WaterPropertyDesc>, IOptimizableAsset
    {
        #region Inspector

        [SerializeField] private WaterPropertyId[] m_DefaultUnlockedProperties = null;
        [SerializeField, HideInInspector] private WaterPropertyBlockF32 m_DefaultValues;
        [SerializeField, HideInInspector] private WaterPropertyDesc[] m_DisplaySortedMap;
        [SerializeField, HideInInspector] private WaterPropertyDesc[] m_IndexMap;

        #endregion // Inspector

        public WaterPropertyDesc Property(WaterPropertyId inId)
        {
            EnsureCreated();

            if (inId < 0 || inId >= WaterPropertyId.MAX)
                return null;

            return m_IndexMap[(int) inId];
        }

        public WaterPropertyBlockF32 DefaultValues() { return m_DefaultValues; }
        public ListSlice<WaterPropertyId> DefaultUnlocked() { return m_DefaultUnlockedProperties; }

        public ListSlice<WaterPropertyDesc> Sorted()
        {
            return m_DisplaySortedMap;
        }

        static private readonly WaterPropertyId[] SortOrder = new WaterPropertyId[]
        {
            WaterPropertyId.Temperature,
            WaterPropertyId.Light,
            WaterPropertyId.PH,
            WaterPropertyId.Oxygen,
            WaterPropertyId.CarbonDioxide
        };

        static private int SortingOrder(WaterPropertyId inId)
        {
            return Array.IndexOf(SortOrder, inId);
        }

        static public Comparison<WaterPropertyId> SortByVisualOrder = (x, y) =>
        {
            return SortingOrder(x).CompareTo(SortingOrder(y));
        };

        #region IOptimizedAsset
        
        #if UNITY_EDITOR

        int IOptimizableAsset.Order { get { return -10; } }

        bool IOptimizableAsset.Optimize()
        {
            SortObjects((a, b) => a.Index().CompareTo(b.Index()));
            m_DisplaySortedMap = new WaterPropertyDesc[SortOrder.Length];
            m_IndexMap = new WaterPropertyDesc[(int) WaterPropertyId.MAX];

            foreach(var property in m_Objects)
            {
                WaterPropertyId propIndex = property.Index();
                if (propIndex <= WaterProperties.TrackedMax)
                    m_DefaultValues[propIndex] = property.DefaultValue();

                m_IndexMap[(int) propIndex] = property;

                int sortIndex = Array.IndexOf(SortOrder, propIndex);
                if (sortIndex >= 0)
                    m_DisplaySortedMap[sortIndex] = property;
            }
            return true;
        }

        #endif // UNITY_EDITOR

        #endregion // IOptimizedAsset

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(WaterPropertyDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}