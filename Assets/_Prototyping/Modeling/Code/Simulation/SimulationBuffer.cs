using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Buffer for all simulation data.
    /// </summary>
    public class SimulationBuffer
    {
        public const float ErrorScale = 1.3f;

        private enum DirtyFlags : byte
        {
            Facts = 0x01,
            Populations = 0x02,

            ALL = Facts | Populations
        }

        private readonly SimulationProfile m_HistoricalProfile = new SimulationProfile();
        private readonly SimulationProfile m_PlayerProfile = new SimulationProfile();

        private ModelingScenarioData m_Scenario;
        private SimulationResult[] m_HistoricalResultBuffer;
        private SimulationResult[] m_PlayerResultBuffer;

        private DirtyFlags m_HistoricalSimDirty;
        private DirtyFlags m_PlayerSimDirty;

        private readonly HashSet<PlayerFactParams> m_PlayerFacts = new HashSet<PlayerFactParams>();
        private readonly RingBuffer<ActorCount> m_PlayerActors = new RingBuffer<ActorCount>(Simulator.MaxTrackedCritters);

        public SimulatorFlags Flags;

        #region Scenario

        /// <summary>
        /// Returns the current scenario.
        /// </summary>
        public ModelingScenarioData Scenario() { return m_Scenario; }

        /// <summary>
        /// Sets the current scenario.
        /// </summary>
        public bool SetScenario(ModelingScenarioData inScenarioData)
        {
            if (m_Scenario == inScenarioData)
                return false;

            m_Scenario = inScenarioData;
            Array.Resize(ref m_HistoricalResultBuffer, (int) m_Scenario.TickCount() + 1);
            Array.Resize(ref m_PlayerResultBuffer, 1 + (int) m_Scenario.TotalTicks());

            m_HistoricalProfile.Clear();
            m_PlayerProfile.Clear();

            m_PlayerFacts.Clear();
            m_PlayerActors.Clear();

            foreach(var actor in inScenarioData.Actors())
            {
                m_PlayerActors.PushBack(new ActorCount(actor.Id, 0));
            }

            m_PlayerActors.SortByKey<StringHash32, uint, ActorCount>();

            m_PlayerSimDirty = DirtyFlags.ALL;
            m_HistoricalSimDirty = DirtyFlags.ALL;
            return true;
        }

        #endregion // Scenario

        #region Player

        /// <summary>
        /// Returns the player critter count.
        /// </summary>
        public uint GetPlayerCritters(StringHash32 inId)
        {
            uint pop;
            m_PlayerActors.TryBinarySearch(inId, out pop);
            return pop;
        }

        /// <summary>
        /// Sets the player critter count.
        /// </summary>
        public bool SetPlayerCritters(StringHash32 inId, uint inPopulation)
        {
            int idx = m_PlayerActors.BinarySearch<StringHash32, uint, ActorCount>(inId);
            if (idx < 0)
            {
                m_PlayerActors.PushBack(new ActorCount(inId, inPopulation));
                m_PlayerActors.SortByKey<StringHash32, uint, ActorCount>();
                m_PlayerSimDirty |= DirtyFlags.Populations;
                return true;
            }

            ref ActorCount existing = ref m_PlayerActors[idx];
            if (existing.Population != inPopulation)
            {
                existing.Population = inPopulation;
                m_PlayerSimDirty |= DirtyFlags.Populations;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Adds a fact to the player sim.
        /// </summary>
        public bool AddFact(PlayerFactParams inFact)
        {
            if (m_PlayerFacts.Add(inFact))
            {
                m_PlayerSimDirty |= DirtyFlags.Facts;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a fact from the player sim.
        /// </summary>
        public bool RemoveFact(PlayerFactParams inFact)
        {
            if (m_PlayerFacts.Remove(inFact))
            {
                m_PlayerSimDirty |= DirtyFlags.Facts;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns if the player sim contains the given fact.
        /// </summary>
        public bool ContainsFact(PlayerFactParams inFact)
        {
            if (Services.Assets.Bestiary.IsAutoFact(inFact.FactId))
                return true;

            return m_PlayerFacts.Contains(inFact);
        }

        /// <summary>
        /// Returns all facts added to the player sim.
        /// </summary>
        public IEnumerable<PlayerFactParams> PlayerFacts()
        {
            return m_PlayerFacts;
        }

        #endregion // Player

        #region Results

        /// <summary>
        /// Returns historical data results.
        /// </summary>
        public SimulationResult[] HistoricalData()
        {
            RefreshHistorical();
            return m_HistoricalResultBuffer;
        }

        /// <summary>
        /// Returns player model results.
        /// </summary>
        public SimulationResult[] PlayerData()
        {
            RefreshModel();
            return m_PlayerResultBuffer;
        }

        /// <summary>
        /// Refreshes historical data.
        /// </summary>
        public bool RefreshHistorical()
        {
            if (m_HistoricalSimDirty == 0)
                return false;

            if ((m_HistoricalSimDirty & DirtyFlags.Facts) != 0)
                m_HistoricalProfile.Construct(m_Scenario.Environment(), m_Scenario.Critters(), m_Scenario.Facts());

            if ((m_HistoricalSimDirty & DirtyFlags.Populations) != 0)
            {
                foreach(var critter in m_Scenario.Actors())
                    m_HistoricalProfile.InitialState.SetCritters(critter.Id, critter.Population);
            }

            using(Profiling.Time("Generating historical model"))
            {
                Simulator.GenerateToBuffer(m_HistoricalProfile, m_HistoricalResultBuffer, m_Scenario.TickScale(), Flags);
            }
            m_HistoricalSimDirty = 0;
            return true;
        }
    
        /// <summary>
        /// Refreshes player data.
        /// </summary>
        public bool RefreshModel()
        {
            if (m_PlayerSimDirty == 0)
                return false;

            if ((m_PlayerSimDirty & DirtyFlags.Facts) != 0)
                m_PlayerProfile.Construct(m_Scenario.Environment(), m_Scenario.Critters(), m_PlayerFacts);

            if ((m_PlayerSimDirty & DirtyFlags.Populations) != 0)
            {
                foreach(var critter in m_PlayerActors)
                    m_PlayerProfile.InitialState.SetCritters(critter.Id, critter.Population);
            }

            using(Profiling.Time("Generating player model"))
            {
                Simulator.GenerateToBuffer(m_PlayerProfile, m_PlayerResultBuffer, m_Scenario.TickScale(), Flags);
            }
            m_PlayerSimDirty = 0;
            return true;
        }

        /// <summary>
        /// Calculates the amount of error between the player model and the historical model.
        /// </summary>
        public float CalculateModelError()
        {
            RefreshHistorical();
            RefreshModel();

            float error = 0;
            int ticksToCalc = m_HistoricalResultBuffer.Length;
            for(int i = 0; i < ticksToCalc; ++i)
            {
                error += SimulationResult.CalculateError(m_HistoricalResultBuffer[i], m_PlayerResultBuffer[i]);
            }
            return Mathf.Clamp01((error / ticksToCalc) * ErrorScale);
        }
    
        #endregion // Results
    }
}