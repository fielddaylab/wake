using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab System/Bestiary Database", fileName = "BestiaryDB")]
    public class BestiaryDB : DBObjectCollection<BestiaryDesc>, IOptimizableAsset
    {
        #region Inspector

        [Header("Defaults")]
        [SerializeField] private Sprite m_DefaultEatSprite = null;
        [SerializeField] private Sprite m_DefaultHumanCatchSprite = null;
        [SerializeField] private Sprite m_DefaultIsEatenSprite = null;
        [SerializeField] private Sprite m_DefaultProduceSprite = null;
        [SerializeField] private Sprite m_DefaultConsumeSprite = null;
        [SerializeField] private Sprite m_DefaultReproduceSprite = null;
        [SerializeField] private Sprite m_DefaultGrowSprite = null;
        [SerializeField] private Sprite m_DefaultDeathSprite = null;

        [Header("Graphs")]
        [SerializeField] private Sprite[] m_Graphs = new Sprite[(int) BFGraphType.MAX];

        // HIDDEN

        [SerializeField, HideInInspector] private BFBase[] m_AllFacts = null;
        [SerializeField, HideInInspector] private ushort m_CritterCount;
        [SerializeField, HideInInspector] private ushort m_EnvironmentCount;

        #endregion // Inspector

        [NonSerialized] private Dictionary<StringHash32, BFBase> m_FactMap;
        [NonSerialized] private HashSet<StringHash32> m_AutoFacts;

        #region Defaults

        public Sprite DefaultIcon(BFBase inFact)
        {
            switch(inFact.Type)
            {
                case BFTypeId.Eat:
                    if (m_DefaultHumanCatchSprite != null && inFact.Parent.HasFlags(BestiaryDescFlags.Human))
                        return m_DefaultHumanCatchSprite;
                    return m_DefaultEatSprite;
                case BFTypeId.Produce:
                    return m_DefaultProduceSprite;
                case BFTypeId.Consume:
                    return m_DefaultConsumeSprite;
                case BFTypeId.Reproduce:
                    return m_DefaultReproduceSprite;
                case BFTypeId.Grow:
                    return m_DefaultGrowSprite;
                case BFTypeId.Death:
                    return m_DefaultDeathSprite;
                default:
                    return null;
            }
        }

        public Sprite DefaultIsEatenIcon() {
            return m_DefaultIsEatenSprite;
        }

        public Sprite GraphTypeToImage(BFGraphType inGraphType)
        {
            return m_Graphs[(int) inGraphType];
        }

        #endregion // Defaults

        #region Lookup

        public ListSlice<BestiaryDesc> Critters
        {
            get { return new ListSlice<BestiaryDesc>(m_Objects, m_EnvironmentCount, m_CritterCount); }
        }

        public ListSlice<BestiaryDesc> Environments
        {
            get { return new ListSlice<BestiaryDesc>(m_Objects, 0, m_EnvironmentCount); }
        }

        public ListSlice<BestiaryDesc> AllEntriesForCategory(BestiaryDescCategory inCategory)
        {
            switch(inCategory)
            {
                case BestiaryDescCategory.Critter:
                    return new ListSlice<BestiaryDesc>(m_Objects, m_EnvironmentCount, m_CritterCount);
                case BestiaryDescCategory.Environment:
                    return new ListSlice<BestiaryDesc>(m_Objects, 0, m_EnvironmentCount);

                case BestiaryDescCategory.ALL:
                    return m_Objects;
                
                default:
                    throw new ArgumentOutOfRangeException("inCategory");
            }
        }

        public BFBase Fact(StringHash32 inFactId)
        {
            EnsureCreated();

            BFBase fact;
            m_FactMap.TryGetValue(inFactId, out fact);
            Assert.NotNull(fact, "Could not find BFBase with id '{0}'", inFactId);
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

        public bool HasFactWithId(StringHash32 inFactId)
        {
            EnsureCreated();
            return m_FactMap.ContainsKey(inFactId);
        }

        public ListSlice<BFBase> AllFacts()
        {
            EnsureCreated();
            return m_AllFacts;
        }

        #endregion // Facts

        #region Internal

        protected override void PreLookupConstruct()
        {
            base.PreLookupConstruct();
            m_FactMap = new Dictionary<StringHash32, BFBase>(Count());
            m_AutoFacts = new HashSet<StringHash32>();

            foreach(var fact in m_AllFacts)
            {
                StringHash32 factId = fact.Id;
                Assert.False(m_FactMap.ContainsKey(factId), "Duplicate fact id '{0}'", factId);
                m_FactMap.Add(factId, fact);
                if (fact.Mode != BFMode.Player)
                {
                    m_AutoFacts.Add(factId);
                }
            }
        }

        #endregion // Internal

        #if UNITY_EDITOR

        int IOptimizableAsset.Order { get { return 10; } }

        bool IOptimizableAsset.Optimize()
        {
            SortObjects(SortByCategory);

            List<BFBase> allFacts = new List<BFBase>(512);
            Dictionary<BestiaryDesc, List<BFBase>> critterReciprocalFactLists = new Dictionary<BestiaryDesc, List<BFBase>>();

            m_CritterCount = 0;
            m_EnvironmentCount = 0;

            BestiaryDesc entry;
            StringHash32 envId;
            BFEat eat;
            for(int i = 0; i < m_Objects.Length; i++)
            {
                entry = m_Objects[i];

                foreach(var fact in entry.OwnedFacts)
                {
                    allFacts.Add(fact);
                }

                switch(entry.Category())
                {
                    case BestiaryDescCategory.Critter:
                        {
                            m_CritterCount++;
                            envId = entry.StationId();

                            foreach(var fact in entry.OwnedFacts)
                            {
                                eat = fact as BFEat;
                                if (eat != null)
                                {
                                    AddToListMap(critterReciprocalFactLists, eat.Critter, eat);
                                }
                            }
                            break;
                        }

                    case BestiaryDescCategory.Environment:
                        {
                            m_EnvironmentCount++;
                            break;
                        }
                }
            }
            foreach(var obj in m_Objects)
            {
                obj.OptimizeSecondPass(GetList(critterReciprocalFactLists, obj));
            }

            m_AllFacts = allFacts.ToArray();

            return true;
        }

        static private readonly int[] CategorySortOrder = new int[] { 1, 0, 2 };

        static private readonly Comparison<BestiaryDesc> SortByCategory = (a, b) => 
        {
            int compare = CategorySortOrder[(int) a.Category()].CompareTo(CategorySortOrder[(int) b.Category()]);
            if (compare == 0)
                compare = a.name.CompareTo(b.name);
            return compare;
        };

        static private void AddToListMap<T, U>(Dictionary<T, List<U>> ioMap, T inKey, U inValue)
        {
            if (inValue == null)
                return;

            List<U> list;
            if (!ioMap.TryGetValue(inKey, out list))
            {
                list = ioMap[inKey] = new List<U>() { inValue };
            }
            else
            {
                list.Add(inValue);
            }
        }

        static private List<U> GetList<T, U>(Dictionary<T, List<U>> ioMap, T inKey)
        {
            List<U> list;
            ioMap.TryGetValue(inKey, out list);
            return list;
        }

        [UnityEditor.CustomEditor(typeof(BestiaryDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}