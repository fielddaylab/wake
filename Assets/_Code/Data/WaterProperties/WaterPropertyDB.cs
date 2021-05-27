using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Water Property/Water Property DB", fileName = "WaterPropertyDB")]
    public class WaterPropertyDB : DBObjectCollection<WaterPropertyDesc>
    {
        #region Inspector

        [SerializeField] private WaterPropertyId[] m_DefaultUnlockedProperties = null;

        #endregion // Inspector

        [NonSerialized] private WaterPropertyDesc[] m_PropertyIdMap;
        [NonSerialized] private WaterPropertyDesc[] m_SortedMap;
        [NonSerialized] private WaterPropertyBlockF32 m_DefaultValues;

        protected override void PreLookupConstruct()
        {
            base.PreLookupConstruct();
            m_PropertyIdMap = new WaterPropertyDesc[(int) WaterPropertyId.MAX];
            m_SortedMap = new WaterPropertyDesc[SortOrder.Length];
        }

        protected override void ConstructLookupForItem(WaterPropertyDesc inItem, int inIndex)
        {
            base.ConstructLookupForItem(inItem, inIndex);

            WaterPropertyId propIndex = inItem.Index();
            if (m_PropertyIdMap[(int) propIndex] != null)
            {
                Log.Error("[WaterPropertyDB] Multiple properties mapped to id '{0}'", propIndex);
            }
            else
            {
                m_PropertyIdMap[(int) propIndex] = inItem;
            }

            if (propIndex <= WaterPropertyId.TRACKED_MAX)
            {
                m_DefaultValues[propIndex] = inItem.DefaultValue();
            }

            int sortIndex = Array.IndexOf(SortOrder, propIndex);
            if (sortIndex >= 0)
                m_SortedMap[sortIndex] = inItem;
        }

        public WaterPropertyDesc Property(WaterPropertyId inId)
        {
            EnsureCreated();

            if (inId < 0 || inId >= WaterPropertyId.MAX)
                return null;

            return m_PropertyIdMap[(int) inId];
        }

        public WaterPropertyBlockF32 DefaultValues() { return m_DefaultValues; }
        public ListSlice<WaterPropertyId> DefaultUnlocked() { return m_DefaultUnlockedProperties; }

        public ListSlice<WaterPropertyDesc> Sorted()
        {
            return m_SortedMap;
        }

        public IEnumerable<WaterPropertyDesc> Measurable()
        {
            foreach(var prop in m_SortedMap)
            {
                if (prop.HasFlags(WaterPropertyFlags.IsMeasureable))
                    yield return prop;
            }
        }

        static private readonly WaterPropertyId[] SortOrder = new WaterPropertyId[]
        {
            WaterPropertyId.Temperature,
            WaterPropertyId.Light,
            WaterPropertyId.Oxygen,
            WaterPropertyId.Salinity,
            WaterPropertyId.CarbonDioxide,
            WaterPropertyId.PH
        };

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(WaterPropertyDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}