using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using UnityEngine.UI;
using BeauUtil.Debugger;
using BeauUtil;

namespace Aqua.Modeling {
    public unsafe class SimulationDataCtrl : MonoBehaviour {

        private const int HistoricalArenaSize = (int) (1024 * 5.5f);
        private const int PlayerArenaSize = (int) (1024 * 9f);
        private const int TotalArenaSize = HistoricalArenaSize + PlayerArenaSize;

        private enum SectionType {
            Historical,
            Player,
            Predict
        }

        private enum DataReadyFlags : byte {
            Profile = 0x01,
            Data = 0x02,

            Full = Profile | Data
        }

        private struct ResultWrapper {
            public SimSnapshot* Ptr;

            public ResultWrapper(Unsafe.ArenaHandle allocator, int additionalTicks) {
                Ptr = Unsafe.AllocArray<SimSnapshot>(allocator, Simulation.MaxTicks + additionalTicks);
            }

            public ResultWrapper(SimSnapshot* output) {
                Ptr = output;
            }
        }

        public class InterventionData {
            public BestiaryDesc Target;
            public int Amount;

            public readonly HashSet<BestiaryDesc> AdditionalEntities = new HashSet<BestiaryDesc>();
            public readonly HashSet<BFBase> AdditionalFacts = new HashSet<BFBase>();
        }

        [SerializeField] private float m_ErrorScale = 2;
        
        private Unsafe.ArenaHandle m_Allocator;

        private SimProfile m_HistoricalProfile;
        private Simulation.Buffer m_HistoricalBuffer;
        private ResultWrapper m_HistoricalOutput;
        private DataReadyFlags m_HistoricalReady;
        
        private SimProfile m_PlayerProfile;
        private Simulation.Buffer m_PlayerBuffer;
        private ResultWrapper m_PlayerOutput; // 0 - syncTicks
        private DataReadyFlags m_PlayerReady;

        private SimProfile m_PredictProfile;
        private ResultWrapper m_PredictOutputSlice; // syncTicks + 1 -> MaxTicks
        private DataReadyFlags m_PredictReady;

        private ModelState m_State;
        private ModelProgressInfo m_ProgressInfo;
        private AsyncHandle m_HistoricalProfileTask;
        private AsyncHandle m_HistoricalDataTask;
        private AsyncHandle m_PlayerProfileTask;
        private AsyncHandle m_PlayerDataTask;
        private AsyncHandle m_PredictProfileTask;
        private AsyncHandle m_PredictDataTask;

        private readonly HashSet<BestiaryDesc> m_RelevantCritters = new HashSet<BestiaryDesc>();
        private readonly HashSet<StringHash32> m_RelevantCritterIds = new HashSet<StringHash32>();
        private WaterPropertyMask m_RelevantWaterProperties = default;
        private readonly HashSet<StringHash32> m_CrittersWithHistoricalData = new HashSet<StringHash32>();
        private WaterPropertyMask m_WaterPropertiesWithHistoricalData = default;

        public readonly InterventionData Intervention = new InterventionData();

        public Action OnInterventionUpdated;

        public void SetData(ModelState state, ModelProgressInfo info) {
            m_State = state;
            m_ProgressInfo = info;
        }

        #region Unity Events

        private SimulationDataCtrl() {
            HasHistoricalPopulation = (StringHash32 id) => {
                return m_CrittersWithHistoricalData.Contains(id);
            };
            ShouldGraphHistorical = (StringHash32 id) => {
                return m_RelevantCritterIds.Contains(id) && HasHistoricalPopulation(id);
            };
            ShouldGraphWaterProperty = (WaterPropertyId id) => {
                return m_RelevantWaterProperties[id] && m_WaterPropertiesWithHistoricalData[id];
            };
        }

        private void Awake() {
            Log.Msg("profile size = {0}, buffer size = {1}, snapshot size = {2}", SimProfile.BufferSize, Simulation.Buffer.BufferSize, sizeof(SimSnapshot));

            m_Allocator = Unsafe.CreateArena(TotalArenaSize, "sim");

            m_HistoricalProfile = new SimProfile(m_Allocator);
            m_HistoricalBuffer = new Simulation.Buffer(m_Allocator);
            m_HistoricalOutput = new ResultWrapper(m_Allocator, 0);

            m_PlayerProfile = new SimProfile(m_Allocator);
            m_PlayerBuffer = new Simulation.Buffer(m_Allocator);
            m_PlayerOutput = new ResultWrapper(m_Allocator, 2);
            m_PredictProfile = new SimProfile(m_Allocator);

            Log.Msg("Simulation arena spare bytes = {0} / {1}", Unsafe.ArenaFreeBytes(m_Allocator), Unsafe.ArenaSize(m_Allocator));
        }

        private void OnDestroy() {
            Unsafe.TryFreeArena(ref m_Allocator);

            m_HistoricalProfile.Dispose();
            m_PlayerProfile.Dispose();
            m_HistoricalBuffer.Dispose();
            m_PlayerBuffer.Dispose();

            m_HistoricalDataTask.Cancel();
            m_HistoricalProfileTask.Cancel();
            m_PlayerDataTask.Cancel();
            m_PlayerProfileTask.Cancel();
            m_PredictDataTask.Cancel();
            m_PredictProfileTask.Cancel();
        }

        #endregion // Unity Events

        public void ClearSite() {
            ClearHistorical();
            ClearPlayer();
            m_PredictOutputSlice = default;
        }

        public void LoadSite() {
            if (m_ProgressInfo.Sim) {
                m_PredictOutputSlice = new ResultWrapper(m_PlayerOutput.Ptr + m_ProgressInfo.Sim.SyncTickCount + 1);
            } else {
                m_PredictOutputSlice = default;
            }

            m_RelevantCritters.Clear();
            m_RelevantCritterIds.Clear();
            m_RelevantWaterProperties.Mask = 0;

            if (m_ProgressInfo.Scope != null) {
                foreach(var organismId in m_ProgressInfo.Scope.OrganismIds) {
                    m_RelevantCritters.Add(Assets.Bestiary(organismId));
                    m_RelevantCritterIds.Add(organismId);
                }
                if (m_ProgressInfo.Scope.IncludeWaterChemistryInAccuracy) {
                    m_RelevantWaterProperties = Save.Inventory.GetPropertyUnlockedMask();
                }
            } else {
                foreach(var organism in m_ProgressInfo.ImportableEntities) {
                    if (organism.Category() == BestiaryDescCategory.Critter) {
                        m_RelevantCritters.Add(organism);
                        m_RelevantCritterIds.Add(organism.Id());
                    }
                }
                m_RelevantWaterProperties = Save.Inventory.GetPropertyUnlockedMask();
            }

            GenerateHistorical();
            GeneratePlayerProfile();
        }

        public void LoadConceptualModel() {
            m_CrittersWithHistoricalData.Clear();
            m_WaterPropertiesWithHistoricalData.Mask = 0;

            foreach(var entity in m_State.Conceptual.GraphedEntities) {
                if (entity.Category() == BestiaryDescCategory.Environment) {
                    continue;
                }

                StringHash32 id = entity.Id();
                BFPopulationHistory popHistory = BestiaryUtils.FindPopulationHistoryRule(m_State.Environment, id);
                if (m_State.Conceptual.GraphedFacts.Contains(popHistory)) {
                    m_CrittersWithHistoricalData.Add(id);
                }
            }

            for(WaterPropertyId id = 0; id < WaterPropertyId.TRACKED_COUNT; id++) {
                BFWaterPropertyHistory waterHistory = BestiaryUtils.FindWaterPropertyHistoryRule(m_State.Environment, id);
                if (m_State.Conceptual.GraphedFacts.Contains(waterHistory)) {
                    m_WaterPropertiesWithHistoricalData[id] = true;
                }
            }
        }

        public void ClearSimulatedData() {
            m_PlayerReady &= ~DataReadyFlags.Data;
            m_HistoricalReady &= ~DataReadyFlags.Data;
            m_PredictReady = 0;
            m_PredictProfile.Clear();

            m_HistoricalDataTask.Cancel();
            m_PlayerDataTask.Cancel();
            m_PredictDataTask.Cancel();
            m_PredictProfileTask.Cancel();
        }

        public bool IsExecutingRequests() {
            return m_HistoricalDataTask.IsRunning() || m_HistoricalProfileTask.IsRunning()
                || m_PlayerDataTask.IsRunning() || m_PlayerProfileTask.IsRunning()
                || m_PredictProfileTask.IsRunning() || m_PredictDataTask.IsRunning();
        }

        private void GenerateSimulatedSubset() {
            m_State.Conceptual.SimulatedEntities.Clear();
            m_State.Conceptual.SimulatedFacts.Clear();
            FactUtil.GatherSimulatedSubset(m_RelevantCritters, m_State.Conceptual.GraphedEntities, m_State.Conceptual.GraphedFacts, m_State.Conceptual.SimulatedEntities, m_State.Conceptual.SimulatedFacts);
        }

        #region Historical Data

        public SimProfile HistoricalProfile { get { return m_HistoricalProfile; } }
        
        public bool IsHistoricalProfileReady() {
            return (m_HistoricalReady & DataReadyFlags.Profile) != 0;
        }

        public bool IsHistoricalDataReady() {
            return (m_HistoricalReady & DataReadyFlags.Data) != 0;
        }

        public bool IsHistoricalReady() {
            return m_HistoricalReady == DataReadyFlags.Full;
        }

        public SimSnapshot* RetrieveHistoricalData(out uint totalTicks) {
            if (!IsHistoricalDataReady()) {
                totalTicks = 0;
                return null;
            }

            totalTicks = 1 + m_ProgressInfo.Sim.SyncTickCount;
            return m_HistoricalOutput.Ptr;
        }

        /// <summary>
        /// Clears all historical data.
        /// </summary>
        public void ClearHistorical() {
            m_HistoricalProfileTask.Cancel();
            m_HistoricalDataTask.Cancel();
            m_HistoricalProfile.Clear();
            m_HistoricalReady = 0;
        }

        /// <summary>
        /// Ensures that historical profile and data is loaded.
        /// </summary>
        public void EnsureHistorical() {
            if ((m_HistoricalReady & DataReadyFlags.Profile) == 0 && !m_HistoricalProfileTask.IsRunning()) {
                m_HistoricalProfileTask = Async.Schedule(HistoricalProfileTask(m_HistoricalProfile, m_ProgressInfo), AsyncFlags.HighPriority);
                m_HistoricalDataTask.Cancel();
                m_HistoricalDataTask = Async.Schedule(DescriptiveDataTask(m_HistoricalProfile, m_HistoricalBuffer, m_ProgressInfo, m_HistoricalOutput, SectionType.Historical, null), AsyncFlags.HighPriority);
            } else if ((m_HistoricalReady & DataReadyFlags.Data) == 0 && !m_HistoricalDataTask.IsRunning()) {
                m_HistoricalDataTask = Async.Schedule(DescriptiveDataTask(m_HistoricalProfile, m_HistoricalBuffer, m_ProgressInfo, m_HistoricalOutput, SectionType.Historical, null), AsyncFlags.HighPriority);
            }
        }

        /// <summary>
        /// Forces historical profile and data to rebuild.
        /// </summary>
        public void GenerateHistorical() {
            m_HistoricalProfileTask.Cancel();
            m_HistoricalDataTask.Cancel();
            m_HistoricalReady = 0;

            m_HistoricalProfileTask = Async.Schedule(HistoricalProfileTask(m_HistoricalProfile, m_ProgressInfo), AsyncFlags.HighPriority);
            m_HistoricalDataTask = Async.Schedule(DescriptiveDataTask(m_HistoricalProfile, m_HistoricalBuffer, m_ProgressInfo, m_HistoricalOutput, SectionType.Historical, null), AsyncFlags.HighPriority);
        }

        /// <summary>
        /// Returns if any historical data is missing.
        /// </summary>
        public ModelMissingReasons EvaluateHistoricalDataMissing() {
            ModelMissingReasons reasons = 0;
            
            foreach(var sync in m_RelevantCritterIds) {
                if (!m_CrittersWithHistoricalData.Contains(sync)) {
                    reasons |= ModelMissingReasons.HistoricalPopulations;
                    Log.Msg("[SimulationDataCtrl] Missing historical population for '{0}'", sync);
                }
            }

            foreach(var prop in m_RelevantWaterProperties) {
                if (!m_WaterPropertiesWithHistoricalData[prop]) {
                    reasons |= ModelMissingReasons.HistoricalWaterChem;
                    Log.Msg("[SimulationDataCtrl] Missing historical water chemistry for '{0}'", prop);
                }
            }

            return reasons;
        }

        #endregion // Historical Data

        #region Player Data

        public SimProfile PlayerProfile { get { return m_PlayerProfile; } }

        public bool IsPlayerProfileReady() {
            return (m_PlayerReady & DataReadyFlags.Profile) != 0;
        }

        public bool IsPlayerDataReady() {
            return (m_PlayerReady & DataReadyFlags.Data) != 0;
        }

        public bool IsPlayerReady() {
            return m_PlayerReady == DataReadyFlags.Full;
        }

        public SimSnapshot* RetrievePlayerData(out uint totalTicks) {
            if (!IsPlayerDataReady()) {
                totalTicks = 0;
                return null;
            }

            totalTicks = 1 + m_ProgressInfo.Sim.SyncTickCount;
            return m_PlayerOutput.Ptr;
        }

        /// <summary>
        /// Clears all player data.
        /// </summary>
        public void ClearPlayer() {
            m_PlayerProfileTask.Cancel();
            m_PlayerDataTask.Cancel();
            m_PlayerProfile.Clear();
            m_PlayerReady = 0;

            // prediction data depends on player data
            ClearPredict();
        }

        /// <summary>
        /// Ensures that player profile is loaded.
        /// </summary>
        public void EnsurePlayerProfile() {
            if ((m_PlayerReady & DataReadyFlags.Profile) == 0 && !m_PlayerProfileTask.IsRunning()) {
                GenerateSimulatedSubset();
                m_PlayerProfileTask = Async.Schedule(PlayerProfileTask(m_PlayerProfile, m_ProgressInfo, m_State.Conceptual, null, SectionType.Player), AsyncFlags.HighPriority);
                m_PlayerReady &= ~DataReadyFlags.Data;

                // prediction data depends on player data
                ClearPredict();
            }
        }

        /// <summary>
        /// Ensures that player data is loaded.
        /// </summary>
        public void EnsurePlayerData() {
            if ((m_PlayerReady & DataReadyFlags.Profile) == 0 && !m_PlayerProfileTask.IsRunning()) {
                GenerateSimulatedSubset();
                m_PlayerProfileTask = Async.Schedule(PlayerProfileTask(m_PlayerProfile, m_ProgressInfo, m_State.Conceptual, null, SectionType.Player), AsyncFlags.HighPriority);
                m_PlayerDataTask.Cancel();
                m_PlayerDataTask = Async.Schedule(DescriptiveDataTask(m_PlayerProfile, m_PlayerBuffer, m_ProgressInfo, m_PlayerOutput, SectionType.Player, ShouldGraphHistorical), AsyncFlags.HighPriority);
                
                // prediction data depends on player data
                ClearPredict();
            } else if ((m_PlayerReady & DataReadyFlags.Data) == 0 && !m_PlayerDataTask.IsRunning()) {
                m_PlayerDataTask = Async.Schedule(DescriptiveDataTask(m_PlayerProfile, m_PlayerBuffer, m_ProgressInfo, m_PlayerOutput, SectionType.Player, ShouldGraphHistorical), AsyncFlags.HighPriority);
                
                // prediction data depends on player data
                m_PredictReady &= ~DataReadyFlags.Data;
                m_PredictDataTask.Cancel();
            }
        }

        /// <summary>
        /// Forces player profile to rebuild.
        /// </summary>
        public void GeneratePlayerProfile() {
            m_PlayerProfileTask.Cancel();
            m_PlayerDataTask.Cancel();
            m_PlayerReady = 0;

            GenerateSimulatedSubset();
            m_PlayerProfileTask = Async.Schedule(PlayerProfileTask(m_PlayerProfile, m_ProgressInfo, m_State.Conceptual, null, SectionType.Player), AsyncFlags.HighPriority);
            
            // prediction data depends on player data
            ClearPredict();
        }

        /// <summary>
        /// Forces player data to rebuild.
        /// </summary>
        public void GeneratePlayerData() {
            // if profile is not ready, we also need to rebuild that
            if ((m_PlayerReady & DataReadyFlags.Profile) == 0 && !m_PlayerProfileTask.IsRunning()) {
                GenerateSimulatedSubset();
                m_PlayerProfileTask = Async.Schedule(PlayerProfileTask(m_PlayerProfile, m_ProgressInfo, m_State.Conceptual, null, SectionType.Player), AsyncFlags.HighPriority);
                ClearPredict();
            }
            m_PlayerReady &= ~DataReadyFlags.Data;

            m_PlayerDataTask.Cancel();
            m_PlayerDataTask = Async.Schedule(DescriptiveDataTask(m_PlayerProfile, m_PlayerBuffer, m_ProgressInfo, m_PlayerOutput, SectionType.Player, ShouldGraphHistorical), AsyncFlags.HighPriority);

            // prediction data depends on player data
            m_PredictReady &= ~DataReadyFlags.Data;
            m_PredictDataTask.Cancel();
        }

        /// <summary>
        /// Calculates accuracy between historical data and player data.
        /// </summary>
        public int CalculateAccuracy(uint snapshotCount) {
            return 100 - (int) (m_ErrorScale * Simulation.CalculateAverageError(m_PlayerOutput.Ptr, m_PlayerProfile, m_HistoricalOutput.Ptr, m_HistoricalProfile, snapshotCount, ShouldGraphHistorical, m_RelevantCritterIds.Count, m_ProgressInfo.Scope?.IncludeWaterChemistryInAccuracy ?? false));
        }

        #endregion // Player Data

        #region Prediction Data

        public SimProfile PredictProfile { get { return m_PredictProfile; } }

        public bool IsPredictProfileReady() {
            return (m_PredictReady & DataReadyFlags.Profile) != 0;
        }

        public bool IsPredictDataReady() {
            return (m_PredictReady & DataReadyFlags.Data) != 0;
        }

        public bool IsPredictReady() {
            return m_PredictReady == DataReadyFlags.Full;
        }

        public SimSnapshot* RetrievePredictData(out uint totalTicks) {
            if (!IsPredictDataReady()) {
                totalTicks = 0;
                return null;
            }

            totalTicks = 1 + m_ProgressInfo.Sim.PredictTickCount;
            return m_PredictOutputSlice.Ptr;
        }

        /// <summary>
        /// Clears prediction profile and data.
        /// </summary>
        public void ClearPredict() {
            m_PredictProfileTask.Cancel();
            m_PredictDataTask.Cancel();
            m_PredictProfile.Clear();
            m_PredictReady = 0;
        }

        /// <summary>
        /// Ensures that the prediction profile is loaded.
        /// </summary>
        public void EnsurePredictProfile() {
            if ((m_PredictReady & DataReadyFlags.Profile) == 0 && !m_PredictProfileTask.IsRunning()) {
                m_PredictProfileTask = Async.Schedule(PlayerProfileTask(m_PlayerProfile, m_ProgressInfo, m_State.Conceptual, Intervention, SectionType.Predict));
                m_PredictReady &= ~DataReadyFlags.Data;
            }
        }

        /// <summary>
        /// Ensures that prediction data is loaded.
        /// </summary>
        public void EnsurePredictData() {
            EnsurePlayerData();

            if ((m_PredictReady & DataReadyFlags.Profile) == 0 && !m_PredictProfileTask.IsRunning()) {
                m_PredictProfileTask = Async.Schedule(PlayerProfileTask(m_PredictProfile, m_ProgressInfo, m_State.Conceptual, Intervention, SectionType.Predict), AsyncFlags.HighPriority);
                m_PredictDataTask.Cancel();
                m_PredictDataTask = Async.Schedule(PredictDataTask(m_PredictProfile, m_PlayerBuffer, m_ProgressInfo, Intervention, m_PredictOutputSlice), AsyncFlags.HighPriority);
            } else if ((m_PredictReady & DataReadyFlags.Data) == 0 && !m_PredictDataTask.IsRunning()) {
                m_PredictDataTask = Async.Schedule(PredictDataTask(m_PredictProfile, m_PlayerBuffer, m_ProgressInfo, Intervention, m_PredictOutputSlice), AsyncFlags.HighPriority);
            }
        }

        /// <summary>
        /// Forces predict profile to rebuild.
        /// </summary>
        public void GeneratePredictProfile() {
            m_PredictProfileTask.Cancel();
            m_PredictDataTask.Cancel();
            m_PredictReady = 0;

            m_PredictProfileTask = Async.Schedule(PlayerProfileTask(m_PredictProfile, m_ProgressInfo, m_State.Conceptual, Intervention, SectionType.Predict), AsyncFlags.HighPriority);
        }

        /// <summary>
        /// Forces predict data to rebuild.
        /// </summary>
        public void GeneratePredictData() {
            EnsurePlayerData();

            // if profile is not ready, we also need to rebuild that
            if ((m_PredictReady & DataReadyFlags.Profile) == 0 && !m_PredictProfileTask.IsRunning()) {
                m_PredictProfileTask = Async.Schedule(PlayerProfileTask(m_PredictProfile, m_ProgressInfo, m_State.Conceptual, Intervention, SectionType.Predict), AsyncFlags.HighPriority);
            }
            m_PredictReady &= ~DataReadyFlags.Data;

            m_PredictDataTask.Cancel();
            m_PredictDataTask = Async.Schedule(PredictDataTask(m_PredictProfile, m_PlayerBuffer, m_ProgressInfo, Intervention, m_PredictOutputSlice), AsyncFlags.HighPriority);
        }

        /// <summary>
        /// Regenerates intervention data.
        /// </summary>
        public void RegenerateIntervention() {
            Intervention.AdditionalEntities.Clear();
            Intervention.AdditionalFacts.Clear();
            
            FactUtil.GatherInterventionSubset(Intervention.Target, Save.Bestiary, m_State.Conceptual.SimulatedEntities, Intervention.AdditionalEntities, Intervention.AdditionalFacts);
            ClearPredict();
            DispatchInterventionUpdate();
        }

        /// <summary>
        /// Dispatches the intervention update message.
        /// </summary>
        public void DispatchInterventionUpdate() {
            OnInterventionUpdated?.Invoke();
        }

        /// <summary>
        /// Clears the intervention.
        /// </summary>
        public void ClearIntervention() {
            if (Intervention.Target != null) {
                Intervention.Target = null;
                Intervention.AdditionalEntities.Clear();
                Intervention.AdditionalFacts.Clear();
                Intervention.Amount = 0;
                ClearPredict();
                DispatchInterventionUpdate();
            }
        }

        /// <summary>
        /// Returns if the given organism can be introduced as an intervention.
        /// </summary>
        public bool CanIntroduceForIntervention(BestiaryDesc organism) {
            return !m_State.Conceptual.SimulatedEntities.Contains(organism);
        }

        /// <summary>
        /// Returns if the current intervention is a new organism
        /// </summary>
        public bool IsInterventionNewOrganism() {
            return Intervention.Target != null && !m_State.Conceptual.SimulatedEntities.Contains(Intervention.Target);
        }

        /// <summary>
        /// Returns the intervention starting position.
        /// </summary>
        public uint GetInterventionStartingPopulation(BestiaryDesc organism) {
            SimSnapshot* endSnapshot = m_PredictOutputSlice.Ptr - 1;
            Simulation.GetPopulation(endSnapshot, m_PlayerProfile, organism.Id(), out uint population, out float _);
            return population;
        }

        /// <summary>
        /// Evaluates whether or not intervention goals were met.
        /// </summary>
        public bool EvaluateInterventionGoals() {
            if (!m_ProgressInfo.Scope) {
                return false;
            }

            SimProfile finalProfile = m_PredictProfile;
            SimSnapshot finalShapshot = m_PredictOutputSlice.Ptr[m_ProgressInfo.Sim.PredictTickCount];
            ActorCountRange[] targets = m_ProgressInfo.Scope.InterventionTargets;
            for(int i = 0; i < targets.Length; i++) {
                ActorCountRange target = targets[i];
                int actorIdx = finalProfile.IndexOfActorType(target.Id);
                if (actorIdx < 0) {
                    return false;
                }

                uint population = finalShapshot.Populations[actorIdx];
                long diff = (long) population - target.Population;
                if (diff > target.Range || diff < -target.Range) {
                    return false;
                }
            }

            return true;
        }

        #endregion // Prediction Data

        #region Profiles

        /// <summary>
        /// Builds historical profile.
        /// </summary>
        private IEnumerator HistoricalProfileTask(SimProfile profile, ModelProgressInfo info) {
            profile.Clear();

            if (info.Sim != null) {
                profile.ImportSim(info.Sim);
            }

            foreach(var entity in info.ImportableEntities) {
                profile.ImportActor(entity);
                yield return null;
            }

            profile.FinishActors();
            yield return null;

            foreach(var entity in info.ImportableEntities) {
                foreach(var internalFact in entity.InternalFacts) {
                    profile.ImportFact(internalFact, BFDiscoveredFlags.All);
                    yield return null;
                }

                foreach(var alwaysFact in entity.AssumedFacts) {
                    profile.ImportFact(alwaysFact, BFDiscoveredFlags.All);
                    yield return null;
                }
            }

            foreach(var fact in info.ImportableFacts) {
                profile.ImportFact(fact, BFDiscoveredFlags.All);
                yield return null;
            }

            profile.FinishFacts();
            m_HistoricalReady |= DataReadyFlags.Profile;

            Log.Msg("[SimulationUI] Finished building historical profile\n{0}", SimProfile.Dump(profile));
        }

        /// <summary>
        /// Builds player or prediction profile.
        /// </summary>
        private IEnumerator PlayerProfileTask(SimProfile profile, ModelProgressInfo info, ConceptualModelState model, InterventionData intervention, SectionType type) {
            profile.Clear();

            if (info.Sim != null) {
                profile.ImportSim(info.Sim);
            }

            foreach(var entity in model.SimulatedEntities) {
                profile.ImportActor(entity);
                yield return null;
            }

            if (intervention != null) {
                foreach(var entity in intervention.AdditionalEntities) {
                    profile.ImportActor(entity);
                    yield return null;
                }
            }

            profile.FinishActors();
            yield return null;

            foreach(var entity in model.SimulatedEntities) {
                foreach(var internalFact in entity.InternalFacts) {
                    profile.ImportFact(internalFact, BFDiscoveredFlags.All);
                    yield return null;
                }

                foreach(var alwaysFact in entity.AssumedFacts) {
                    profile.ImportFact(alwaysFact, BFDiscoveredFlags.All);
                    yield return null;
                }
            }

            if (intervention != null) {
                foreach(var entity in intervention.AdditionalEntities) {
                    foreach(var internalFact in entity.InternalFacts) {
                        profile.ImportFact(internalFact, BFDiscoveredFlags.All);
                        yield return null;
                    }

                    foreach(var alwaysFact in entity.AssumedFacts) {
                        profile.ImportFact(alwaysFact, BFDiscoveredFlags.All);
                        yield return null;
                    }
                }
            }

            foreach(var fact in model.SimulatedFacts) {
                profile.ImportFact(fact, Save.Bestiary.GetDiscoveredFlags(fact.Id));
                yield return null;
            }

            if (intervention != null) {
                foreach(var fact in intervention.AdditionalFacts) {
                    profile.ImportFact(fact, Save.Bestiary.GetDiscoveredFlags(fact.Id));
                    yield return null;
                }
            }

            profile.FinishFacts();

            if (type == SectionType.Player) {
                m_PlayerReady |= DataReadyFlags.Profile;
                Log.Msg("[SimulationUI] Finished building player profile\n{0}", SimProfile.Dump(profile));
            } else {
                m_PredictReady |= DataReadyFlags.Profile;
                Log.Msg("[SimulationUI] Finished building predict profile\n{0}", SimProfile.Dump(profile));
            }
        }
    
        /// <summary>
        /// Builds descriptive data.
        /// </summary>
        private IEnumerator DescriptiveDataTask(SimProfile profile, Simulation.Buffer buffer, ModelProgressInfo info, ResultWrapper output, SectionType type, Predicate<StringHash32> organismFilter) {
            if (info.Sim == null) {
                Log.Warn("[SimulationDataCtrl] No simulation available");
                yield break;
            }

            using(Profiling.Time(string.Format("generating {0} data", type))) {
                SimSnapshot initialSnapshot = Simulation.GenerateInitialSnapshot(profile, info.Sim, organismFilter);
                Simulation.Prepare(buffer, profile, initialSnapshot);
                yield return null;

                WriteResult(ref initialSnapshot, output, 0);

                SimSnapshot current = initialSnapshot;
                for(uint i = 1; i <= info.Sim.SyncTickCount; i++) {
                    current = Simulation.Simulate(buffer, profile, current, i, SimulationFlags.Debug);
                    WriteResult(ref current, output, i);
                    yield return null;
                    Log.Msg(buffer.DebugReport?.Flush());
                }
            }

            if (type == SectionType.Historical) {
                m_HistoricalReady |= DataReadyFlags.Data;;
            } else {
                m_PlayerReady |= DataReadyFlags.Data;
            }
        }

        /// <summary>
        /// Builds prediction data.
        /// </summary>
        private IEnumerator PredictDataTask(SimProfile profile, Simulation.Buffer buffer, ModelProgressInfo info, InterventionData intervention, ResultWrapper output) {
            if (info.Sim == null) {
                Log.Warn("[SimulationDataCtrl] No simulation available");
                yield break;
            }

            using(Profiling.Time("generating prediction data")) {
                SimSnapshot initialSnapshot = InitializePredictSnapshot(output, intervention);
                Simulation.Prepare(buffer, profile, initialSnapshot);
                yield return null;

                SimSnapshot current = initialSnapshot;
                for(uint i = 1; i <= info.Sim.PredictTickCount; i++) {
                    current = Simulation.Simulate(buffer, profile, current, info.Sim.SyncTickCount + i, SimulationFlags.Debug);
                    WriteResult(ref current, output, i);
                    yield return null;
                    Log.Msg(buffer.DebugReport?.Flush());
                }
            }

            m_PredictReady |= DataReadyFlags.Data;
        }

        /// <summary>
        /// Initializes the prediction snapshot..
        /// </summary>
        private SimSnapshot InitializePredictSnapshot(ResultWrapper output, InterventionData intervention) {
            SimSnapshot* ptr = output.Ptr;
            Simulation.CopyTo(ptr - 1, m_PlayerProfile, ptr, m_PredictProfile);
            if (intervention.Target) {
                int index = m_PredictProfile.IndexOfActorType(intervention.Target.Id());
                if (index >= 0) {
                    uint current = ptr->Populations[index];
                    current = (uint) Math.Max(0, current + intervention.Amount);
                    ptr->Populations[index] = current;
                }
            }
            return *ptr;
        }

        static private SimSnapshot ReadResult(ResultWrapper output, uint index) {
            return output.Ptr[index];
        }

        static private void WriteResult(ref SimSnapshot snapshot, ResultWrapper output, uint index) {
            output.Ptr[index] = snapshot;
        }

        #endregion // Profiles
    
        #region Evaluation

        public readonly Predicate<StringHash32> HasHistoricalPopulation;
        public readonly Predicate<StringHash32> ShouldGraphHistorical;
        public readonly Predicate<WaterPropertyId> ShouldGraphWaterProperty;

        #endregion // Evaluation
    }
}