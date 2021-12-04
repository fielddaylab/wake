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

        private const int ArenaSize = (int) (1024 * 8f);

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

        public struct InterventionData {
            public BestiaryDesc Target;
            public int Amount;
        }

        [SerializeField] private float m_ErrorScale = 2;
        
        private Unsafe.ArenaHandle m_HistoricalArena;
        private SimProfile m_HistoricalProfile;
        private Simulation.Buffer m_HistoricalBuffer;
        private ResultWrapper m_HistoricalOutput;
        private DataReadyFlags m_HistoricalReady;
        
        private Unsafe.ArenaHandle m_PlayerArena;
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

        private readonly HashSet<StringHash32> m_CrittersToSync = new HashSet<StringHash32>();
        private readonly HashSet<StringHash32> m_CrittersWithHistoricalData = new HashSet<StringHash32>();

        public InterventionData Intervention;

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
                return m_CrittersToSync.Contains(id) && HasHistoricalPopulation(id);
            };
        }

        private void Awake() {
            m_HistoricalArena = Unsafe.CreateArena(ArenaSize, "sim.historical");

            m_HistoricalProfile = new SimProfile(m_HistoricalArena);
            m_HistoricalBuffer = new Simulation.Buffer(m_HistoricalArena);
            m_HistoricalOutput = new ResultWrapper(m_HistoricalArena, 0);

            m_PlayerArena = Unsafe.CreateArena(ArenaSize + SimProfile.BufferSize, "sim.player");
            m_PlayerProfile = new SimProfile(m_PlayerArena);
            m_PlayerBuffer = new Simulation.Buffer(m_PlayerArena);
            m_PlayerOutput = new ResultWrapper(m_PlayerArena, 1);
            m_PredictProfile = new SimProfile(m_PlayerArena);

            Log.Msg("Simulation arenas spare bytes = {0} / {1}", Unsafe.ArenaFreeBytes(m_HistoricalArena), Unsafe.ArenaFreeBytes(m_PlayerArena));
        }

        private void OnDestroy() {
            Unsafe.TryFreeArena(ref m_HistoricalArena);
            Unsafe.TryFreeArena(ref m_PlayerArena);

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
            GenerateHistorical();
            GeneratePlayerProfile();

            if (m_ProgressInfo.Sim) {
                m_PredictOutputSlice = new ResultWrapper(m_PlayerOutput.Ptr + m_ProgressInfo.Sim.SyncTickCount + 1);
            } else {
                m_PredictOutputSlice = default;
            }

            m_CrittersToSync.Clear();
            if (m_ProgressInfo.Scope != null) {
                foreach(var organismId in m_ProgressInfo.Scope.OrganismIds) {
                    m_CrittersToSync.Add(organismId);
                }
            } else {
                foreach(var organism in m_ProgressInfo.ImportableEntities) {
                    if (organism.Category() == BestiaryDescCategory.Critter) {
                        m_CrittersToSync.Add(organism.Id());
                    }
                }
            }
        }

        public void LoadConceptualModel() {
            m_CrittersWithHistoricalData.Clear();

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
        public bool IsAnyHistoricalDataMissing() {
            foreach(var sync in m_CrittersToSync) {
                if (!m_CrittersWithHistoricalData.Contains(sync)) {
                    return true;
                }
            }

            return false;
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
                m_PlayerProfileTask = Async.Schedule(PlayerProfileTask(m_PlayerProfile, m_ProgressInfo, m_State.Conceptual, default, SectionType.Player), AsyncFlags.HighPriority);
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
                m_PlayerProfileTask = Async.Schedule(PlayerProfileTask(m_PlayerProfile, m_ProgressInfo, m_State.Conceptual, default, SectionType.Player), AsyncFlags.HighPriority);
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

            m_PlayerProfileTask = Async.Schedule(PlayerProfileTask(m_PlayerProfile, m_ProgressInfo, m_State.Conceptual, default, SectionType.Player), AsyncFlags.HighPriority);
            
            // prediction data depends on player data
            ClearPredict();
        }

        /// <summary>
        /// Forces player data to rebuild.
        /// </summary>
        public void GeneratePlayerData() {
            // if profile is not ready, we also need to rebuild that
            if ((m_PlayerReady & DataReadyFlags.Profile) == 0 && !m_PlayerProfileTask.IsRunning()) {
                m_PlayerProfileTask = Async.Schedule(PlayerProfileTask(m_PlayerProfile, m_ProgressInfo, m_State.Conceptual, default, SectionType.Player), AsyncFlags.HighPriority);
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
            return 100 - (int) (m_ErrorScale * Simulation.CalculateAverageError(m_PlayerOutput.Ptr, m_PlayerProfile, m_HistoricalOutput.Ptr, m_HistoricalProfile, snapshotCount, ShouldGraphHistorical, m_CrittersToSync.Count, m_ProgressInfo.Scope?.IncludeWaterChemistryInAccuracy ?? false));
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
            Intervention = default;
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
                m_PredictProfileTask = Async.Schedule(PlayerProfileTask(m_PredictProfile, m_ProgressInfo, m_State.Conceptual, default, SectionType.Predict), AsyncFlags.HighPriority);
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

            m_PredictProfileTask = Async.Schedule(PlayerProfileTask(m_PredictProfile, m_ProgressInfo, m_State.Conceptual, default, SectionType.Predict), AsyncFlags.HighPriority);
        }

        /// <summary>
        /// Forces predict data to rebuild.
        /// </summary>
        public void GeneratePredictData() {
            EnsurePlayerData();

            // if profile is not ready, we also need to rebuild that
            if ((m_PredictReady & DataReadyFlags.Profile) == 0 && !m_PredictProfileTask.IsRunning()) {
                m_PredictProfileTask = Async.Schedule(PlayerProfileTask(m_PredictProfile, m_ProgressInfo, m_State.Conceptual, default, SectionType.Predict), AsyncFlags.HighPriority);
            }
            m_PredictReady &= ~DataReadyFlags.Data;

            m_PredictDataTask.Cancel();
            m_PredictDataTask = Async.Schedule(PredictDataTask(m_PredictProfile, m_PlayerBuffer, m_ProgressInfo, Intervention, m_PredictOutputSlice), AsyncFlags.HighPriority);
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

            foreach(var entity in model.GraphedEntities) {
                profile.ImportActor(entity);
                yield return null;
            }

            bool interventionIsNew = intervention.Target != null && !model.GraphedEntities.Contains(intervention.Target);

            if (interventionIsNew) {
                profile.ImportActor(intervention.Target);
                yield return null;
            }

            profile.FinishActors();
            yield return null;

            foreach(var entity in model.GraphedEntities) {
                foreach(var internalFact in entity.InternalFacts) {
                    profile.ImportFact(internalFact, BFDiscoveredFlags.All);
                    yield return null;
                }

                foreach(var alwaysFact in entity.AssumedFacts) {
                    profile.ImportFact(alwaysFact, BFDiscoveredFlags.All);
                    yield return null;
                }
            }

            if (interventionIsNew) {
                foreach(var internalFact in intervention.Target.InternalFacts) {
                    profile.ImportFact(internalFact, BFDiscoveredFlags.All);
                    yield return null;
                }

                foreach(var alwaysFact in intervention.Target.AssumedFacts) {
                    profile.ImportFact(alwaysFact, BFDiscoveredFlags.All);
                    yield return null;
                }

                foreach(var playerFact in intervention.Target.PlayerFacts) {
                    if (playerFact.Parent == intervention.Target && Save.Bestiary.HasFact(playerFact.Id)) {
                        profile.ImportFact(playerFact, Save.Bestiary.GetDiscoveredFlags(playerFact.Id));
                        yield return null;
                    }
                }
            }

            foreach(var fact in model.GraphedFacts) {
                profile.ImportFact(fact, Save.Bestiary.GetDiscoveredFlags(fact.Id));
                yield return null;
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
                    current = Simulation.Simulate(buffer, profile, current, i, SimulationFlags.Debug);
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

        #endregion // Evaluation
    }
}