using System;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class CritterData
    {
        private readonly CritterProfile m_Profile;
        private uint m_Population;

        private WaterPropertyBlockF m_Production;
        private WaterPropertyBlockF m_Consumption;

        public uint Population() { return m_Population; }
        public uint Mass() { return m_Population * m_Profile.MassPerPopulation(); }

        public CritterData(CritterProfile inProfile)
        {
            m_Profile = inProfile;
        }

        public void SetPopulation(uint inPopulation)
        {
            if (inPopulation < 0)
                m_Population = 0;
            else
                m_Population = inPopulation;
        }

        public ref WaterPropertyBlockF ToProduce
        {
            get { return ref m_Production; }
        }

        public ref WaterPropertyBlockF ToConsume
        {
            get { return ref m_Consumption; }
        }

        public uint TryEat(uint inMass)
        {
            uint consumedMass = inMass;
            uint consumedPopulation = consumedMass;
            if (!m_Profile.IsHerd())
            {
                consumedPopulation = (uint) Mathf.CeilToInt((float) inMass / m_Profile.MassPerPopulation());
                consumedMass = consumedPopulation * m_Profile.MassPerPopulation();
            }

            if (consumedPopulation > m_Population)
            {
                consumedPopulation = m_Population;
            }

            m_Population -= consumedPopulation;
            return consumedMass;
        }

        public uint Grow(uint inMass)
        {
            uint populationIncrease = inMass;
            if (!m_Profile.IsHerd())
            {
                populationIncrease = (uint) Mathf.CeilToInt((float) inMass / m_Profile.MassPerPopulation());
            }

            m_Population += populationIncrease;
            return populationIncrease;
        }

        public uint Reproduce(float inProportion)
        {
            uint populationIncrease = (uint) Mathf.RoundToInt(inProportion * m_Population);
            m_Population += populationIncrease;
            return populationIncrease;
        }
    
        public uint Die(uint inMass)
        {
            uint populationDecrease = inMass;
            if (!m_Profile.IsHerd())
            {
                populationDecrease = (uint) Mathf.CeilToInt((float) inMass / m_Profile.MassPerPopulation());
            }
            if (populationDecrease > m_Population)
            {
                populationDecrease = m_Population;
            }

            m_Population -= populationDecrease;
            return populationDecrease;
        }
    }
}