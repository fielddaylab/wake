using System;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class CritterProfile : IFactVisitor
    {
        private readonly BestiaryDesc m_Desc;
        private BFBody m_Body;
        private bool m_IsHerd;

        private WaterPropertyBlockF m_ToProducePerPopulation;
        private WaterPropertyBlockF m_ToConsumePerPopulation;

        private uint m_GrowthPerTick;
        private float m_ReproducePerTick;

        public CritterProfile(BestiaryDesc inDesc)
        {
            m_Desc = inDesc;
            m_IsHerd = inDesc.HasFlags(BestiaryDescFlags.TreatAsHerd);
        }

        public bool IsHerd() { return m_IsHerd; }
        public uint MassPerPopulation()
        {
            return m_Body.StartingMass();
        }

        public void Clear()
        {
            m_Body = null;
            m_ToProducePerPopulation = default(WaterPropertyBlockF);
            m_ToConsumePerPopulation = default(WaterPropertyBlockF);
            m_GrowthPerTick = 0;
            m_ReproducePerTick = 0;
        }

        #region Apply

        public void SetupTick(CritterData ioData, in WaterPropertyBlockF inEnvironment)
        {
            ioData.ToProduce = m_ToProducePerPopulation * ioData.Population();
            ioData.ToConsume = m_ToConsumePerPopulation * ioData.Population();

            // TODO: Calculate state by environment, etc
        }

        public void EndTick(CritterData ioData)
        {
            uint toKill = 0;

            for(WaterPropertyId i = 0; i < WaterPropertyId.TRACKED_MAX; ++i)
            {
                float proportion = ioData.ToConsume[i] / m_ToConsumePerPopulation[i];
                toKill = Math.Max(toKill, (uint) proportion * ioData.Mass());
            }

            if (toKill > 0)
            {
                ioData.Die(toKill);
            }

            if (m_GrowthPerTick > 0 && ioData.Population() > 0)
            {
                ioData.Grow(m_GrowthPerTick);
            }

            if (m_ReproducePerTick > 0 && ioData.Population() > 0)
            {
                ioData.Reproduce(m_ReproducePerTick);
            }
        }

        #endregion // Apply

        #region IFactVisitor

        void IFactVisitor.Visit(BFBase inFact, PlayerFactParams inParams)
        {
        }

        void IFactVisitor.Visit(BFBody inFact, PlayerFactParams inParams)
        {
            m_Body = inFact;
        }

        void IFactVisitor.Visit(BFWaterProperty inFact, PlayerFactParams inParams)
        {
        }

        void IFactVisitor.Visit(BFEat inFact, PlayerFactParams inParams)
        {
            m_ToConsumePerPopulation.Food += inFact.Amount();
        }

        void IFactVisitor.Visit(BFGrow inFact, PlayerFactParams inParams)
        {
            m_GrowthPerTick = inFact.Amount();
        }

        void IFactVisitor.Visit(BFReproduce inFact, PlayerFactParams inParams)
        {
            m_ReproducePerTick = inFact.Amount();
        }

        void IFactVisitor.Visit(BFStateStarvation inFact, PlayerFactParams inParams)
        {
        }

        void IFactVisitor.Visit(BFStateRange inFact, PlayerFactParams inParams)
        {
        }

        void IFactVisitor.Visit(BFStateAge inFact, PlayerFactParams inParams)
        {
        }

        #endregion // IFactVisitor
    }
}