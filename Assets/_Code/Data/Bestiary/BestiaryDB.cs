using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Bestiary Database", fileName = "BestiaryDB")]
    public class BestiaryDB : DBObjectCollection<BestiaryDesc>
    {
        private Dictionary<StringHash32, BestiaryFactBase> m_FactMap;

        private List<BestiaryDesc> m_Critters;
        private List<BestiaryDesc> m_Ecosystems;

        #region Lookup

        public IReadOnlyList<BestiaryDesc> AllEntriesForCategory(BestiaryDescCategory inCategory)
        {
            switch(inCategory)
            {
                case BestiaryDescCategory.Environment:
                    return m_Ecosystems;
                case BestiaryDescCategory.Critter:
                    return m_Critters;
                
                default:
                    throw new ArgumentOutOfRangeException("inCategory");
            }
        }

        public BestiaryFactBase Fact(StringHash32 inFactId)
        {
            EnsureCreated();

            BestiaryFactBase fact;
            m_FactMap.TryGetValue(inFactId, out fact);
            return fact;
        }

        public TFact Fact<TFact>(StringHash32 inFactId) where TFact : BestiaryFactBase
        {
            return (TFact) Fact(inFactId);
        }

        public IEnumerable<BestiaryFactBase> Facts() 
        {
            return m_FactMap.Values;
        }

        #endregion // Facts

        #region Internal

        protected override void PreLookupConstruct()
        {
            base.PreLookupConstruct();

            int listSize = Mathf.Max(4, Count() / 2 + 1);
            m_Ecosystems = new List<BestiaryDesc>(listSize);
            m_Critters = new List<BestiaryDesc>(listSize);

            m_FactMap = new Dictionary<StringHash32, BestiaryFactBase>(Count());
        }

        protected override void ConstructLookupForItem(BestiaryDesc inItem, int inIndex)
        {
            base.ConstructLookupForItem(inItem, inIndex);

            inItem.Initialize();

            foreach(var fact in inItem.Facts)
            {
                m_FactMap.Add(fact.Id(), fact);
            }

            switch(inItem.Category())
            {
                case BestiaryDescCategory.Critter:
                    m_Critters.Add(inItem);
                    break;

                case BestiaryDescCategory.Environment:
                    m_Ecosystems.Add(inItem);
                    break;
            }
        }

        #endregion // Internal

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(BestiaryDB))]
        private class Inspector : BaseInspector
        {}  

        #endif // UNITY_EDITOR
    }
}