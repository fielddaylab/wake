using System;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Critter information and operations.
    /// </summary>
    public sealed class CritterProfile : IFactVisitor, IKeyValuePair<StringHash32, CritterProfile>
    {
        private readonly BestiaryDesc m_Desc;
        private readonly StringHash32 m_Id;
        private readonly bool m_IsHerd;
        private uint m_MassPerPopulation;
        private uint m_PopulationCap;

        private WaterPropertyBlockF32 m_ToProducePerPopulation;
        private WaterPropertyBlockF32 m_ToProducePerPopulationStressed;
        private WaterPropertyMask m_AssignedProduceStressedMask;

        private WaterPropertyBlockF32 m_ToConsumePerPopulation;
        private WaterPropertyBlockF32 m_ToConsumePerPopulationStressed;
        private WaterPropertyMask m_AssignedConsumeStressedMask;

        private float m_FoodPerPopulation;
        private float m_FoodPerPopulationStressed;

        private int m_EatTypeCount;
        private StringHash32[] m_EatTypes = new StringHash32[Simulator.MaxTrackedCritters];
        private int[] m_EatTypeIndices = new int[Simulator.MaxTrackedCritters];

        private WaterPropertyBlock<ActorStateTransitionRange> m_Transitions;

        private uint m_ScarcityLevel;
        private uint m_GrowthPerTick;
        private float m_ReproducePerTick;
        private float m_DeathPerTick;

        public CritterProfile(BestiaryDesc inDesc)
        {
            if (inDesc == null)
                throw new ArgumentNullException();
            
            m_Desc = inDesc;
            m_Id = inDesc.Id();
            m_IsHerd = inDesc.HasFlags(BestiaryDescFlags.TreatAsHerd);

            for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX; ++i)
            {
                m_Transitions[i] = ActorStateTransitionRange.Default;
            }
        }

        public StringHash32 Id() { return m_Id; }
        public BestiaryDesc Desc() { return m_Desc; }
        public bool IsHerd() { return m_IsHerd; }
        public uint MassPerPopulation() { return m_MassPerPopulation; }

        public ListSlice<StringHash32> EatTargets() { return new ListSlice<StringHash32>(m_EatTypes, 0, m_EatTypeCount); }
        public ListSlice<int> EatTargetIndices() { return new ListSlice<int>(m_EatTypeIndices, 0, m_EatTypeCount); }

        public void Clear()
        {
            m_MassPerPopulation = 0;
            m_PopulationCap = 0;

            m_ToProducePerPopulation = default(WaterPropertyBlockF32);
            m_ToProducePerPopulationStressed = default(WaterPropertyBlockF32);
            m_AssignedProduceStressedMask = default(WaterPropertyMask);

            m_ToConsumePerPopulation = default(WaterPropertyBlockF32);
            m_ToConsumePerPopulationStressed = default(WaterPropertyBlockF32);
            m_AssignedConsumeStressedMask = default(WaterPropertyMask);

            m_FoodPerPopulation = 0;
            m_FoodPerPopulationStressed = 0;

            Array.Clear(m_EatTypes, 0, m_EatTypeCount);
            Array.Clear(m_EatTypeIndices, 0, m_EatTypeCount);
            m_EatTypeCount = 0;

            for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX; ++i)
            {
                m_Transitions[i] = ActorStateTransitionRange.Default;
            }

            m_ScarcityLevel = 0;
            m_GrowthPerTick = 0;
            m_ReproducePerTick = 0;
            m_DeathPerTick = 0;
        }

        public void PostProcess(SimulationProfile inProfile)
        {
            for(int i = 0; i < m_EatTypeCount; ++i)
            {
                m_EatTypeIndices[i] = inProfile.CritterIndex(m_EatTypes[i]);
            }
        }

        #region Apply

        public void SetupTick(ref CritterData ioData, in WaterPropertyBlockF32 inEnvironment, SimulatorFlags inFlags)
        {
            if (ioData.Population == 0)
            {
                if ((inFlags & SimulatorFlags.Debug) != 0)
                {
                    Debug.LogFormat("[CritterProfile] Critter '{0}' is dead due to 0 population", Id().ToDebugString());
                }
                ioData.State = ActorStateId.Dead;
                return;
            }

            ActorStateId state = ActorStateId.Alive;
            ActorStateId checkedState;
            for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX && state != ActorStateId.Dead; ++i)
            {
                checkedState = m_Transitions[i].Evaluate(inEnvironment[i]);
                if (checkedState > state)
                {
                    state = checkedState;
                }
            }

            if ((inFlags & SimulatorFlags.Debug) != 0)
            {
                Debug.LogFormat("[CritterProfile] Critter '{0}' is {1}", Id().ToDebugString(), state);
            }

            ioData.State = state;
            switch(state)
            {
                case ActorStateId.Alive:
                    {
                        ioData.ToConsume = m_ToConsumePerPopulation * ioData.Population;
                        ioData.Hunger = (uint) (m_FoodPerPopulation * ioData.Population);
                        break;
                    }

                case ActorStateId.Stressed:
                    {
                        ioData.ToConsume = m_ToConsumePerPopulationStressed * ioData.Population;
                        ioData.Hunger = (uint) (m_FoodPerPopulationStressed * ioData.Population);
                        break;
                    }

                case ActorStateId.Dead:
                    {
                        ioData.ToConsume = default(WaterPropertyBlockF32);
                        ioData.Hunger = 0;
                        ioData.Population = 0;
                        break;
                    }
            }
        }

        public void EndTick(ref CritterData ioData, SimulatorFlags inFlags)
        {
            if (ioData.Population > 0)
            {
                uint toKillAbsolute = 0;
                if (ioData.Hunger > 0)
                {
                    toKillAbsolute = (uint) (ioData.Hunger / m_FoodPerPopulation);
                }
                
                for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX; ++i)
                {
                    float remainder = ioData.ToConsume[i];
                    if (remainder > 0)
                    {
                        toKillAbsolute = Math.Max(toKillAbsolute, (uint) (remainder / m_ToConsumePerPopulation[i]));
                    }
                }

                uint toKill = CalculateMass((uint) (ioData.Population * m_DeathPerTick) + toKillAbsolute);

                if (toKill > 0)
                {
                    uint popDecrease = Die(ref ioData, toKill);
                    if ((inFlags & SimulatorFlags.Debug) != 0)
                    {
                        Debug.LogFormat("[CritterProfile] {0} of critter '{1}' died", popDecrease, Id().ToDebugString());
                    }
                }

                if (m_GrowthPerTick > 0 && ioData.Population > 0)
                {
                    uint popIncrease = Grow(ref ioData, m_GrowthPerTick);
                    if ((inFlags & SimulatorFlags.Debug) != 0)
                    {
                        Debug.LogFormat("[CritterProfile] {0} of critter '{1}' added by reproduction", popIncrease, Id().ToDebugString());
                    }
                }

                if (m_ReproducePerTick > 0 && ioData.Population > 0)
                {
                    uint popIncrease = Reproduce(ref ioData, m_ReproducePerTick);
                    if ((inFlags & SimulatorFlags.Debug) != 0)
                    {
                        Debug.LogFormat("[CritterProfile] {0} of critter '{1}' added by reproduction", popIncrease, Id().ToDebugString());
                    }
                }

                if (ioData.Population == 0)
                {
                    if ((inFlags & SimulatorFlags.Debug) != 0)
                    {
                        Debug.LogFormat("[CritterProfile] Critter '{0}' population hit 0", Id().ToDebugString());
                    }
                    ioData.State = ActorStateId.Dead;
                }
            }
        }

        #endregion // Apply

        #region Operations

        public uint CalculateMass(uint inPopulation) { return inPopulation * MassPerPopulation(); }
        public uint CalculateMass(in CritterData inCritterData) { return inCritterData.Population * MassPerPopulation(); }
        public uint CalculateMass(in CritterResult inResult) { return inResult.Population * MassPerPopulation(); }

        public void CopyFrom(ref CritterData ioData, in SimulationResult inResult)
        {
            var result = inResult.GetCritters(Id());
            ioData.Population = result.Population;
            ioData.State = ActorStateId.Alive;
        }

        public void CopyTo(in CritterData inData, ref SimulationResult ioResult)
        {
            ioResult.SetCritters(Id(), inData.Population, inData.State);
        }

        public WaterPropertyBlockF32 CalculateProduction(in CritterData inCritterData)
        {
            switch(inCritterData.State)
            {
                case ActorStateId.Dead:
                default:
                    return default(WaterPropertyBlockF32);
                case ActorStateId.Alive:
                    return m_ToProducePerPopulation * inCritterData.Population;
                case ActorStateId.Stressed:
                    return m_ToProducePerPopulationStressed * inCritterData.Population;
            }
        }

        public uint TryBeEaten(ref CritterData ioData, uint inMass)
        {
            uint consumedMass = inMass;
            uint consumedPopulation = consumedMass;
            if (!IsHerd())
            {
                consumedPopulation = (uint) Mathf.CeilToInt((float) inMass / MassPerPopulation());
            }

            if (m_ScarcityLevel > 0)
            {
                float magicNumber = (float) ioData.Population / m_ScarcityLevel;
                if (magicNumber < 1) //Magic number to correct for populations hiding from predators and not reaching 0
                {
                    consumedPopulation = (uint) (consumedPopulation * magicNumber);
                }
            }

            uint maxPopulationLoss = (uint) (Simulator.MaxEatProportion * ioData.Population);
            if (consumedPopulation > maxPopulationLoss)
            {
                consumedPopulation = maxPopulationLoss;
            }

            consumedMass = consumedPopulation * MassPerPopulation();
            ioData.Population -= consumedPopulation;

            if (ioData.Hunger > 0 && consumedPopulation > 0)
            {
                float perPopulation = (ioData.State == ActorStateId.Alive ? m_FoodPerPopulation : m_FoodPerPopulationStressed);
                uint hungerDecrease = Math.Min(ioData.Hunger, (uint) (consumedPopulation * perPopulation));
                ioData.Hunger -= hungerDecrease;
            }

            return consumedMass;
        }

        private uint Grow(ref CritterData ioData, uint inMass)
        {
            uint populationIncrease = inMass;
            if (!IsHerd())
            {
                populationIncrease = (uint) Mathf.CeilToInt((float) inMass / MassPerPopulation());
            }

            uint maxIncrease = (uint) (Simulator.MaxReproduceProportion * (m_PopulationCap - ioData.Population));
            if (populationIncrease > maxIncrease)
            {
                populationIncrease = maxIncrease;
            }

            ioData.Population += populationIncrease;
            return populationIncrease;
        }

        private uint Reproduce(ref CritterData ioData, float inProportion)
        {
            uint populationIncrease = (uint) Mathf.RoundToInt(inProportion * ioData.Population);

            uint maxIncrease = (uint) (Simulator.MaxReproduceProportion * (m_PopulationCap - ioData.Population));
            if (populationIncrease > maxIncrease)
            {
                populationIncrease = maxIncrease;
            }

            ioData.Population += populationIncrease;
            return populationIncrease;
        }
    
        private uint Die(ref CritterData ioData, uint inMass)
        {
            uint populationDecrease = inMass;
            if (!IsHerd())
            {
                populationDecrease = (uint) Mathf.CeilToInt((float) inMass / MassPerPopulation());
            }

            if (populationDecrease >= ioData.Population) //Magic Nubmer to keep a species from completely dying out
            {
                populationDecrease = (uint) (ioData.Population * Simulator.MaxDeathProportion);
            }

            ioData.Population -= populationDecrease;
            return populationDecrease;
        }

        #endregion // Operations

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, CritterProfile>.Key { get { return m_Desc.Id(); } }

        CritterProfile IKeyValuePair<StringHash32, CritterProfile>.Value { get { return this; } }

        #endregion // KeyValue

        #region IFactVisitor

        void IFactVisitor.Visit(BFBase inFact, PlayerFactParams inParams)
        {
            // Default
        }

        void IFactVisitor.Visit(BFBody inFact, PlayerFactParams inParams)
        {
            m_MassPerPopulation = inFact.MassPerPopulation();
            m_PopulationCap = inFact.PopulationHardCap();
        }

        void IFactVisitor.Visit(BFWaterProperty inFact, PlayerFactParams inParams)
        {
            // Does not affect a critter
        }

        void IFactVisitor.Visit(BFEat inFact, PlayerFactParams inParams)
        {
            // TODO: Account for stress?
            m_FoodPerPopulation += inFact.Amount();
            m_EatTypes[m_EatTypeCount++] = inFact.Target().Id();
        }

        void IFactVisitor.Visit(BFGrow inFact, PlayerFactParams inParams)
        {
            // TODO: Account for stress?
            m_GrowthPerTick = inFact.Amount();
            m_ScarcityLevel = inFact.ScarcityLevel();
        }

        void IFactVisitor.Visit(BFReproduce inFact, PlayerFactParams inParams)
        {
            // TODO: Account for stress?
            m_ReproducePerTick = inFact.Amount();
            m_ScarcityLevel = inFact.ScarcityLevel();
        }

        void IFactVisitor.Visit(BFStateStarvation inFact, PlayerFactParams inParams)
        {
            // TODO: Eliminate starvation?
        }

        void IFactVisitor.Visit(BFStateRange inFact, PlayerFactParams inParams)
        {
            var transition = m_Transitions[inFact.PropertyId()];
            transition.Encompass(inFact);
            m_Transitions[inFact.PropertyId()] = transition;
        }

        void IFactVisitor.Visit(BFStateAge inFact, PlayerFactParams inParams)
        {
            // TODO: What if BFStateAge is for "Stressed"?
            m_DeathPerTick = inFact.Proportion();
        }

        #endregion // IFactVisitor
    }
}