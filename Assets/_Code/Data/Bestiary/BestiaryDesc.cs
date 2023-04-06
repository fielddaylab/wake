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
        [SerializeField] private TextId m_EncodedMessageId = default;
        
        [SerializeField] internal BFBase[] m_Facts = Array.Empty<BFBase>();

        [SerializeField] private Color m_WaterColor = ColorBank.Blue;

        [SerializeField] internal Sprite m_Icon = null;
        [SerializeField] private Sprite m_EncodedIcon = null;
        [SerializeField, StreamingPath("png,jpg,jpeg,webm,mp4")] private string m_SketchPath = null;
        [SerializeField] private Color m_Color = ColorBank.White;

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
        [SerializeField, HideInInspector] private uint m_HistoricalRecordDuration = 2;
        [SerializeField, HideInInspector] private ActorStateTransitionSet m_StateTransitions;
        [SerializeField, HideInInspector] private StringHash32 m_FirstStressFactId;

        [NonSerialized] private string m_CachedName;

        #endregion // Inspector

        public BestiaryDescCategory Category() { return m_Type; }
        public BestiaryDescFlags Flags() { return m_Flags; }
        public BestiaryDescSize Size() { return m_Size; }

        [LeafLookup("StationId")] public StringHash32 StationId() { return m_StationId; }
        [LeafLookup("DiveSiteId")] public StringHash32 DiveSiteId() { Assert.True(m_Type == BestiaryDescCategory.Environment); return m_DiveSiteId; }

        public string ScientificName() { return m_ScientificName; }
        [LeafLookup("Name")] public TextId CommonName() { return m_CommonNameId; }
        [LeafLookup("PluralName")] public TextId PluralCommonName() { return m_PluralCommonNameId.IsEmpty ? m_CommonNameId : m_PluralCommonNameId; }
        public TextId Description() { return m_DescriptionId; }
        public TextId EncodedMessage() { Assert.True((m_Flags & BestiaryDescFlags.IsSpecter) != 0); return m_EncodedMessageId; }

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
        public Sprite EncodedIcon() { return m_EncodedIcon; }

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
        public StreamedImageSet ImageSet() { return new StreamedImageSet(m_SketchPath, m_Icon); }

        #region Facts

        public StringHash32 FirstStressedFactId()
        {
            Assert.True(m_Type == BestiaryDescCategory.Critter, "BestiaryDesc '{0}' is not a critter", name);
            return m_FirstStressFactId;
        }

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

        public IEnumerable<BFBase> FactsOfType(BFTypeId inTypeId)
        {
            foreach(var fact in m_AllFacts)
            {
                if (fact.Type == inTypeId)
                    yield return fact;
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

        public bool HasOrganism(StringHash32 inOrganismId)
        {
            Assert.True(m_Type == BestiaryDescCategory.Environment, "{0} is not an environment", name);
            return ArrayUtils.Contains(m_InhabitingOrganisms, inOrganismId);
        }

        #endregion // Checks

        #region Sorting

        public void CacheName()
        {
            m_CachedName = Loc.Find(m_CommonNameId);
        }

        static public readonly Comparison<BestiaryDesc> SortById = (x, y) => x.Id().CompareTo(y.Id());
        static public readonly Comparison<BestiaryDesc> SortByOrder = (x, y) => x.m_SortingOrder.CompareTo(y.m_SortingOrder);
        
        static public readonly Comparison<BestiaryDesc> SortByEnvironment = (x, y) => EnvironmentSortingFirst(x, y, SortByOrder);
        static public readonly Comparison<BestiaryDesc> SortByName = (x, y) => x.m_CachedName.CompareTo(y.m_CachedName);

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

        static public readonly Comparison<BestiaryDesc> SortNatural = (x, y) => {
            int env = x.m_StationSortingOrder - y.m_StationSortingOrder;
            if (env != 0) {
                return env;
            }

            int order = x.m_SortingOrder - y.m_SortingOrder;
            if (order != 0) {
                return order;
            }

            return x.m_CachedName.CompareTo(y.m_CachedName);
        };

        static public readonly Comparison<BestiaryDesc> SortNaturalInStation = (x, y) => {
            int order = x.m_SortingOrder - y.m_SortingOrder;
            if (order != 0) {
                return order;
            }

            return x.m_CachedName.CompareTo(y.m_CachedName);
        };

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
        DoNotUseInExperimentation = 0x04,
        TreatAsHerd = 0x08,
        Human = 0x10,
        IgnoreStarvation = 0x20,
        IsMicroscopic = 0x40,
        TreatAsPlant = 0x80,
        DoNotUseInStressTank = 0x100,
        IsNotLiving = 0x200,
        HideInBestiary = 0x400,
        IsSpecter = 0x800
    }

    public enum BestiaryDescSize
    {
        Tiny,
        Small,
        Medium,
        Large,

        Ecosystem = 8
    }

    [Serializable]
    public struct TaggedBestiaryDesc
    {
        public BestiaryDesc Entity;
        public StringHash32 Tag;

        public TaggedBestiaryDesc(BestiaryDesc inEntity)
        {
            Entity = inEntity;
            Tag = inEntity.StationId();
        }

        public TaggedBestiaryDesc(BestiaryDesc inEntity, StringHash32 inStationId)
        {
            Entity = inEntity;
            Tag = inStationId;
        }
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