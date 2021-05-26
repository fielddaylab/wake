using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Aqua;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Critter information and operations.
    /// </summary>
    public sealed class CritterProfile : IFactVisitor, IKeyValuePair<StringHash32, CritterProfile>
    {
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct EatConfig
        {
            public StringHash32 Type;
            public short Index;
            public Fraction16 Proportion;
            public float MassScale;
            public float MassScaleInv;

            static public readonly IComparer<EatConfig> Comparer = new ComparerClass();

            private class ComparerClass : IComparer<EatConfig>
            {
                public int Compare(EatConfig x, EatConfig y)
                {
                    return x.Type.CompareTo(y.Type);
                }
            }
        }

        private readonly BestiaryDesc m_Desc;
        private readonly StringHash32 m_Id;
        private readonly bool m_IsHerd;
        private readonly bool m_IgnoreStarvation;
        private uint m_MassPerPopulation;
        private uint m_PopulationCap;

        private WaterPropertyBlockF32 m_ToProducePerPopulation;
        private WaterPropertyBlockF32 m_ToProducePerPopulationStressed;
        private WaterPropertyMask m_AssignedProduceStressedMask;

        private WaterPropertyBlockF32 m_ToConsumePerPopulation;
        private WaterPropertyBlockF32 m_ToConsumePerPopulationStressed;
        private WaterPropertyMask m_AssignedConsumeStressedMask;

        private int m_EatTypeCount;
        private uint m_EatAmountTotal;
        private EatConfig[] m_EatTypes = new EatConfig[Simulator.MaxTrackedCritters];

        private int m_EatTypeStressedCount;
        private uint m_EatAmountStressedTotal;
        private EatConfig[] m_EatTypesStressed = new EatConfig[Simulator.MaxTrackedCritters];

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
            m_IgnoreStarvation = inDesc.HasFlags(BestiaryDescFlags.IgnoreStarvation);

            for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX; ++i)
            {
                m_Transitions[i] = ActorStateTransitionRange.Default;
            }
        }

        public StringHash32 Id() { return m_Id; }
        public BestiaryDesc Desc() { return m_Desc; }
        public bool IsHerd() { return m_IsHerd; }
        public uint MassPerPopulation() { return m_MassPerPopulation; }

        public ListSlice<EatConfig> EatTargets(ActorStateId inState)
        {
            switch(inState)
            {
                case ActorStateId.Alive:
                    return new ListSlice<EatConfig>(m_EatTypes, 0, m_EatTypeCount);
                case ActorStateId.Stressed:
                    return new ListSlice<EatConfig>(m_EatTypesStressed, 0, m_EatTypeStressedCount);
                default:
                    return default(ListSlice<EatConfig>);
            }
        }

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

            Array.Clear(m_EatTypes, 0, m_EatTypeCount);
            Array.Clear(m_EatTypesStressed, 0, m_EatTypeStressedCount);
            m_EatTypeCount = 0;
            m_EatTypeStressedCount = 0;
            m_EatAmountTotal = 0;
            m_EatAmountStressedTotal = 0;

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
            Array.Sort(m_EatTypes, 0, m_EatTypeCount, EatConfig.Comparer);
            Array.Sort(m_EatTypesStressed, 0, m_EatTypeStressedCount, EatConfig.Comparer);

            for(int i = 0; i < m_EatTypeCount; ++i)
            {
                ref EatConfig config = ref m_EatTypes[i];
                config.Proportion = new Fraction16(config.MassScale / m_EatAmountTotal);
                config.MassScale = config.MassScale / Simulator.HungerPerCritter;
                config.MassScaleInv = 1 / config.MassScale;
                config.Index = (short) inProfile.CritterIndex(config.Type);
            }

            for(int i = 0; i < m_EatTypeStressedCount; ++i)
            {
                ref EatConfig config = ref m_EatTypesStressed[i];
                config.Proportion = new Fraction16(config.MassScale / m_EatAmountStressedTotal);
                config.MassScale = config.MassScale / Simulator.HungerPerCritter;
                config.MassScaleInv = 1 / config.MassScale;
                config.Index = (short) inProfile.CritterIndex(config.Type);
            }
        }

        #region Apply

        public void SetupTick(ref CritterData ioData, in WaterPropertyBlockF32 inEnvironment, SimulatorFlags inFlags)
        {
            if (ioData.Population == 0)
            {
                if ((inFlags & SimulatorFlags.Debug) != 0)
                {
                    Log.Msg("[CritterProfile] Critter '{0}' is dead due to 0 population", Id());
                }
                ioData.State = ActorStateId.Dead;
                return;
            }

            ioData.Hunger = m_EatTypeCount > 0 ? Simulator.HungerPerCritter * ioData.Population : 0;

            ReevaluateState(ref ioData, inEnvironment, inFlags, WaterPropertyMask.All());
        }

        public void ReevaluateState(ref CritterData ioData, in WaterPropertyBlockF32 inEnvironment, SimulatorFlags inFlags, in WaterPropertyMask inMask)
        {
            if (ioData.Population == 0)
                return;

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
                Log.Msg("[CritterProfile] Critter '{0}' is {1}", Id(), state);
            }

            ioData.State = state;
            switch(state)
            {
                case ActorStateId.Alive:
                    {
                        ioData.ToConsume = (m_ToConsumePerPopulation * ioData.Population) & inMask;
                        break;
                    }

                case ActorStateId.Stressed:
                    {
                        ioData.ToConsume = (m_ToConsumePerPopulationStressed * ioData.Population) & inMask;
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
            if (ioData.Population == 0 || m_IgnoreStarvation)
                return;

            uint toKillAbsolute = 0;
            if (ioData.Hunger > 0)
            {
                toKillAbsolute = ioData.Hunger / Simulator.HungerPerCritter;
            }
            
            for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX; ++i)
            {
                float remainder = ioData.ToConsume[i];
                if (remainder > 0)
                {
                    toKillAbsolute = Math.Max(toKillAbsolute, (uint) (remainder / m_ToConsumePerPopulation[i]));
                }
            }

            uint toKill = CalculateMass(Simulator.FixedMultiply(ioData.Population, m_DeathPerTick) + toKillAbsolute);

            if (toKill > 0)
            {
                uint popDecrease = Die(ref ioData, toKill);
                if ((inFlags & SimulatorFlags.Debug) != 0)
                {
                    Log.Msg("[CritterProfile] {0} of critter '{1}' died", popDecrease, Id());
                }
            }

            if (m_GrowthPerTick > 0 && ioData.Population > 0)
            {
                uint popIncrease = Grow(ref ioData, m_GrowthPerTick);
                if ((inFlags & SimulatorFlags.Debug) != 0)
                {
                    Log.Msg("[CritterProfile] {0} of critter '{1}' added by reproduction", popIncrease, Id());
                }
            }

            if (m_ReproducePerTick > 0 && ioData.Population > 0)
            {
                uint popIncrease = Reproduce(ref ioData, m_ReproducePerTick);
                if ((inFlags & SimulatorFlags.Debug) != 0)
                {
                    Log.Msg("[CritterProfile] {0} of critter '{1}' added by reproduction", popIncrease, Id());
                }
            }

            if (ioData.Population == 0)
            {
                if ((inFlags & SimulatorFlags.Debug) != 0)
                {
                    Log.Msg("[CritterProfile] Critter '{0}' population hit 0", Id());
                }
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
                consumedPopulation = inMass / MassPerPopulation();
            }

            if (m_ScarcityLevel > 0)
            {
                float magicNumber = (float) ioData.Population / m_ScarcityLevel;
                if (magicNumber < 1) //Magic number to correct for populations hiding from predators and not reaching 0
                {
                    consumedPopulation = Simulator.FixedMultiply(consumedPopulation, magicNumber);
                }
            }

            uint maxPopulationLoss = Simulator.FixedMultiply(ioData.Population, Simulator.MaxEatProportion);
            if (consumedPopulation > maxPopulationLoss)
            {
                consumedPopulation = maxPopulationLoss;
            }

            consumedMass = consumedPopulation * MassPerPopulation();
            ioData.Population -= consumedPopulation;

            if (ioData.Hunger > 0 && consumedPopulation > 0)
            {
                uint hungerDecrease = Math.Min(ioData.Hunger, consumedPopulation * Simulator.HungerPerCritter);
                ioData.Hunger -= hungerDecrease;
            }

            return consumedMass;
        }

        private uint Grow(ref CritterData ioData, uint inMass)
        {
            uint populationIncrease = inMass;
            if (!IsHerd())
            {
                populationIncrease = inMass / MassPerPopulation();
            }

            uint maxIncrease = Simulator.FixedMultiply((m_PopulationCap - ioData.Population), Simulator.MaxReproduceProportion);
            if (populationIncrease > maxIncrease)
            {
                populationIncrease = maxIncrease;
            }

            ioData.Population += populationIncrease;
            return populationIncrease;
        }

        private uint Reproduce(ref CritterData ioData, float inProportion)
        {
            uint populationIncrease = Simulator.FixedMultiply(ioData.Population, inProportion);

            uint maxIncrease = Simulator.FixedMultiply((m_PopulationCap - ioData.Population), Simulator.MaxReproduceProportion);
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
                populationDecrease = inMass / MassPerPopulation();
            }

            if (populationDecrease >= ioData.Population) //Magic Nubmer to keep a species from completely dying out
            {
                populationDecrease = Simulator.FixedMultiply(ioData.Population, Simulator.MaxDeathProportion);
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

        void IFactVisitor.Visit(BFBase inFact)
        {
            // Default
        }

        void IFactVisitor.Visit(BFBody inFact)
        {
            m_MassPerPopulation = inFact.MassPerPopulation();
            m_PopulationCap = inFact.PopulationHardCap();
        }

        void IFactVisitor.Visit(BFWaterProperty inFact)
        {
            // Does not affect a critter
        }

        void IFactVisitor.Visit(BFEat inFact)
        {
            // TODO: Account for stress?
            m_EatAmountTotal += inFact.Amount();
            m_EatTypes[m_EatTypeCount++] = new EatConfig()
            {
                MassScale = inFact.Amount(),
                Type = inFact.Target().Id(),
            };
        }

        void IFactVisitor.Visit(BFGrow inFact)
        {
            // TODO: Account for stress?
            m_GrowthPerTick = inFact.Amount();
            m_ScarcityLevel = inFact.ScarcityLevel();
        }

        void IFactVisitor.Visit(BFReproduce inFact)
        {
            // TODO: Account for stress?
            m_ReproducePerTick = inFact.Amount();
            m_ScarcityLevel = inFact.ScarcityLevel();
        }

        void IFactVisitor.Visit(BFProduce inFact)
        {
            // TODO: Account for stress?
            m_ToProducePerPopulation[inFact.Target()] = inFact.Amount();
        }

        void IFactVisitor.Visit(BFConsume inFact)
        {
            // TODO: Account for stress?
            m_ToConsumePerPopulation[inFact.Target()] = inFact.Amount();
        }

        void IFactVisitor.Visit(BFState inFact)
        {
            m_Transitions[inFact.PropertyId()] = inFact.Range();
        }

        void IFactVisitor.Visit(BFDeath inFact)
        {
            // TODO: What if BFStateAge is for "Stressed"?
            m_DeathPerTick = inFact.Proportion();
        }

        void IFactVisitor.Visit(BFModel inModel)
        {
            // nothing
        }

        #endregion // IFactVisitor
    }
}