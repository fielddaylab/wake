using System;
using System.Collections.Generic;
using System.IO;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Bestiary Entry", fileName = "NewBestiaryEntry")]
    public class BestiaryDesc : DBObject, IOptimizableAsset
    {
        #region Inspector

        [SerializeField, AutoEnum] private BestiaryDescCategory m_Type = BestiaryDescCategory.Critter;
        [SerializeField, AutoEnum] private BestiaryDescFlags m_Flags = 0;
        [SerializeField, AutoEnum] private BestiaryDescSize m_Size = 0;

        [Header("Info")]
        [SerializeField, FilterBestiary(BestiaryDescCategory.Environment)] private BestiaryDesc m_ParentEnvironment = null;
        [SerializeField, FormerlySerializedAs("m_ScientificNameId"), ShowIfField("IsCritter")] private string m_ScientificName = null;
        [SerializeField] private TextId m_CommonNameId = null;
        [SerializeField] private TextId m_PluralCommonNameId = null;
        
        [Space]
        [SerializeField] private BFBase[] m_Facts = null;

        [Space]
        [SerializeField, ShowIfField("IsEnvironment")] private uint m_HistoricalRecordDuration = 2;
        [SerializeField, ShowIfField("IsEnvironment")] private Color m_WaterColor = ColorBank.Blue;

        [Header("Assets")]
        [SerializeField] private Sprite m_Icon = null;
        [SerializeField, ShowIfField("ShowSketch")] private Sprite m_Sketch = null;
        [SerializeField] private Color m_Color = ColorBank.White;
        [SerializeField, ShowIfField("IsCritter")] private SerializedHash32 m_ListenAudioEvent = null;

        [Header("Sorting")]
        [SerializeField] private ushort m_SortingOrder = 0;

        // HIDDEN

        [SerializeField, HideInInspector] private BFBase[] m_AllFacts;
        [SerializeField, HideInInspector] private BestiaryDesc[] m_ChildCritters;
        [SerializeField, HideInInspector] private ushort m_PlayerFactCount;
        [SerializeField, HideInInspector] private ushort m_AlwaysFactCount;
        [SerializeField, HideInInspector] private ushort m_InternalFactOffset;
        [SerializeField, HideInInspector] private ushort m_InternalFactCount;
        [SerializeField, HideInInspector] private WaterPropertyBlockF32 m_EnvState;
        [SerializeField, HideInInspector] private ActorStateTransitionSet m_StateTransitions;

        #endregion // Inspector

        public BestiaryDescCategory Category() { return m_Type; }
        public BestiaryDescFlags Flags() { return m_Flags; }
        public BestiaryDescSize Size() { return m_Size; }

        public BestiaryDesc ParentEnvironment()
        {
            Assert.True(m_Type == BestiaryDescCategory.Critter, "BestiaryDesc '{0}' is not a critter!", Id());
            return m_ParentEnvironment;
        }
        public IReadOnlyList<BestiaryDesc> ChildCritters
        {
            get
            {
                Assert.True(m_Type == BestiaryDescCategory.Environment, "BestiaryDesc '{0}' is not an environment!", Id());
                return m_ChildCritters;
            }
        }

        public string ScientificName() { return m_ScientificName; }
        public TextId CommonName() { return m_CommonNameId; }
        public TextId PluralCommonName() { return m_PluralCommonNameId.IsEmpty ? m_CommonNameId : m_PluralCommonNameId; }

        public ListSlice<BFBase> Facts { get { return m_AllFacts; } }
        public ListSlice<BFBase> PlayerFacts { get { return new ListSlice<BFBase>(m_AllFacts, 0, m_PlayerFactCount); } }
        public ListSlice<BFBase> AssumedFacts { get { return new ListSlice<BFBase>(m_AllFacts, m_PlayerFactCount, m_AlwaysFactCount); } }
        public ListSlice<BFBase> InternalFacts { get { return new ListSlice<BFBase>(m_AllFacts, m_InternalFactOffset, m_InternalFactCount); } }

        public bool HasCategory(BestiaryDescCategory inCategory)
        {
            return inCategory == BestiaryDescCategory.ALL || m_Type == inCategory;
        }

        public bool HasFlags(BestiaryDescFlags inFlags) { return (m_Flags & inFlags) != 0; }
        public bool HasAllFlags(BestiaryDescFlags inFlags) { return (m_Flags & inFlags) == inFlags; }

        public Sprite Icon() { return m_Icon; }

        public uint HistoricalRecordDuration()
        {
            Assert.True(m_Type == BestiaryDescCategory.Environment, "{0} is not an environment", name);
            return m_HistoricalRecordDuration;
        }

        public Color WaterColor()
        {
            Assert.True(m_Type == BestiaryDescCategory.Environment, "{0} is not an environment", name);
            return m_WaterColor;
        }

        public Sprite Sketch() { return m_Sketch; }
        public Color Color() { return m_Color; }

        public StringHash32 ListenAudio() { return m_ListenAudioEvent; }

        #region Facts

        public TFact FactOfType<TFact>() where TFact : BFBase
        {
            TFact result;
            foreach(var fact in m_AllFacts)
            {
                if ((result = fact as TFact) != null)
                    return result;
            }

            return null;
        }

        public IEnumerable<TFact> FactsOfType<TFact>() where TFact : BFBase
        {
            TFact result;
            foreach(var fact in m_AllFacts)
            {
                if ((result = fact as TFact) != null)
                    yield return result;
            }
        }

        #endregion // Facts

        #region Checks

        public WaterPropertyBlockF32 GetEnvironment()
        {
            Assert.True(m_Type == BestiaryDescCategory.Environment, "BestiaryDesc '{0}' is not an environment", name);
            return m_EnvState;
        }

        public ActorStateTransitionSet GetActorStateTransitions()
        {
            Assert.True(m_Type == BestiaryDescCategory.Critter, "BestiaryDesc '{0}' is not a critter", name);
            return m_StateTransitions;
        }

        public ActorStateId EvaluateActorState(in WaterPropertyBlockF32 inEnvironment, out WaterPropertyMask outAffected)
        {
            Assert.True(m_Type == BestiaryDescCategory.Critter, "BestiaryDesc '{0}' is not a critter", name);
            return m_StateTransitions.Evaluate(inEnvironment, out outAffected);
        }

        #endregion // Checks

        #region Sorting

        static public readonly Comparison<BestiaryDesc> SortById = (x, y) => x.Id().CompareTo(y.Id());
        static public readonly Comparison<BestiaryDesc> SortByOrder = (x, y) => x.m_SortingOrder.CompareTo(y.m_SortingOrder);
        static public readonly Comparison<BestiaryDesc> SortByEnvironment = (x, y) => EnvironmentSortingFirst(x, y, SortByOrder);

        static private int EnvironmentSortingFirst(BestiaryDesc x, BestiaryDesc y, Comparison<BestiaryDesc> inComparison)
        {
            BestiaryDesc envX = x.m_ParentEnvironment,
                        envY = y.m_ParentEnvironment;

            if (envX == envY)
            {
                return inComparison(x, y);
            }
            else if (envX == null)
            {
                return -1;
            }
            else if (envY == null)
            {
                return 1;
            }
            else
            {
                return envX.m_SortingOrder.CompareTo(envY.m_SortingOrder);
            }
        }

        #endregion // Sorting

        #region Editor

        #if UNITY_EDITOR

        int IOptimizableAsset.Order { get { return (int) m_Type; } }

        bool IOptimizableAsset.Optimize()
        {
            foreach(var fact in m_Facts)
            {
                Assert.NotNull(fact, "Null fact on BestiaryDesc '{0}'", name);
                fact.BakeProperties(this);
            }

            switch(m_Type)
            {
                case BestiaryDescCategory.Environment:
                    {
                        m_EnvState = ValidationUtils.FindAsset<WaterPropertyDB>().DefaultValues();
                        foreach(var fact in m_Facts)
                        {
                            BFWaterProperty waterProp = fact as BFWaterProperty;
                            if (waterProp != null)
                            {
                                m_EnvState[waterProp.Property] = waterProp.Value;
                            }
                        }
                        break;
                    }

                case BestiaryDescCategory.Critter:
                    {
                        m_StateTransitions.Reset();
                        foreach(var fact in m_Facts)
                        {
                            BFState state = fact as BFState;
                            if (state != null)
                            {
                                m_StateTransitions[state.Property] = state.Range;
                            }
                        }
                        break;
                    }
            }

            return true;
        }

        internal BFBase[] OwnedFacts { get { return m_Facts; } }

        internal void SetChildCritters(List<BestiaryDesc> inChildren)
        {
            m_ChildCritters = inChildren == null || inChildren.Count == 0 ? Array.Empty<BestiaryDesc>() : inChildren.ToArray();
        }

        internal void OptimizeSecondPass(List<BFBase> inReciprocalFacts)
        {
            if (inReciprocalFacts != null && inReciprocalFacts.Count > 0)
            {
                m_AllFacts = new BFBase[m_Facts.Length + inReciprocalFacts.Count];
                Array.Copy(m_Facts, 0, m_AllFacts, 0, m_Facts.Length);
                inReciprocalFacts.CopyTo(0, m_AllFacts, m_Facts.Length, inReciprocalFacts.Count);
            }
            else
            {
                m_AllFacts = (BFBase[]) m_Facts.Clone();
            }

            Array.Sort(m_AllFacts, BFBase.SortByMode);
            m_PlayerFactCount = 0;
            m_InternalFactCount = 0;
            m_AlwaysFactCount = 0;

            for(int i = 0; i < m_AllFacts.Length; i++)
            {
                switch(m_AllFacts[i].Mode)
                {
                    case BFMode.Player:
                        m_PlayerFactCount++;
                        break;

                    case BFMode.Internal:
                        m_InternalFactCount++;
                        break;

                    case BFMode.Always:
                        m_AlwaysFactCount++;
                        break;
                }
            }

            m_InternalFactOffset = (ushort) (m_AlwaysFactCount + m_PlayerFactCount);

            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            switch(m_Type)
            {
                case BestiaryDescCategory.Critter:
                    {
                        if (m_Size == BestiaryDescSize.Ecosystem)
                            m_Size = BestiaryDescSize.Large;
                        break;
                    }

                case BestiaryDescCategory.Environment:
                    {
                        if (m_Size != BestiaryDescSize.Ecosystem)
                            m_Size = BestiaryDescSize.Ecosystem;
                        break;
                    }
            }
        }

        private bool IsCritter()
        {
            return m_Type == BestiaryDescCategory.Critter;
        }

        private bool IsEnvironment()
        {
            return m_Type == BestiaryDescCategory.Environment;
        }

        private bool IsModels()
        {
            return m_Type == BestiaryDescCategory.Critter;
        }

        private bool ShowSketch()
        {
            return m_Type != BestiaryDescCategory.Model;
        }

        [ContextMenu("Load All In Directory")]
        private void FindAllFacts()
        {
            string myPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string myDirectory = Path.GetDirectoryName(myPath);
            m_Facts = ValidationUtils.FindAllAssets<BFBase>(myDirectory);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [UnityEditor.CustomEditor(typeof(BestiaryDesc)), UnityEditor.CanEditMultipleObjects]
        private class Inspector : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                UnityEditor.EditorGUILayout.Space();

                if (GUILayout.Button("Load All In Directory"))
                {
                    foreach(BestiaryDesc bestiary in targets)
                    {
                        bestiary.FindAllFacts();
                    }
                }
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }

    [LabeledEnum]
    public enum BestiaryDescCategory
    {
        Critter,
        Environment,
        Model,

        [Hidden]
        ALL
    }

    [Flags]
    public enum BestiaryDescFlags
    {
        Rare = 0x01,
        LargeCreature = 0x02,
        DoNotUseInExperimentation = 0x04,
        TreatAsHerd = 0x08,
        Human = 0x10,
        IgnoreStarvation = 0x20,
        IsMicroscopic = 0x40,
        TreatAsPlant = 0x80,
        DoNotUseInStressTank = 0x100,
        IsNotLiving = 0x200
    }

    public enum BestiaryDescSize
    {
        Tiny,
        Small,
        Medium,
        Large,

        Ecosystem = 8
    }
}