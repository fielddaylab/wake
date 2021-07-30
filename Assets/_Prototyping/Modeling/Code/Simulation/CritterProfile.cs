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
        private const uint NotAssignedU32 = uint.MaxValue;
        private const float NotAssignedF32 = -1;

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

        private WaterPropertyBlockF32 m_ToConsumePerPopulation;
        private WaterPropertyBlockF32 m_ToConsumePerPopulationStressed;

        private int m_EatTypeCount;
        private uint m_EatAmountTotal;
        private EatConfig[] m_EatTypes = new EatConfig[Simulator.MaxTrackedCritters];

        private int m_EatTypeStressedCount;
        private uint m_EatAmountStressedTotal;
        private EatConfig[] m_EatTypesStressed = new EatConfig[Simulator.MaxTrackedCritters];

        private ActorStateTransitionSet m_Transitions;

        private uint m_ScarcityLevel;

        private uint m_GrowthPerTick;
        private uint m_GrowthPerTickStressed;

        private float m_ReproducePerTick;
        private float m_ReproducePerTickStressed;

        private float m_DeathPerTick;
        private float m_DeathPerTickStressed;

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

        #region Generation

        public void Clear()
        {
            m_MassPerPopulation = 0;
            m_PopulationCap = 0;

            m_ToProducePerPopulation = default(WaterPropertyBlockF32);
            m_ToProducePerPopulationStressed = default(WaterPropertyBlockF32);

            m_ToConsumePerPopulation = default(WaterPropertyBlockF32);
            m_ToConsumePerPopulationStressed = default(WaterPropertyBlockF32);

            Array.Clear(m_EatTypes, 0, m_EatTypeCount);
            Array.Clear(m_EatTypesStressed, 0, m_EatTypeStressedCount);
            m_EatTypeCount = 0;
            m_EatTypeStressedCount = 0;
            m_EatAmountTotal = 0;
            m_EatAmountStressedTotal = 0;

            for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX; i++)
            {
                m_Transitions[i] = ActorStateTransitionRange.Default;
                m_ToProducePerPopulation[i] = m_ToProducePerPopulationStressed[i] = NotAssignedF32;
                m_ToConsumePerPopulation[i] = m_ToConsumePerPopulationStressed[i] = NotAssignedF32;
            }

            m_ScarcityLevel = 0;

            m_GrowthPerTick = NotAssignedU32;
            m_GrowthPerTickStressed = NotAssignedU32;

            m_ReproducePerTick = NotAssignedF32;
            m_ReproducePerTickStressed = NotAssignedF32;

            m_DeathPerTick = NotAssignedF32;
            m_DeathPerTickStressed = NotAssignedF32;
        }

        public void PostProcess(SimulationProfile inProfile)
        {
            for(WaterPropertyId i = 0; i <= WaterPropertyId.TRACKED_MAX; i++)
            {
                float produceA = m_ToProducePerPopulation[i];
                float produceS = m_ToProducePerPopulationStressed[i];
                PostProcessStressValues(ref produceA, ref produceS);
                m_ToProducePerPopulation[i] = produceA;
                m_ToProducePerPopulationStressed[i] = produceS;

                float consumeA = m_ToConsumePerPopulation[i];
                float consumeS = m_ToConsumePerPopulationStressed[i];
                PostProcessStressValues(ref consumeA, ref consumeS);
                m_ToConsumePerPopulation[i] = consumeA;
                m_ToConsumePerPopulationStressed[i] = consumeS;
            }

            PostProcessStressValues(ref m_DeathPerTick, ref m_DeathPerTickStressed);
            PostProcessStressValues(ref m_GrowthPerTick, ref m_GrowthPerTickStressed);
            PostProcessStressValues(ref m_ReproducePerTick, ref m_ReproducePerTickStressed);

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

        static private void PostProcessStressValues(ref float ioAlive, ref float ioStressed)
        {
            if (ioAlive == NotAssignedF32)
                ioAlive = 0;
            if (ioStressed == NotAssignedF32)
                ioStressed = ioAlive;
        }

        static private void PostProcessStressValues(ref uint ioAlive, ref uint ioStressed)
        {
            if (ioAlive == NotAssignedU32)
                ioAlive = 0;
            if (ioStressed == NotAssignedU32)
                ioStressed = ioAlive;
        }

        private ref EatConfig FindEatConfig(StringHash32 inTargetId, EatConfig[] ioConfigs, ref int ioCount, out bool outbNew)
        {
            for(int i = 0; i < ioCount; i++)
            {
                if (ioConfigs[i].Type == inTargetId)
                {
                    outbNew = false;
                    return ref ioConfigs[i];
                }
            }

            ref EatConfig config = ref ioConfigs[ioCount++];
            config.Type = inTargetId;
            outbNew = true;
            return ref config;
        }

        #endregion // Generation

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
                        ioData.Hunger = m_EatAmountTotal > 0 ? Simulator.HungerPerCritter * ioData.Population : 0;
                        break;
                    }

                case ActorStateId.Stressed:
                    {
                        ioData.ToConsume = (m_ToConsumePerPopulationStressed * ioData.Population) & inMask;
                        ioData.Hunger = m_EatAmountStressedTotal > 0 ? Simulator.HungerPerCritter * ioData.Population : 0;
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

        public void EndTick(ref CritterData ioData, SimulatorFlags inFlags, ref SimulationResultDetails ioDetails)
        {
            bool bDetails = (inFlags & SimulatorFlags.OutputDetails) != 0;

            if (ioData.Population == 0 || m_IgnoreStarvation)
            {
                if (bDetails)
                {
                    ioDetails.Deaths.Add(0);
                    ioDetails.Growth.Add(0);
                }
                return;
            }

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
                    float desired = ioData.State == ActorStateId.Stressed ? m_ToConsumePerPopulationStressed[i] : m_ToConsumePerPopulation[i];
                    toKillAbsolute = Math.Max(toKillAbsolute, (uint) (remainder / desired));
                }
            }

            float deathPerTick = ioData.State == ActorStateId.Stressed ? m_DeathPerTickStressed : m_DeathPerTick;
            uint toKill = CalculateMass(Simulator.FixedMultiply(ioData.Population, deathPerTick) + toKillAbsolute);

            if (toKill > 0)
            {
                uint popDecrease = Die(ref ioData, toKill);
                if ((inFlags & SimulatorFlags.Debug) != 0)
                {
                    Log.Msg("[CritterProfile] {0} of critter '{1}' died", popDecrease, Id());
                }
                if (bDetails)
                {
                    ioDetails.Deaths.Add(popDecrease);
                }
            }
            else
            {
                if (bDetails)
                {
                    ioDetails.Deaths.Add(0);
                }
            }

            uint totalGrowth = 0;

            uint growthPerTick = ioData.State == ActorStateId.Stressed ? m_GrowthPerTickStressed : m_GrowthPerTick;
            if (growthPerTick > 0 && ioData.Population > 0)
            {
                uint popIncrease = Grow(ref ioData, growthPerTick);
                totalGrowth += popIncrease;

                if ((inFlags & SimulatorFlags.Debug) != 0)
                {
                    Log.Msg("[CritterProfile] {0} of critter '{1}' added by reproduction", popIncrease, Id());
                }
            }

            float reproPerTick = ioData.State == ActorStateId.Stressed ? m_ReproducePerTickStressed : m_ReproducePerTick;
            if (reproPerTick > 0 && ioData.Population > 0)
            {
                uint popIncrease = Reproduce(ref ioData, reproPerTick);
                totalGrowth += popIncrease;

                if ((inFlags & SimulatorFlags.Debug) != 0)
                {
                    Log.Msg("[CritterProfile] {0} of critter '{1}' added by reproduction", popIncrease, Id());
                }
            }

            if (bDetails)
            {
                ioDetails.Growth.Add(totalGrowth);
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
            m_ScarcityLevel = inFact.ScarcityLevel();
            m_PopulationCap = inFact.PopulationHardCap();
        }

        void IFactVisitor.Visit(BFWaterProperty inFact)
        {
            // Nothing
        }

        void IFactVisitor.Visit(BFPopulation inFact)
        {
            // Nothing
        }

        void IFactVisitor.Visit(BFPopulationHistory inFact)
        {
            // Nothing
        }

        void IFactVisitor.Visit(BFEat inFact)
        {
            if (inFact.OnlyWhenStressed())
            {
                ref EatConfig config = ref FindEatConfig(inFact.Target().Id(), m_EatTypesStressed, ref m_EatTypeStressedCount, out bool discard);
                long change = (long) inFact.Amount() - (long) config.MassScale;
                m_EatAmountStressedTotal = (uint) (m_EatAmountStressedTotal + change);
                config.MassScale = inFact.Amount();
            }
            else
            {
                ref EatConfig mainConfig = ref FindEatConfig(inFact.Target().Id(), m_EatTypes, ref m_EatTypeCount, out bool discard);
                long mainChange = (long) inFact.Amount() - (long) mainConfig.MassScale;
                m_EatAmountTotal = (uint) (m_EatAmountTotal + mainChange);
                mainConfig.MassScale = inFact.Amount();

                ref EatConfig stressConfig = ref FindEatConfig(inFact.Target().Id(), m_EatTypesStressed, ref m_EatTypeStressedCount, out bool bNewStress);
                if (bNewStress)
                {
                    long stressChange = (long) inFact.Amount() - (long) stressConfig.MassScale;
                    m_EatAmountStressedTotal = (uint) (m_EatAmountStressedTotal + stressChange);
                    stressConfig.MassScale = inFact.Amount();
                }
            }
        }

        void IFactVisitor.Visit(BFGrow inFact)
        {
            if (inFact.OnlyWhenStressed())
                m_GrowthPerTickStressed = inFact.Amount();
            else
                m_GrowthPerTick = inFact.Amount();
        }

        void IFactVisitor.Visit(BFReproduce inFact)
        {
            if (inFact.OnlyWhenStressed())
                m_ReproducePerTickStressed = inFact.Amount();
            else
                m_ReproducePerTick = inFact.Amount();
        }

        void IFactVisitor.Visit(BFProduce inFact)
        {
            if (inFact.OnlyWhenStressed())
                m_ToProducePerPopulationStressed[inFact.Target()] = inFact.Amount();
            else
                m_ToProducePerPopulation[inFact.Target()] = inFact.Amount();
        }

        void IFactVisitor.Visit(BFConsume inFact)
        {
            if (inFact.OnlyWhenStressed())
                m_ToConsumePerPopulationStressed[inFact.Target()] = inFact.Amount();
            else
                m_ToConsumePerPopulation[inFact.Target()] = inFact.Amount();
        }

        void IFactVisitor.Visit(BFState inFact)
        {
            m_Transitions[inFact.PropertyId()] = inFact.Range();
        }

        void IFactVisitor.Visit(BFDeath inFact)
        {
            if (inFact.OnlyWhenStressed())
                m_DeathPerTickStressed = inFact.Proportion();
            else
                m_DeathPerTick = inFact.Proportion();
        }

        void IFactVisitor.Visit(BFModel inModel)
        {
            // nothing
        }

        #endregion // IFactVisitor
    }
}