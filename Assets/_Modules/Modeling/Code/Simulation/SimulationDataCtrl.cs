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
    public class SimulationDataCtrl : MonoBehaviour {
        private SimulationProfile m_HistoricalProfile;
        private Simulation.Buffer m_HistoricalBuffer;
        private SimulationProfile m_PlayerProfile;
        private Simulation.Buffer m_PlayerBuffer;

        private ModelState m_State;
        private ModelProgressInfo m_ProgressionInfo;
        private AsyncHandle m_BuildHistoricalProfileTask;

        public void SetData(ModelState state, ModelProgressInfo info) {
            m_State = state;
            m_ProgressionInfo = info;
        }

        #region Unity Events

        private void Awake() {
            m_HistoricalProfile = new SimulationProfile();
            m_PlayerProfile = new SimulationProfile();
            m_HistoricalBuffer = new Simulation.Buffer();
            m_PlayerBuffer = new Simulation.Buffer();
        }

        private void OnDestroy() {
            m_HistoricalProfile.Dispose();
            m_PlayerProfile.Dispose();
            m_HistoricalBuffer.Dispose();
            m_PlayerBuffer.Dispose();
        }

        #endregion // Unity Events

        public void TESTBuildProfile() {
            m_BuildHistoricalProfileTask.Cancel();
            m_BuildHistoricalProfileTask = Async.Schedule(BuildHistoricalTask(m_HistoricalProfile, m_ProgressionInfo), AsyncFlags.HighPriority);
            Async.Schedule(GenerateHistoricalData(m_HistoricalProfile, m_HistoricalBuffer, m_ProgressionInfo), AsyncFlags.HighPriority);
        }

        #region Profiles

        static private IEnumerator BuildHistoricalTask(SimulationProfile profile, ModelProgressInfo info) {
            profile.Clear();

            profile.ImportSim(info.Sim);

            foreach(var entity in info.ImportableEntities) {
                profile.ImportActor(entity);
                yield return null;
            }

            profile.FinishActors();
            yield return null;

            foreach(var entity in info.ImportableEntities) {
                foreach(var internalFact in entity.InternalFacts) {
                    profile.ImportFact(internalFact);
                    yield return null;
                }

                foreach(var alwaysFact in entity.AssumedFacts) {
                    profile.ImportFact(alwaysFact);
                    yield return null;
                }
            }

            foreach(var fact in info.ImportableFacts) {
                profile.ImportFact(fact);
                yield return null;
            }

            profile.FinishFacts();

            Log.Msg("[SimulationUI] Finished building historical profile\n{0}", SimulationProfile.Dump(profile));
        }

        static private IEnumerator BuildPlayerTask(SimulationProfile profile, ModelProgressInfo info, ConceptualModelState model) {
            profile.Clear();

            profile.ImportSim(info.Sim);

            foreach(var entity in model.GraphedEntities) {
                profile.ImportActor(entity);
                yield return null;
            }

            profile.FinishActors();
            yield return null;

            foreach(var entity in model.GraphedEntities) {
                foreach(var internalFact in entity.InternalFacts) {
                    profile.ImportFact(internalFact);
                    yield return null;
                }

                foreach(var alwaysFact in entity.AssumedFacts) {
                    profile.ImportFact(alwaysFact);
                    yield return null;
                }
            }

            foreach(var fact in model.GraphedFacts) {
                profile.ImportFact(fact);
                yield return null;
            }

            profile.FinishFacts();

            Log.Msg("[SimulationUI] Finished building player profile\n{0}", SimulationProfile.Dump(profile));
        }
    
        static private IEnumerator GenerateHistoricalData(SimulationProfile profile, Simulation.Buffer buffer, ModelProgressInfo info) {
            using(Profiling.Time("generating historical data")) {
                SimSnapshot initialSnapshot = Simulation.GenerateInitialSnapshot(profile, info.Sim);;
                Simulation.Prepare(buffer, profile, initialSnapshot);
                yield return null;

                SimSnapshot current = initialSnapshot;
                for(int i = 0; i < info.Sim.SyncTickCount; i++) {
                    current = Simulation.Simulate(buffer, profile, current, SimulationFlags.Debug);
                    yield return null;
                    Log.Msg(buffer.DebugReport?.Flush());
                }
            }
        }

        #endregion // Profiles
    }
}