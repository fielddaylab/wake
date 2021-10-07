using System;
using System.Collections.Generic;
using System.IO;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Bestiary Entry", fileName = "NewBestiaryEntry")]
    public class BestiaryDesc : DBObject, IOptimizableAsset
    {
        #region Inspector

        [SerializeField, AutoEnum] private BestiaryDescCategory m_Type = BestiaryDescCategory.Critter;
        [SerializeField, AutoEnum] private BestiaryDescFlags m_Flags = 0;
        [SerializeField, AutoEnum] private BestiaryDescSize m_Size = 0;

        [SerializeField, MapId(MapCategory.Station)] private StringHash32 m_StationId = null;
        [SerializeField, MapId(MapCategory.DiveSite)] private StringHash32 m_DiveSiteId = null;
        [SerializeField] private string m_ScientificName = null;
        [SerializeField] private TextId m_CommonNameId = default;
        [SerializeField] private TextId m_PluralCommonNameId = default;
        
        [SerializeField] private BFBase[] m_Facts = null;

        [SerializeField] private uint m_HistoricalRecordDuration = 2;
        [SerializeField] private Color m_WaterColor = ColorBank.Blue;

        [SerializeField] private Sprite m_Icon = null;
        [SerializeField] private Sprite m_Sketch = null;
        [SerializeField] private Color m_Color = ColorBank.White;
        [SerializeField] private SerializedHash32 m_ListenAudioEvent = null;

        [SerializeField] private ushort m_SortingOrder = 0;

        // HIDDEN

        [SerializeField, HideInInspector] private BFBase[] m_AllFacts;
        [SerializeField, HideInInspector] private ushort m_PlayerFactCount;
        [SerializeField, HideInInspector] private ushort m_AlwaysFactCount;
        [SerializeField, HideInInspector] private ushort m_InternalFactOffset;
        [SerializeField, HideInInspector] private ushort m_InternalFactCount;
        [SerializeField] private int m_StationSortingOrder;
        [SerializeField, HideInInspector] private WaterPropertyBlockF32 m_EnvState;
        [SerializeField, HideInInspector] private ActorStateTransitionSet m_StateTransitions;

        #endregion // Inspector

        public BestiaryDescCategory Category() { return m_Type; }
        public BestiaryDescFlags Flags() { return m_Flags; }
        public BestiaryDescSize Size() { return m_Size; }

        public StringHash32 StationId() { return m_StationId; }

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
            int envX = x.m_StationSortingOrder,
                envY = y.m_StationSortingOrder;

            if (envX == envY)
            {
                return inComparison(x, y);
            }
            else
            {
                return envX.CompareTo(envY);
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

            if (m_StationId.IsEmpty) {
                m_StationSortingOrder = -1;
            } else {
                MapDesc map = ValidationUtils.FindAsset<MapDesc>(m_StationId.ToDebugString());
                Assert.NotNull(map, "Map with id '{0}' was unable to be found on BestiaryDesc '{1}'", m_StationId, name);
                m_StationSortingOrder = map.SortingOrder();
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

        [ContextMenu("Load All In Directory")]
        private void FindAllFacts()
        {
            string myPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string myDirectory = Path.GetDirectoryName(myPath);
            m_Facts = ValidationUtils.FindAllAssets<BFBase>(myDirectory);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [UnityEditor.CustomEditor(typeof(BestiaryDesc)), UnityEditor.CanEditMultipleObjects]
        private class Inspector : UnityEditor.Editor {
            private SerializedProperty m_TypeProperty;
            private SerializedProperty m_FlagsProperty;
            private SerializedProperty m_SizeProperty;
            private SerializedProperty m_StationIdProperty;
            private SerializedProperty m_DiveSiteIdProperty;
            private SerializedProperty m_ScientificNameProperty;
            private SerializedProperty m_CommonNameIdProperty;
            private SerializedProperty m_PluralCommonNameIdProperty;
            private SerializedProperty m_FactsProperty;
            private SerializedProperty m_HistoricalRecordDurationProperty;
            private SerializedProperty m_WaterColorProperty;
            private SerializedProperty m_IconProperty;
            private SerializedProperty m_SketchProperty;
            private SerializedProperty m_ColorProperty;
            private SerializedProperty m_ListenAudioEventProperty;
            private SerializedProperty m_SortingOrderProperty;

            private void OnEnable() {
                m_TypeProperty = serializedObject.FindProperty("m_Type");
                m_FlagsProperty = serializedObject.FindProperty("m_Flags");
                m_SizeProperty = serializedObject.FindProperty("m_Size");
                m_StationIdProperty = serializedObject.FindProperty("m_StationId");
                m_DiveSiteIdProperty = serializedObject.FindProperty("m_DiveSiteId");
                m_ScientificNameProperty = serializedObject.FindProperty("m_ScientificName");
                m_CommonNameIdProperty = serializedObject.FindProperty("m_CommonNameId");
                m_PluralCommonNameIdProperty = serializedObject.FindProperty("m_PluralCommonNameId");
                m_FactsProperty = serializedObject.FindProperty("m_Facts");
                m_HistoricalRecordDurationProperty = serializedObject.FindProperty("m_HistoricalRecordDuration");
                m_WaterColorProperty = serializedObject.FindProperty("m_WaterColor");
                m_IconProperty = serializedObject.FindProperty("m_Icon");
                m_SketchProperty = serializedObject.FindProperty("m_Sketch");
                m_ColorProperty = serializedObject.FindProperty("m_Color");
                m_ListenAudioEventProperty = serializedObject.FindProperty("m_ListenAudioEvent");
                m_SortingOrderProperty = serializedObject.FindProperty("m_SortingOrder");
            }

            public override void OnInspectorGUI() {
                serializedObject.UpdateIfRequiredOrScript();

                BestiaryDescCategory category = m_TypeProperty.hasMultipleDifferentValues ? BestiaryDescCategory.ALL : (BestiaryDescCategory) m_TypeProperty.intValue;

                EditorGUILayout.PropertyField(m_TypeProperty);
                EditorGUILayout.PropertyField(m_FlagsProperty);
                EditorGUILayout.PropertyField(m_StationIdProperty);

                switch(category) {
                    case BestiaryDescCategory.Critter: {
                        Header("Organism");
                        EditorGUILayout.PropertyField(m_SizeProperty);
                        EditorGUILayout.PropertyField(m_SortingOrderProperty);

                        Header("Text");
                        EditorGUILayout.PropertyField(m_ScientificNameProperty);
                        EditorGUILayout.PropertyField(m_CommonNameIdProperty);
                        EditorGUILayout.PropertyField(m_PluralCommonNameIdProperty);

                        Header("Assets");
                        EditorGUILayout.PropertyField(m_IconProperty);
                        EditorGUILayout.PropertyField(m_SketchProperty);
                        EditorGUILayout.PropertyField(m_ColorProperty);
                        EditorGUILayout.PropertyField(m_ListenAudioEventProperty);

                        Header("Sorting");
                        EditorGUILayout.PropertyField(m_SortingOrderProperty);
                        break;
                    }

                    case BestiaryDescCategory.Environment: {
                        
                        Header("Environment");
                        EditorGUILayout.PropertyField(m_DiveSiteIdProperty);
                        EditorGUILayout.PropertyField(m_HistoricalRecordDurationProperty);
                        EditorGUILayout.PropertyField(m_WaterColorProperty);

                        Header("Text");
                        EditorGUILayout.PropertyField(m_CommonNameIdProperty);

                        Header("Assets");
                        EditorGUILayout.PropertyField(m_IconProperty);
                        EditorGUILayout.PropertyField(m_SketchProperty);
                        EditorGUILayout.PropertyField(m_ColorProperty);

                        Header("Sorting");
                        EditorGUILayout.PropertyField(m_SortingOrderProperty);
                        break;
                    }
                    
                    case BestiaryDescCategory.Model: {
                        
                        Header("Model");
                        EditorGUILayout.PropertyField(m_CommonNameIdProperty);
                        EditorGUILayout.PropertyField(m_IconProperty);
                        EditorGUILayout.PropertyField(m_ColorProperty);
                        break;
                    }
                }

                Header("Facts");
                m_FactsProperty.isExpanded = true;
                EditorGUILayout.PropertyField(m_FactsProperty);

                if (GUILayout.Button("Load All In Directory")) {
                    foreach(BestiaryDesc bestiary in targets) {
                        bestiary.FindAllFacts();
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }

            static private void Header(string inHeader) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(inHeader, EditorStyles.boldLabel);
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

    public class FilterBestiaryIdAttribute : DBObjectIdAttribute {

        public BestiaryDescCategory Category;

        public FilterBestiaryIdAttribute(BestiaryDescCategory inCategory = BestiaryDescCategory.ALL) : base(typeof(BestiaryDesc)) {
            Category = inCategory;
        }

        public override bool Filter(DBObject inObject) {
            return ((BestiaryDesc) inObject).HasCategory(Category);
        }
    }
}