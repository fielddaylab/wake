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
    [CreateAssetMenu(menuName = "Aqualab Content/Bestiary Entry", fileName = "NewBestiaryEntry")]
    public partial class BestiaryDesc : DBObject
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
        [SerializeField] private TextId m_DescriptionId = default;
        
        [SerializeField] internal BFBase[] m_Facts = null;

        [SerializeField] private uint m_HistoricalRecordDuration = 2;
        [SerializeField] private Color m_WaterColor = ColorBank.Blue;

        [SerializeField] private Sprite m_Icon = null;
        [SerializeField, StreamingPath("png,jpg,jpeg,webm,mp4")] private string m_SketchPath = null;
        [SerializeField] private Color m_Color = ColorBank.White;
        [SerializeField] private SerializedHash32 m_ListenAudioEvent = null;

        [SerializeField] private ushort m_SortingOrder = 0;

        // HIDDEN

        [SerializeField, HideInInspector] private BFBase[] m_AllFacts;
        [SerializeField, HideInInspector] private ushort m_PlayerFactCount;
        [SerializeField, HideInInspector] private ushort m_AlwaysFactCount;
        [SerializeField, HideInInspector] private ushort m_InternalFactOffset;
        [SerializeField, HideInInspector] private ushort m_InternalFactCount;
        [SerializeField, HideInInspector] private int m_StationSortingOrder;
        [SerializeField, HideInInspector] private WaterPropertyBlockF32 m_EnvState;
        [SerializeField, HideInInspector] private StringHash32[] m_InhabitingOrganisms;
        [SerializeField, HideInInspector] private ActorStateTransitionSet m_StateTransitions;

        #endregion // Inspector

        public BestiaryDescCategory Category() { return m_Type; }
        public BestiaryDescFlags Flags() { return m_Flags; }
        public BestiaryDescSize Size() { return m_Size; }

        public StringHash32 StationId() { return m_StationId; }
        public StringHash32 DiveSiteId() { Assert.True(m_Type == BestiaryDescCategory.Environment); return m_DiveSiteId; }

        public string ScientificName() { return m_ScientificName; }
        public TextId CommonName() { return m_CommonNameId; }
        public TextId PluralCommonName() { return m_PluralCommonNameId.IsEmpty ? m_CommonNameId : m_PluralCommonNameId; }
        public TextId Description() { return m_DescriptionId; }

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

        public ListSlice<StringHash32> Organisms()
        {
            Assert.True(m_Type == BestiaryDescCategory.Environment, "{0} is not an environment", name);
            return m_InhabitingOrganisms;
        }

        public string SketchPath() { return m_SketchPath; }
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
    }

    [LabeledEnum(false)]
    public enum BestiaryDescCategory
    {
        [Label("Organism")]
        Critter,

        [Label("Ecosystem")]
        Environment,

        [Hidden]
        _Unused,

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

        public override string Name(DBObject inObject) {
            BestiaryDesc desc = (BestiaryDesc) inObject;
            StringHash32 stationId = desc.StationId();
            if (stationId.IsEmpty) {
                return string.Format("Shared/{0}", desc.name);
            } else {
                return string.Format("{0}/{1}", stationId.ToDebugString(), desc.name);
            }
        }
    }
}