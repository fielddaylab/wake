using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Water Property/Water Property DB", fileName = "WaterPropertyDB")]
    public class WaterPropertyDB : DBObjectCollection<WaterPropertyDesc>
    {
        [NonSerialized] private WaterPropertyDesc[] m_PropertyIdMap;
        [NonSerialized] private WaterPropertyDesc[] m_SortedMap;

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
                Debug.LogErrorFormat("[WaterPropertyDB] Multiple properties mapped to id '{0}'", propIndex);
            }
            else
            {
                m_PropertyIdMap[(int) propIndex] = inItem;
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

        public IReadOnlyList<WaterPropertyDesc> Sorted()
        {
            return m_SortedMap;
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