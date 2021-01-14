using System;
using Aqua;
using BeauData;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    [CreateAssetMenu(menuName = "Aqualab/Energy/Actor Type")]
    public class ActorType : ScriptableObject, ISimType<ActorType>, IKeyValuePair<FourCC, ActorType>, ICopyCloneable<ActorType>, IFactVisitor
    {
        #region Types

        [Serializable]
        public class EatingConfig : ICopyCloneable<EatingConfig>
        {
            public EdibleConfig[] EdibleActors;

            public ushort BaseEatSize;
            public float MassEatSize;
            public float MaxSizeMultiplier;

            public EatingConfig Clone()
            {
                return CloneUtils.DefaultClone(this);
            }

            public void CopyFrom(EatingConfig inClone)
            {
                CloneUtils.CopyFrom(ref EdibleActors, inClone.EdibleActors);
                
                BaseEatSize = inClone.BaseEatSize;
                MassEatSize = inClone.MassEatSize;
                MaxSizeMultiplier = inClone.MaxSizeMultiplier;
            }
        }

        [Serializable]
        public struct EdibleConfig
        {
            [ActorTypeId] public FourCC ActorType;
            [UnityEngine.Serialization.FormerlySerializedAs("ConversionRate")] public float Rate;
            public QualitativeParamsF Qualitative;
        }

        [Serializable]
        public struct ResourceRequirementConfig
        {
            [VarTypeId] public FourCC ResourceId;
            public ushort BaseValue;
            public float MassValue;

            public QualitativeParams Qualitative;
        }

        [Serializable]
        public struct PropertyCompareConfig
        {
            [VarTypeId] public FourCC PropertyId;
            public CompareOp Comparison;
            public float BaseValue;
            public float MassValue;

            public QualitativeParamsF Qualitative;
        }

        [Serializable]
        public class RequirementsConfig : ICopyCloneable<RequirementsConfig>
        {
            public ResourceRequirementConfig[] DesiredResources;
            public PropertyCompareConfig[] DesiredProperties;
            public ResourceRequirementConfig[] ProducingResources;

            public RequirementsConfig Clone()
            {
                return CloneUtils.DefaultClone(this);
            }

            public void CopyFrom(RequirementsConfig inClone)
            {
                CloneUtils.CopyFrom(ref DesiredResources, inClone.DesiredResources);
                CloneUtils.CopyFrom(ref DesiredProperties, inClone.DesiredProperties);
                CloneUtils.CopyFrom(ref ProducingResources, inClone.ProducingResources);
            }
        }

        [Serializable]
        public class ReproductionConfig : ICopyCloneable<ReproductionConfig>
        {
            // when/children count
            [UnityEngine.Serialization.FormerlySerializedAs("Frequency")] public ushort Interval;
            public ushort Count;

            public QualitativeParams QualitativeInterval;

            // prerequisites
            public ushort MinAge;
            public ushort MinMass;

            public ResourceRequirementConfig[] ResourceThresholds;
            public PropertyCompareConfig[] PropertyThresholds;

            public ActorCount MinActorMass;

            public ReproductionConfig Clone()
            {
                return CloneUtils.DefaultClone(this);
            }

            public void CopyFrom(ReproductionConfig inClone)
            {
                Interval = inClone.Interval;
                Count = inClone.Count;
                QualitativeInterval = inClone.QualitativeInterval;

                MinAge = inClone.MinAge;
                MinMass = inClone.MinMass;

                CloneUtils.CopyFrom(ref ResourceThresholds, inClone.ResourceThresholds);
                CloneUtils.CopyFrom(ref PropertyThresholds, inClone.PropertyThresholds);

                MinActorMass = inClone.MinActorMass;
            }
        }

        [Serializable]
        public class GrowthConfig : ICopyCloneable<GrowthConfig>
        {
            public ushort StartingMass;
            public ushort MaxMass;

            [UnityEngine.Serialization.FormerlySerializedAs("Frequency")] public ushort Interval;
            public ushort MinGrowth;

            public QualitativeParams QualitativeInterval;

            public ResourceRequirementConfig[] ImprovedGrowthResourceThresholds;
            public PropertyCompareConfig[] ImprovedGrowthPropertyThresholds;
            public ushort ImprovedGrowth;

            public GrowthConfig Clone()
            {
                return CloneUtils.DefaultClone(this);
            }

            public void CopyFrom(GrowthConfig inClone)
            {
                StartingMass = inClone.StartingMass;
                MaxMass = inClone.MaxMass;

                QualitativeInterval = inClone.QualitativeInterval;

                Interval = inClone.Interval;
                MinGrowth = inClone.MinGrowth;

                CloneUtils.CopyFrom(ref ImprovedGrowthResourceThresholds, inClone.ImprovedGrowthResourceThresholds);
                CloneUtils.CopyFrom(ref ImprovedGrowthPropertyThresholds, inClone.ImprovedGrowthPropertyThresholds);
                ImprovedGrowth = inClone.ImprovedGrowth;
            }
        }

        [Serializable]
        public class DeathConfig : ICopyCloneable<DeathConfig>
        {
            public ushort Age;
            public QualitativeParams QualitativeAge;

            public VarPair[] ResourceStarvation;
            public VarPair[] PropertyStarvation;

            public ushort MassAgeThreshold;
            public ushort Mass;

            public DeathConfig Clone()
            {
                return CloneUtils.DefaultClone(this);
            }

            public void CopyFrom(DeathConfig inClone)
            {
                Age = inClone.Age;
                QualitativeAge = inClone.QualitativeAge;
                CloneUtils.CopyFrom(ref ResourceStarvation, inClone.ResourceStarvation);
                CloneUtils.CopyFrom(ref PropertyStarvation, inClone.PropertyStarvation);
                MassAgeThreshold = inClone.MassAgeThreshold;
                Mass = inClone.Mass;
            }
        }

        [Serializable]
        public class DisplayConfig
        {
            public Sprite Image;
            public ushort MassScale;
            public Color Color = Color.white;
            
            public bool DisplayPopulation;
            public bool DisplayMass;
        }

        [Serializable]
        public struct ConfigRange
        {
            public ushort SoftCap;
            public ushort Increment;
        }

        #endregion // Types

        #region Inspector

        [SerializeField, ActorTypeId] private FourCC m_Id = FourCC.Zero;
        [SerializeField] private string m_ScriptName = null;
        [SerializeField, AutoEnum] private ActorTypeFlags m_Flags = default(ActorTypeFlags);
        [SerializeField] private ushort m_MaxActors = 0;

        [Header("Resources")]
        [SerializeField] private RequirementsConfig m_ResourceRequirements = default(RequirementsConfig);

        [Header("Activities")]
        [SerializeField] private EatingConfig m_EatingSettings = null;
        [SerializeField] private GrowthConfig m_GrowthSettings = default(GrowthConfig);
        [SerializeField] private ReproductionConfig m_ReproductionSettings = default(ReproductionConfig);
        [SerializeField] private DeathConfig m_DeathSettings = default(DeathConfig);

        [Header("Other")]
        [SerializeField] private ConfigRange m_ConfigSettings = default(ConfigRange);
        [SerializeField] private DisplayConfig m_DisplaySettings = default(DisplayConfig);
        [SerializeField] private PropertyBlock m_ExtraData = default(PropertyBlock);

        #endregion // Inspector

        [NonSerialized] private SimTypeDatabase<ActorType> m_Database;
        [NonSerialized] private ActorType m_Original;

        #region KeyValuePair

        FourCC IKeyValuePair<FourCC, ActorType>.Key { get { return m_Id; } }

        ActorType IKeyValuePair<FourCC, ActorType>.Value { get { return this; } }

        #endregion // KeyValuePair

        #region ISimType

        void ISimType<ActorType>.Hook(SimTypeDatabase<ActorType> inDatabase)
        {
            m_Database = inDatabase;
        }

        void ISimType<ActorType>.Unhook(SimTypeDatabase<ActorType> inDatabase)
        {
            if (m_Database == inDatabase)
            {
                m_Database = null;
            }
        }

        #endregion // ISimType

        #region ICopyCloneable

        /// <summary>
        /// Creates a clone of this ActorType.
        /// </summary>
        public ActorType Clone()
        {
            ActorType clone = Instantiate(this);
            clone.m_Original = this;
            return clone;
        }

        /// <summary>
        /// Copies data from the given ActorType.
        /// </summary>
        public void CopyFrom(ActorType inType)
        {
            m_ResourceRequirements.CopyFrom(inType.m_ResourceRequirements);
            m_EatingSettings.CopyFrom(inType.m_EatingSettings);
            m_GrowthSettings.CopyFrom(inType.m_GrowthSettings);
            m_ReproductionSettings.CopyFrom(inType.m_ReproductionSettings);
            m_DeathSettings.CopyFrom(inType.m_DeathSettings);
        }

        #endregion // ICopyCloneable

        #region Accessors

        public FourCC Id() { return m_Id; }
        public string ScriptName() { return m_ScriptName; }
        public ActorTypeFlags Flags() { return m_Flags; }
        public ushort MaxActors() { return m_MaxActors; }

        public RequirementsConfig Requirements() { return m_ResourceRequirements; }

        public EatingConfig EatSettings() { return m_EatingSettings; }
        public GrowthConfig GrowthSettings() { return m_GrowthSettings; }
        public ReproductionConfig ReproductionSettings() { return m_ReproductionSettings; }
        public DeathConfig DeathSettings() { return m_DeathSettings; }

        public ConfigRange ConfigSettings() { return m_ConfigSettings; }
        public DisplayConfig DisplaySettings() { return m_DisplaySettings; }
        public PropertyBlock ExtraData() { return m_ExtraData; }

        public ActorType OriginalType() { return m_Original; }

        #endregion // Accessors

        private ActorType()
        {
            m_Original = this;
        }

        #region Operations

        /// <summary>
        /// Initializes an actor state.
        /// </summary>
        public void CreateActor(ref ActorState ioState)
        {
            ioState.Type = m_Id;
            ioState.Flags = ActorStateFlags.Alive;

            ioState.Age = 0;
            ioState.Mass = m_GrowthSettings.StartingMass;

            ioState.OffsetA = 0;
            ioState.OffsetB = 0;
        }

        /// <summary>
        /// Sets up resource requirements and production.
        /// </summary>
        public void SetupResourceExchange(ref ActorState ioState, in EnergySimContext inContext)
        {
            ioState.DesiredResources = default(VarState<ushort>);

            for (int reqIdx = 0; reqIdx < m_ResourceRequirements.DesiredResources.Length; ++reqIdx)
            {
                ResourceRequirementConfig req = m_ResourceRequirements.DesiredResources[reqIdx];
                int resIdx = inContext.Database.Resources.IdToIndex(req.ResourceId);
                ioState.DesiredResources[resIdx] += (ushort)(req.BaseValue + req.MassValue * ioState.Mass);
            }

            ioState.ProducingResources = default(VarState<ushort>);

            for (int reqIdx = 0; reqIdx < m_ResourceRequirements.ProducingResources.Length; ++reqIdx)
            {
                ResourceRequirementConfig req = m_ResourceRequirements.ProducingResources[reqIdx];
                int resIdx = inContext.Database.Resources.IdToIndex(req.ResourceId);
                ioState.ProducingResources[resIdx] += (ushort)(req.BaseValue + req.MassValue * ioState.Mass);
            }
        }

        /// <summary>
        /// Evaluates environment properties.
        /// </summary>
        public void EvaluateProperties(ref ActorState ioState, in EnergySimContext inContext)
        {
            ioState.MetPropertyRequirements = ushort.MaxValue;

            for (int reqIdx = 0; reqIdx < m_ResourceRequirements.DesiredProperties.Length; ++reqIdx)
            {
                PropertyCompareConfig req = m_ResourceRequirements.DesiredProperties[reqIdx];
                int propIdx = inContext.Database.Properties.IdToIndex(req.PropertyId);
                float threshold = req.BaseValue + req.MassValue * ioState.Mass;
                bool bMet = req.Comparison.Evaluate(inContext.CachedCurrent.Environment.Properties[propIdx], threshold);
                if (!bMet)
                {
                    ioState.MetPropertyRequirements &= (ushort)(~(1 << propIdx));
                }
            }
        }

        /// <summary>
        /// Performs growth operations.
        /// </summary>
        public ushort PerformGrowth(ref ActorState ioState, in EnergySimContext inContext)
        {
            ushort growth = PredictGrowth(ioState, inContext);
            ioState.Mass += growth;
            return growth;
        }

        /// <summary>
        /// Sets this ActorType configuration as dirty.
        /// </summary>
        public void Dirty()
        {
            m_Database?.Dirty();
        }

        #endregion // Operations

        #region Evaluation

        /// <summary>
        /// Predicts the amount of growth an actor will undergo this tick.
        /// </summary>
        public ushort PredictGrowth(in ActorState inActorState, in EnergySimContext inContext)
        {
            // if this never grows, don't do anything
            if ((m_GrowthSettings.MinGrowth == 0 && m_GrowthSettings.ImprovedGrowth == 0) || m_GrowthSettings.Interval <= 0)
                return 0;

            ushort remainingMass = (ushort)(m_GrowthSettings.MaxMass - inActorState.Mass);

            // mass check
            if (remainingMass <= 0)
                return 0;

            // frequency check
            if (((inActorState.Age + inActorState.OffsetA) % m_GrowthSettings.Interval) != 0)
                return 0;

            ushort growth = m_GrowthSettings.MinGrowth;

            if (m_GrowthSettings.ImprovedGrowthPropertyThresholds.Length > 0 || m_GrowthSettings.ImprovedGrowthResourceThresholds.Length > 0)
            {
                if (AllResources(m_GrowthSettings.ImprovedGrowthResourceThresholds, inActorState.Mass, inContext)
                    && AllProperties(m_GrowthSettings.ImprovedGrowthPropertyThresholds, inActorState.Mass, inContext))
                {
                    growth = m_GrowthSettings.ImprovedGrowth;
                }
            }

            return Math.Min(growth, remainingMass);
        }

        /// <summary>
        /// Returns whether or not the given actor should reproduce.
        /// </summary>
        public bool ShouldReproduce(in ActorState inActorState, in EnergySimContext inContext)
        {
            // age/frequency
            if (inActorState.Age == 0 || m_ReproductionSettings.Interval == 0 || inActorState.Age < m_ReproductionSettings.MinAge)
                return false;

            if (((inActorState.Age + inActorState.OffsetB) % m_ReproductionSettings.Interval) != 0)
                return false;

            // mass
            if (inActorState.Mass < m_ReproductionSettings.MinMass)
                return false;

            if (m_ReproductionSettings.ResourceThresholds.Length > 0 || m_ReproductionSettings.PropertyThresholds.Length > 0)
            {
                if (!AllResources(m_ReproductionSettings.ResourceThresholds, inActorState.Mass, inContext)
                    || !AllProperties(m_ReproductionSettings.PropertyThresholds, inActorState.Mass, inContext))
                {
                    return false;
                }
            }

            // actor count
            if (m_ReproductionSettings.MinActorMass.Id != FourCC.Zero && inContext.CachedCurrent.Masses != null)
            {
                int actorIdx = inContext.Database.Actors.IdToIndex(m_ReproductionSettings.MinActorMass.Id);
                if (inContext.CachedCurrent.Masses[actorIdx] < m_ReproductionSettings.MinActorMass.Count)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the weight for an eating target.
        /// </summary>
        public float GetEatTargetRate(in ActorState inActorState, FourCC inTargetType, in EnergySimContext inContext)
        {
            for (int i = m_EatingSettings.EdibleActors.Length - 1; i >= 0; --i)
            {
                if (m_EatingSettings.EdibleActors[i].ActorType == inTargetType)
                    return m_EatingSettings.EdibleActors[i].Rate;
            }

            return 0;
        }

        /// <summary>
        /// Returns the bite size for an actor.
        /// </summary>
        public void GetEatSize(in ActorState inActorState, in EnergySimContext inContext, out ushort outBiteSize, out ushort outMaxSize)
        {
            outBiteSize = (ushort)(m_EatingSettings.BaseEatSize + (inActorState.Mass * m_EatingSettings.MassEatSize));
            outMaxSize = (ushort)(outBiteSize * m_EatingSettings.MaxSizeMultiplier);
        }

        /// <summary>
        /// Returns whether or not the given actor should die.
        /// </summary>
        public bool ShouldDie(in ActorState inState, in EnergySimContext inContext)
        {
            if (m_DeathSettings.Age > 0 && inState.Age >= m_DeathSettings.Age)
            {
                return true;
            }

            if (inState.Mass < m_DeathSettings.Mass)
            {
                if (m_DeathSettings.MassAgeThreshold > 0)
                    return inState.Age >= m_DeathSettings.MassAgeThreshold;
                return true;
            }

            for (int reqIdx = 0; reqIdx < m_DeathSettings.ResourceStarvation.Length; ++reqIdx)
            {
                VarPair req = m_DeathSettings.ResourceStarvation[reqIdx];
                int resIdx = inContext.Database.Resources.IdToIndex(req.Id);
                if (inState.ResourceStarvation[resIdx] >= req.Value)
                    return true;
            }

            for (int reqIdx = 0; reqIdx < m_DeathSettings.PropertyStarvation.Length; ++reqIdx)
            {
                VarPair req = m_DeathSettings.PropertyStarvation[reqIdx];
                int propIdx = inContext.Database.Properties.IdToIndex(req.Id);
                if (inState.PropertyStarvation[propIdx] >= req.Value)
                    return true;
            }

            return false;
        }

        #endregion // Evaluation

        #region Resources

        static private bool AnyResources(ResourceRequirementConfig[] inResources, ushort inMass, in EnergySimContext inContext)
        {
            for (int reqIdx = 0; reqIdx < inResources.Length; ++reqIdx)
            {
                ResourceRequirementConfig req = inResources[reqIdx];
                int resIdx = inContext.Database.Resources.IdToIndex(req.ResourceId);
                if (inContext.CachedCurrent.Environment.OwnedResources[resIdx] >= (req.BaseValue + req.MassValue * inMass))
                    return true;
            }

            return false;
        }

        static private bool AllResources(ResourceRequirementConfig[] inResources, ushort inMass, in EnergySimContext inContext)
        {
            for (int reqIdx = 0; reqIdx < inResources.Length; ++reqIdx)
            {
                ResourceRequirementConfig req = inResources[reqIdx];
                int resIdx = inContext.Database.Resources.IdToIndex(req.ResourceId);
                if (inContext.CachedCurrent.Environment.OwnedResources[resIdx] >= (req.BaseValue + req.MassValue * inMass))
                    return false;
            }

            return true;
        }

        #endregion // Resources

        #region Properties

        static private bool AnyProperties(PropertyCompareConfig[] inProperties, ushort inMass, in EnergySimContext inContext)
        {
            for (int reqIdx = 0; reqIdx < inProperties.Length; ++reqIdx)
            {
                PropertyCompareConfig req = inProperties[reqIdx];
                int propIdx = inContext.Database.Properties.IdToIndex(req.PropertyId);
                if (req.Comparison.Evaluate(inContext.CachedCurrent.Environment.Properties[propIdx], (req.BaseValue + req.MassValue * inMass)))
                    return true;
            }

            return false;
        }

        static private bool AllProperties(PropertyCompareConfig[] inProperties, ushort inMass, in EnergySimContext inContext)
        {
            for (int reqIdx = 0; reqIdx < inProperties.Length; ++reqIdx)
            {
                PropertyCompareConfig req = inProperties[reqIdx];
                int propIdx = inContext.Database.Properties.IdToIndex(req.PropertyId);
                if (req.Comparison.Evaluate(inContext.CachedCurrent.Environment.Properties[propIdx], (req.BaseValue + req.MassValue * inMass)))
                    return false;
            }

            return true;
        }

        #endregion // Properties

        #region Unity Events

        #if UNITY_EDITOR

        private void OnValidate()
        {
            Dirty();
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events
    
        #region IFactVisitor

        void IFactVisitor.Visit(BFBase inFact, PlayerFactParams inParams)
        {
            throw new NotImplementedException();
        }

        void IFactVisitor.Visit(BFBody inFact, PlayerFactParams inParams)
        {
            throw new NotImplementedException();
        }

        void IFactVisitor.Visit(BFWaterProperty inFact, PlayerFactParams inParams)
        {
            throw new NotImplementedException();
        }

        void IFactVisitor.Visit(BFEat inFact, PlayerFactParams inParams)
        {
            throw new NotImplementedException();
        }

        void IFactVisitor.Visit(BFGrow inFact, PlayerFactParams inParams)
        {
            throw new NotImplementedException();
        }

        void IFactVisitor.Visit(BFReproduce inFact, PlayerFactParams inParams)
        {
            throw new NotImplementedException();
        }

        void IFactVisitor.Visit(BFStateStarvation inFact, PlayerFactParams inParams)
        {
            throw new NotImplementedException();
        }

        void IFactVisitor.Visit(BFStateRange inFact, PlayerFactParams inParams)
        {
            throw new NotImplementedException();
        }

        void IFactVisitor.Visit(BFStateAge inFact, PlayerFactParams inParams)
        {
            throw new NotImplementedException();
        }

        #endregion // IFactVisitor
    }

    [LabeledEnum, Flags]
    public enum ActorTypeFlags
    {
        [Hidden]
        None = 0,

        [Label("Allow Partial Consumption")]
        AllowPartialConsumption = 0x001,

        [Label("Treat as Herd")]
        TreatAsHerd = 0x002
    }
}