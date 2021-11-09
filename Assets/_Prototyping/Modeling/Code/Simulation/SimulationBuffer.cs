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
            Model = 0x02,

            ALL = Historical | Model
        }

        public readonly SimulationProfile HistoricalProfile = new SimulationProfile();
        public readonly SimulationProfile PlayerProfile = new SimulationProfile();

        private ModelingScenarioData m_Scenario;
        private SimulationResult[] m_HistoricalResultBuffer;
        private SimulationResult[] m_PlayerResultBuffer;
        private SimulationResultDetails[] m_PlayerDetailBuffer;
        private SimulationResult[] m_PredictionResultBuffer;
        private SimulationResultDetails[] m_PredictionDetailBuffer;

        private DirtyFlags m_HistoricalSimDirty;
        private DirtyFlags m_PlayerSimDirty;

        private readonly HashSet<BFBase> m_PlayerFacts = new HashSet<BFBase>();
        private readonly HashSet<BestiaryDesc> m_PlayerCritters = new HashSet<BestiaryDesc>();
        private readonly RingBuffer<ActorCountU32> m_PlayerActors = new RingBuffer<ActorCountU32>(Simulator.MaxTrackedCritters);
        private readonly RingBuffer<ActorCountI32> m_PlayerActorPredictionAdjust = new RingBuffer<ActorCountI32>(Simulator.MaxTrackedCritters);
        private readonly HashSet<StringHash32> m_KnownHistoricalPopulations = new HashSet<StringHash32>();

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
            Array.Resize(ref m_PlayerDetailBuffer, (int) m_Scenario.TickCount() + 1);
            Array.Resize(ref m_PredictionResultBuffer, (int) m_Scenario.PredictionTicks() + 1);
            Array.Resize(ref m_PredictionDetailBuffer, (int) m_Scenario.PredictionTicks() + 1);

            HistoricalProfile.Clear();
            PlayerProfile.Clear();

            m_PlayerCritters.Clear();
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
                Array.Resize(ref m_PlayerDetailBuffer, (int) m_Scenario.TickCount() + 1);
                Array.Resize(ref m_PredictionResultBuffer, (int) m_Scenario.PredictionTicks() + 1);
                Array.Resize(ref m_PredictionDetailBuffer, (int) m_Scenario.PredictionTicks() + 1);

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
            foreach(var critter in m_Scenario.Actors())
            {
                if (critter.Id == inId)
                    return critter.Population;
            }

            return 0;
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
        /// Clears all player critter counts.
        /// </summary>
        public bool ClearPlayerCritters()
        {
            bool bChanged = false;
            for(int i = 0; i < m_PlayerActors.Count; i++)
            {
                bChanged |= m_PlayerActors[i].Population > 0;
                m_PlayerActors[i].Population = 0;
            }

            if (bChanged)
            {
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
        /// Clears all player critter adjusts.
        /// </summary>
        public bool ClearPlayerPredictionCritterAdjusts()
        {
            bool bChanged = false;
            for(int i = 0; i < m_PlayerActorPredictionAdjust.Count; i++)
            {
                bChanged |= m_PlayerActorPredictionAdjust[i].Population != 0;
                m_PlayerActorPredictionAdjust[i].Population = 0;
            }

            if (bChanged)
            {
                m_PlayerSimDirty |= DirtyFlags.Prediction;
                InvokeOnUpdate();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a fact to the player sim.
        /// </summary>
        public bool AddFact(BFBase inFact)
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
        public bool RemoveFact(BFBase inFact)
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
        public bool ContainsFact(BFBase inFact)
        {
            if (Services.Assets.Bestiary.IsAutoFact(inFact.Id))
                return true;

            return m_PlayerFacts.Contains(inFact);
        }

        /// <summary>
        /// Clears all player facts.
        /// </summary>
        public bool ClearFacts()
        {
            if (m_PlayerFacts.Count > 0)
            {
                m_PlayerFacts.Clear();
                m_PlayerSimDirty |= DirtyFlags.Facts;
                InvokeOnUpdate();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a critter to the player sim.
        /// </summary>
        public bool SelectCritter(BestiaryDesc inCritter)
        {
            if (m_PlayerCritters.Add(inCritter))
            {
                m_PlayerSimDirty |= DirtyFlags.Facts | DirtyFlags.Populations;
                InvokeOnUpdate();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears all selected critters.
        /// </summary>
        public bool ClearSelectedCritters()
        {
            if (m_PlayerCritters.Count > 0)
            {
                m_PlayerCritters.Clear();
                m_PlayerSimDirty |= DirtyFlags.Facts;
                InvokeOnUpdate();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns all facts added to the player sim.
        /// </summary>
        public IReadOnlyCollection<BFBase> PlayerFacts()
        {
            return m_PlayerFacts;
        }

        /// <summary>
        /// Returns all critters added to the player sim.
        /// </summary>
        public IReadOnlyCollection<BestiaryDesc> PlayerCritters()
        {
            return m_PlayerCritters;
        }

        /// <summary>
        /// Returns if the player has all historical populations.
        /// </summary>
        public bool PlayerKnowsAllHistoricalPopulations()
        {
            RefreshModel();
            return m_KnownHistoricalPopulations.Count == m_Scenario.Actors().Length;
        }

        /// <summary>
        /// Returns if the player knows the historical population for the given critter.
        /// </summary>
        public bool PlayerKnowsHistoricalPopulation(StringHash32 inCritterId)
        {
            RefreshModel();
            return m_KnownHistoricalPopulations.Contains(inCritterId);
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
        /// Returns the end state of the historical data.
        /// </summary>
        public SimulationResult HistoricalEndState()
        {
            RefreshHistorical();
            return m_HistoricalResultBuffer[m_HistoricalResultBuffer.Length - 1];
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
        /// Returns player model results details.
        /// </summary>
        public SimulationResultDetails[] PlayerDataDetails()
        {
            RefreshModel();
            return m_PlayerDetailBuffer;
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
        /// Returns player model prediction result details.
        /// </summary>
        public SimulationResultDetails[] PredictDataDetails()
        {
            RefreshModel();
            return m_PredictionDetailBuffer;
        }

        /// <summary>
        /// Returns the end state of the prediction results.
        /// </summary>
        public SimulationResult PredictEndState()
        {
            RefreshModel();
            return m_PredictionResultBuffer[m_PredictionResultBuffer.Length - 1];
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
        private bool RefreshHistorical()
        {
            if (m_HistoricalSimDirty == 0)
                return false;

            if ((m_HistoricalSimDirty & DirtyFlags.Facts) != 0)
            {
                using(Profiling.Time("Generating historical profile"))
                {
                    HistoricalProfile.Construct(m_Scenario.Environment(), m_Scenario.Critters());
                    HistoricalProfile.InitialState.Random = new SimulationRandom(m_Scenario.Seed());
                }
            }

            if ((m_HistoricalSimDirty & DirtyFlags.Populations) != 0)
            {
                using(Profiling.Time("Generating historical populations"))
                {
                    // initialize to 0, in proper order
                    HistoricalProfile.InitialState.ClearCritters();
                    foreach(var critter in HistoricalProfile.Critters())
                        HistoricalProfile.InitialState.SetCritters(critter.Id(), 0);

                    foreach(var critter in m_Scenario.Actors())
                        HistoricalProfile.InitialState.SetCritters(critter.Id, critter.Population);
                }
            }

            using(Profiling.Time("Generating historical model"))
            {
                Simulator.GenerateToBuffer(HistoricalProfile, m_HistoricalResultBuffer, Flags);
            }
            m_HistoricalSimDirty = 0;
            return true;
        }
    
        /// <summary>
        /// Refreshes player data.
        /// </summary>
        private bool RefreshModel()
        {
            if (m_PlayerSimDirty == 0)
                return false;

            bool bUpdateModel = false;
            
            if ((m_PlayerSimDirty & DirtyFlags.Facts) != 0)
            {
                using(Profiling.Time("Generating player profile"))
                {
                    PlayerProfile.Construct(m_Scenario.Environment(), m_PlayerCritters, m_PlayerFacts);
                    PlayerProfile.InitialState.Random = HistoricalProfile.InitialState.Random;
                }
                bUpdateModel = true;
            }

            if ((m_PlayerSimDirty & DirtyFlags.Populations) != 0)
            {
                using(Profiling.Time("Generating player populations"))
                {
                    // initialize to 0, in proper order
                    PlayerProfile.InitialState.ClearCritters();
                    foreach(var critter in PlayerProfile.Critters())
                        PlayerProfile.InitialState.SetCritters(critter.Id(), 0);

                    foreach(var critter in m_PlayerActors)
                        PlayerProfile.InitialState.SetCritters(critter.Id, critter.Population);
                }

                bUpdateModel = true;
            }

            if (bUpdateModel)
            {
                using(Profiling.Time("Generating player model")) 
                {
                    Simulator.GenerateToBuffer(PlayerProfile, m_PlayerResultBuffer, m_PlayerDetailBuffer, Flags);
                }
            }

            if (bUpdateModel || (m_PlayerSimDirty & DirtyFlags.Prediction) != 0)
            {
                SimulationResult initialPredictResult = m_PlayerResultBuffer[m_PlayerResultBuffer.Length - 1];
                
                using(Profiling.Time("Generating player prediction populations"))
                {
                    foreach(var critter in m_Scenario.AdjustableActors())
                        initialPredictResult.AdjustCritters(critter.Id, 0);
                    foreach(var critter in m_PlayerActorPredictionAdjust)
                        initialPredictResult.AdjustCritters(critter.Id, critter.Population);
                }
                
                using(Profiling.Time("Generating player prediction"))
                {
                    Simulator.GenerateToBuffer(PlayerProfile, initialPredictResult, m_PredictionResultBuffer, m_PredictionDetailBuffer, Flags);
                }
            }

            m_KnownHistoricalPopulations.Clear();
            var bestiaryData = Save.Bestiary;
            var actors = m_Scenario.Actors();
            var historical = m_Scenario.PopulationHistoryFacts();
            for(int i = 0; i < actors.Length; i++)
            {
                if (bestiaryData.HasFact(historical[i]))
                {
                    m_KnownHistoricalPopulations.Add(actors[i].Id);
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
            ActorCountRange target;
            for(int i = 0; i < targets.Length; ++i)
            {
                target = targets[i];
                error += GraphingUtils.RPD(predicted.GetCritters(target.Id).Population, target.Population, target.Range);
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