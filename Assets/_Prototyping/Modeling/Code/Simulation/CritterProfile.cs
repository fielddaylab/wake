using System;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Critter information and operations.
    /// </summary>
    public class CritterProfile : IFactVisitor, IKeyValuePair<StringHash32, CritterProfile>
    {
        private readonly BestiaryDesc m_Desc;
        private BFBody m_Body;
        private bool m_IsHerd;

        private WaterPropertyBlockF32 m_ToProducePerPopulation;
        private WaterPropertyBlockF32 m_ToProducePerPopulationStressed;
        private WaterPropertyMask m_AssignedProduceStressedMask;

        private WaterPropertyBlockF32 m_ToConsumePerPopulation;
        private WaterPropertyBlockF32 m_ToConsumePerPopulationStressed;
        private WaterPropertyMask m_AssignedConsumeStressedMask;

        private int m_EatTypeCount;
        private StringHash32[] m_EatTypes = new StringHash32[Simulator.MaxTrackedCritters];
        private int[] m_EatTypeIndices = new int[Simulator.MaxTrackedCritters];

        private WaterPropertyBlock<ActorStateTransitionRange> m_Transitions;

        private uint m_GrowthPerTick;
        private float m_ReproducePerTick;
        private float m_DeathPerTick;

        public CritterProfile(BestiaryDesc inDesc)
        {
            if (inDesc == null)
                throw new ArgumentNullException();
            
            m_Desc = inDesc;
            m_IsHerd = inDesc.HasFlags(BestiaryDescFlags.TreatAsHerd);

            for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX; ++i)
            {
                m_Transitions[i] = ActorStateTransitionRange.Default;
            }
        }

        public StringHash32 Id() { return m_Desc.Id(); }
        public BestiaryDesc Desc() { return m_Desc; }
        public bool IsHerd() { return m_IsHerd; }
        public uint MassPerPopulation() { return m_Body.StartingMass(); }

        public ListSlice<StringHash32> EatTargets() { return new ListSlice<StringHash32>(m_EatTypes, 0, m_EatTypeCount); }
        public ListSlice<int> EatTargetIndices() { return new ListSlice<int>(m_EatTypeIndices, 0, m_EatTypeCount); }

        public void Clear()
        {
            m_Body = null;

            m_ToProducePerPopulation = default(WaterPropertyBlockF32);
            m_ToProducePerPopulationStressed = default(WaterPropertyBlockF32);
            m_AssignedProduceStressedMask = default(WaterPropertyMask);

            m_ToConsumePerPopulation = default(WaterPropertyBlockF32);
            m_ToConsumePerPopulationStressed = default(WaterPropertyBlockF32);
            m_AssignedConsumeStressedMask = default(WaterPropertyMask);

            Array.Clear(m_EatTypes, 0, m_EatTypeCount);
            Array.Clear(m_EatTypeIndices, 0, m_EatTypeCount);
            m_EatTypeCount = 0;

            for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX; ++i)
            {
                m_Transitions[i] = ActorStateTransitionRange.Default;
            }

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

        public void SetupTick(ref CritterData ioData, in WaterPropertyBlockF32 inEnvironment)
        {
            if (ioData.Population == 0)
            {
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

            ioData.State = state;
            switch(state)
            {
                case ActorStateId.Alive:
                    {
                        ioData.ToProduce = m_ToProducePerPopulation * ioData.Population;
                        ioData.ToConsume = m_ToConsumePerPopulation * ioData.Population;
                        break;
                    }

                case ActorStateId.Stressed:
                    {
                        ioData.ToProduce = m_ToProducePerPopulationStressed * ioData.Population;
                        ioData.ToConsume = m_ToConsumePerPopulationStressed * ioData.Population;
                        break;
                    }

                case ActorStateId.Dead:
                    {
                        ioData.ToProduce = default(WaterPropertyBlockF32);
                        ioData.ToConsume = default(WaterPropertyBlockF32);
                        ioData.Population = 0;
                        break;
                    }
            }
        }

        public void EndTick(ref CritterData ioData)
        {
            uint toKill = 0;

            for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX; ++i)
            {
                float remainder = ioData.ToConsume[i];
                if (remainder > 0)
                {
                    float proportion = remainder / m_ToConsumePerPopulation[i];
                    toKill = Math.Max(toKill, (uint) (proportion * CalculateMass(ioData)));
                }
            }

            toKill += (uint) (CalculateMass(ioData) * m_DeathPerTick);

            if (toKill > 0)
            {
                Die(ref ioData, toKill);
            }

            if (m_GrowthPerTick > 0 && ioData.Population > 0)
            {
                Grow(ref ioData, m_GrowthPerTick);
            }

            if (m_ReproducePerTick > 0 && ioData.Population > 0)
            {
                Reproduce(ref ioData, m_ReproducePerTick);
            }

            if (ioData.Population == 0)
            {
                ioData.State = ActorStateId.Dead;
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
            ioData.State = result.State;
        }

        public void CopyTo(in CritterData inData, ref SimulationResult ioResult)
        {
            ioResult.SetCritters(Id(), inData.Population, inData.State);
        }

        public uint TryEat(ref CritterData ioData, uint inMass)
        {
            uint consumedMass = inMass;
            uint consumedPopulation = consumedMass;
            if (!IsHerd())
            {
                consumedPopulation = (uint) Mathf.CeilToInt((float) inMass / MassPerPopulation());
            }

            if (consumedPopulation > ioData.Population)
            {
                consumedPopulation = ioData.Population;
            }

            consumedMass = consumedPopulation * MassPerPopulation();
            ioData.Population -= consumedPopulation;

            if (!ioData.AttemptedEat && consumedPopulation > 0)
            {
                float perPopulation = (ioData.State == ActorStateId.Alive ? m_ToConsumePerPopulation : m_ToProducePerPopulation)[WaterPropertyId.Food];
                ioData.ToConsume[WaterPropertyId.Food] -= consumedPopulation * perPopulation;
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

            ioData.Population += populationIncrease;
            return populationIncrease;
        }

        private uint Reproduce(ref CritterData ioData, float inProportion)
        {
            uint populationIncrease = (uint) Mathf.RoundToInt(inProportion * ioData.Population);
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
            if (populationDecrease > ioData.Population)
            {
                populationDecrease = ioData.Population;
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
            m_Body = inFact;
        }

        void IFactVisitor.Visit(BFWaterProperty inFact, PlayerFactParams inParams)
        {
            // Does not affect a critter
        }

        void IFactVisitor.Visit(BFEat inFact, PlayerFactParams inParams)
        {
            // TODO: Account for stress?
            m_ToConsumePerPopulation.Food += inFact.Amount();
            m_EatTypes[m_EatTypeCount++] = inFact.Target().Id();
        }

        void IFactVisitor.Visit(BFGrow inFact, PlayerFactParams inParams)
        {
            // TODO: Account for stress?
            m_GrowthPerTick = inFact.Amount();
        }

        void IFactVisitor.Visit(BFReproduce inFact, PlayerFactParams inParams)
        {
            // TODO: Account for stress?
            m_ReproducePerTick = inFact.Amount();
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