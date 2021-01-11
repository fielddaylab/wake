using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Bestiary Database", fileName = "BestiaryDB")]
    public class BestiaryDB : DBObjectCollection<BestiaryDesc>
    {
        [NonSerialized] private Dictionary<StringHash32, BFBase> m_FactMap;

        [NonSerialized] private List<BestiaryDesc> m_Critters;
        [NonSerialized] private List<BestiaryDesc> m_Ecosystems;

        [NonSerialized] private HashSet<StringHash32> m_AutoFacts;

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

        public BFBase Fact(StringHash32 inFactId)
        {
            EnsureCreated();

            BFBase fact;
            m_FactMap.TryGetValue(inFactId, out fact);
            return fact;
        }

        public TFact Fact<TFact>(StringHash32 inFactId) where TFact : BFBase
        {
            return (TFact) Fact(inFactId);
        }

        public bool IsAutoFact(StringHash32 inFactId)
        {
            EnsureCreated();

            return m_AutoFacts.Contains(inFactId);
        }

        #endregion // Facts

        #region Internal

        protected override void PreLookupConstruct()
        {
            base.PreLookupConstruct();

            int listSize = Mathf.Max(4, Count() / 2 + 1);
            m_Ecosystems = new List<BestiaryDesc>(listSize);
            m_Critters = new List<BestiaryDesc>(listSize);

            m_FactMap = new Dictionary<StringHash32, BFBase>(Count());
            m_AutoFacts = new HashSet<StringHash32>();
        }

        protected override void ConstructLookupForItem(BestiaryDesc inItem, int inIndex)
        {
            base.ConstructLookupForItem(inItem, inIndex);

            inItem.Initialize();

            foreach(var fact in inItem.Facts)
            {
                m_FactMap.Add(fact.Id(), fact);
                if (fact.Mode() != BFMode.Player)
                {
                    m_AutoFacts.Add(fact.Id());
                }
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