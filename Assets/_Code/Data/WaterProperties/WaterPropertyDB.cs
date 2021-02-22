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

        protected override void PreLookupConstruct()
        {
            base.PreLookupConstruct();
            m_PropertyIdMap = new WaterPropertyDesc[(int) WaterPropertyId.MAX];
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
        }

        public WaterPropertyDesc Property(WaterPropertyId inId)
        {
            EnsureCreated();
            
            if (inId < 0 || inId >= WaterPropertyId.MAX)
                return null;

            return m_PropertyIdMap[(int) inId];
        }

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(WaterPropertyDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}