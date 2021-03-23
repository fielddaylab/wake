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
            Prediction = 0x04,

            ALL = Facts | Populations | Prediction
        }

        /// <summary>
        /// Flags indicating which things were updated.
        /// </summary>
        public enum UpdateFlags : byte
        {
            Historical = 0x01,
            Model = 0x02
        }

        private readonly SimulationProfile m_HistoricalProfile = new SimulationProfile();
        private readonly SimulationProfile m_PlayerProfile = new SimulationProfile();

        private ModelingScenarioData m_Scenario;
        private SimulationResult[] m_HistoricalResultBuffer;
        private SimulationResult[] m_PlayerResultBuffer;
        private SimulationResult[] m_PredictionResultBuffer;

        private DirtyFlags m_HistoricalSimDirty;
        private DirtyFlags m_PlayerSimDirty;

        private readonly HashSet<PlayerFactParams> m_PlayerFacts = new HashSet<PlayerFactParams>();
        private readonly RingBuffer<ActorCountU32> m_PlayerActors = new RingBuffer<ActorCountU32>(Simulator.MaxTrackedCritters);
        private readonly RingBuffer<ActorCountI32> m_PlayerActorPredictionAdjust = new RingBuffer<ActorCountI32>(Simulator.MaxTrackedCritters);

        public SimulatorFlags Flags;
        public Action OnUpdate;

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
            Array.Resize(ref m_PlayerResultBuffer, (int) m_Scenario.TickCount() + 1);
            Array.Resize(ref m_PredictionResultBuffer, (int) m_Scenario.PredictionTicks() + 1);

            m_HistoricalProfile.Clear();
            m_PlayerProfile.Clear();

            m_PlayerFacts.Clear();
            m_PlayerActors.Clear();

            foreach(var actor in inScenarioData.Actors())
            {
                m_PlayerActors.PushBack(new ActorCountU32(actor.Id, 0));
            }

            m_PlayerActors.SortByKey<StringHash32, uint, ActorCountU32>();

            m_PlayerSimDirty = DirtyFlags.ALL;
            m_HistoricalSimDirty = DirtyFlags.ALL;
            InvokeOnUpdate();
            return true;
        }

        /// <summary>
        /// Invalidates all data and forces it to reload.
        /// </summary>
        public void ReloadScenario()
        {
            if (m_Scenario != null)
            {
                Array.Resize(ref m_HistoricalResultBuffer, (int) m_Scenario.TickCount() + 1);
                Array.Resize(ref m_PlayerResultBuffer, (int) m_Scenario.TickCount() + 1);
                Array.Resize(ref m_PredictionResultBuffer, (int) m_Scenario.PredictionTicks() + 1);

                m_PlayerSimDirty = DirtyFlags.ALL;
                m_HistoricalSimDirty = DirtyFlags.ALL;
                InvokeOnUpdate();
            }
        }

        /// <summary>
        /// Returns the amount of critters at the start of the model data.
        /// </summary>
        public uint GetModelCritters(StringHash32 inId)
        {
            return m_HistoricalProfile.InitialState.GetCritters(inId).Population;
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
            int idx = m_PlayerActors.BinarySearch<StringHash32, uint, ActorCountU32>(inId);
            if (idx < 0)
            {
                m_PlayerActors.PushBack(new ActorCountU32(inId, inPopulation));
                m_PlayerActors.SortByKey<StringHash32, uint, ActorCountU32>();
                m_PlayerSimDirty |= DirtyFlags.Populations;
                InvokeOnUpdate();
                return true;
            }

            ref ActorCountU32 existing = ref m_PlayerActors[idx];
            if (existing.Population != inPopulation)
            {
                existing.Population = inPopulation;
                m_PlayerSimDirty |= DirtyFlags.Populations;
                InvokeOnUpdate();
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Returns the player prediction critter adjust.
        /// </summary>
        public int GetPlayerPredictionCritterAdjust(StringHash32 inId)
        {
            int pop;
            m_PlayerActorPredictionAdjust.TryBinarySearch(inId, out pop);
            return pop;
        }

        /// <summary>
        /// Sets the player prediction critter count adjust.
        /// </summary>
        public bool SetPlayerPredictionCritterAdjust(StringHash32 inId, int inPopulation)
        {
            int idx = m_PlayerActorPredictionAdjust.BinarySearch<StringHash32, int, ActorCountI32>(inId);
            if (idx < 0)
            {
                m_PlayerActorPredictionAdjust.PushBack(new ActorCountI32(inId, inPopulation));
                m_PlayerActorPredictionAdjust.SortByKey<StringHash32, int, ActorCountI32>();
                m_PlayerSimDirty |= DirtyFlags.Prediction;
                InvokeOnUpdate();
                return true;
            }

            ref ActorCountI32 existing = ref m_PlayerActorPredictionAdjust[idx];
            if (existing.Population != inPopulation)
            {
                existing.Population = inPopulation;
                m_PlayerSimDirty |= DirtyFlags.Prediction;
                InvokeOnUpdate();
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
                m_PlayerSimDirty |= DirtyFlags.Facts | DirtyFlags.Populations;
                InvokeOnUpdate();
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
                InvokeOnUpdate();
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
        /// Returns player model prediction results.
        /// </summary>
        public SimulationResult[] PredictData()
        {
            RefreshModel();
            return m_PredictionResultBuffer;
        }

        /// <summary>
        /// Refreshes historical and player.
        /// </summary>
        public UpdateFlags Refresh()
        {
            UpdateFlags flags = 0;

            if (RefreshHistorical())
                flags |= UpdateFlags.Historical;
            if (RefreshModel())
                flags |= UpdateFlags.Model;

            return flags;
        }

        /// <summary>
        /// Refreshes historical data.
        /// </summary>
        public bool RefreshHistorical()
        {
            if (m_HistoricalSimDirty == 0)
                return false;

            if ((m_HistoricalSimDirty & DirtyFlags.Facts) != 0)
            {
                using(Profiling.Time("Generating historical profile"))
                {
                    m_HistoricalProfile.Construct(m_Scenario.Environment(), m_Scenario.Critters(), m_Scenario.Facts());
                }
            }

            if ((m_HistoricalSimDirty & DirtyFlags.Populations) != 0)
            {
                using(Profiling.Time("Generating historical populations"))
                {
                    // initialize to 0, in proper order
                    m_HistoricalProfile.InitialState.ClearCritters();
                    foreach(var critter in m_HistoricalProfile.Critters())
                        m_HistoricalProfile.InitialState.SetCritters(critter.Id(), 0);

                    foreach(var critter in m_Scenario.Actors())
                        m_HistoricalProfile.InitialState.SetCritters(critter.Id, critter.Population);
                }
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

            bool bUpdateModel = false;
            
            if ((m_PlayerSimDirty & DirtyFlags.Facts) != 0)
            {
                using(Profiling.Time("Generating player profile"))
                {
                    m_PlayerProfile.Construct(m_Scenario.Environment(), m_Scenario.Critters(), m_PlayerFacts);
                }
                bUpdateModel = true;
            }

            if ((m_PlayerSimDirty & DirtyFlags.Populations) != 0)
            {
                using(Profiling.Time("Generating player populations"))
                {
                    // initialize to 0, in proper order
                    m_PlayerProfile.InitialState.ClearCritters();
                    foreach(var critter in m_PlayerProfile.Critters())
                        m_PlayerProfile.InitialState.SetCritters(critter.Id(), 0);

                    foreach(var critter in m_PlayerActors)
                        m_PlayerProfile.InitialState.SetCritters(critter.Id, critter.Population);
                }

                bUpdateModel = true;
            }

            if (bUpdateModel)
            {
                using(Profiling.Time("Generating player model"))
                {
                    Simulator.GenerateToBuffer(m_PlayerProfile, m_PlayerResultBuffer, m_Scenario.TickScale(), Flags);
                }
            }

            if (bUpdateModel || (m_PlayerSimDirty & DirtyFlags.Prediction) != 0)
            {
                SimulationResult initialPredictResult = m_PlayerResultBuffer[m_PlayerResultBuffer.Length - 1];
                
                using(Profiling.Time("Generating player prediction populations"))
                {
                    foreach(var critter in m_PlayerActorPredictionAdjust)
                        initialPredictResult.AdjustCritters(critter.Id, critter.Population);
                }
                
                using(Profiling.Time("Generating player prediction"))
                {
                    Simulator.GenerateToBuffer(m_PlayerProfile, initialPredictResult, m_PredictionResultBuffer, m_Scenario.TickScale(), Flags);
                }
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
            return ticksToCalc == 0 ? 0 : Mathf.Clamp01((error / ticksToCalc) * ErrorScale);
        }

        /// <summary>
        /// Calculates the amount of error between the player prediction and the target values.
        /// </summary>
        public float CalculatePredictionError()
        {
            RefreshModel();

            float error = 0;
            SimulationResult predicted = m_PredictionResultBuffer[m_PredictionResultBuffer.Length - 1];
            
            var targets = m_Scenario.PredictionTargets();
            ActorCountU32 target;
            for(int i = 0; i < targets.Length; ++i)
            {
                target = targets[i];
                error += GraphingUtils.RPD(predicted.GetCritters(target.Id).Population, target.Population);
            }

            return targets.Length == 0 ? 0 : Mathf.Clamp01((error / targets.Length) * ErrorScale);
        }
    
        #endregion // Results
    
        private void InvokeOnUpdate()
        {
            OnUpdate?.Invoke();
        }
    }
}