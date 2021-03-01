using System;
using System.Collections.Generic;
using Aqua;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    /// <summary>
    /// Buffer for all simulation data.
    /// </summary>
    public class SimulationBuffer
    {
        private readonly SimulationProfile m_HistoricalProfile = new SimulationProfile();
        private readonly SimulationProfile m_PlayerProfile = new SimulationProfile();

        private ModelingScenarioData m_Scenario;
        private SimulationResult[] m_HistoricalResultBuffer;
        private SimulationResult[] m_PlayerResultBuffer;

        private bool m_HistoricalSimDirty;
        private bool m_PlayerSimDirty;

        public SimulatorFlags Flags;

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
            Array.Resize(ref m_HistoricalResultBuffer, m_Scenario.TickCount() + 1);
            Array.Resize(ref m_PlayerResultBuffer, 1 + m_Scenario.TickCount() + m_Scenario.PredictionTicks());

            m_HistoricalProfile.Clear();
            m_PlayerProfile.Clear();

            m_PlayerSimDirty = true;
            m_HistoricalSimDirty = true;
            return true;
        }

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
            if (!m_HistoricalSimDirty)
                return false;

            m_HistoricalProfile.Clear();
            m_HistoricalProfile.Construct(m_Scenario.Environment(), m_Scenario.Facts());
            foreach(var critter in m_Scenario.Actors())
                m_HistoricalProfile.InitialState.SetCritters(critter.Id, critter.Population);

            Simulator.GenerateToBuffer(m_HistoricalProfile, m_HistoricalResultBuffer, m_Scenario.TickScale(), Flags);
            m_HistoricalSimDirty = false;
            return true;
        }
    
        /// <summary>
        /// Refreshes player data.
        /// </summary>
        public bool RefreshModel()
        {
            if (!m_PlayerSimDirty)
                return false;

            Simulator.GenerateToBuffer(m_PlayerProfile, m_PlayerResultBuffer, m_Scenario.TickScale(), Flags);
            m_PlayerSimDirty = false;
            return true;
        }
    }
}