using BeauUtil;
using System;
using UnityEngine;
using UnityEditor;
using BeauUtil.Debugger;

namespace Aqua.Modeling.Editor {
    public class SimulationTester : ScriptableWizard {
        public BFSim Sim;

        private readonly ModelProgressInfo m_ProgressInfo = new ModelProgressInfo();

        [MenuItem("Aqualab/Test Modeling Simulation")]
        static private void CreateWizard() {
            ScriptableWizard.DisplayWizard<SimulationTester>("Test Simulation", "Export", "Simulate");
        }

        private void OnWizardUpdate() {
            isValid = Sim != null;
        }

        private void OnWizardOtherButton() {
            if (Sim == null) {
                return;
            }

            using(Profiling.Time("generating report")) {
                m_ProgressInfo.Load(Sim.Parent, null);

                using(var buffer = new Simulation.Buffer())
                using(var profile = new SimulationProfile()) {
                    profile.ImportSim(m_ProgressInfo.Sim);

                    foreach(var entity in m_ProgressInfo.ImportableEntities) {
                        profile.ImportActor(entity);
                    }

                    profile.FinishActors();

                    foreach(var entity in m_ProgressInfo.ImportableEntities) {
                        foreach(var internalFact in entity.InternalFacts) {
                            profile.ImportFact(internalFact);
                        }

                        foreach(var alwaysFact in entity.AssumedFacts) {
                            profile.ImportFact(alwaysFact);
                        }
                    }

                    foreach(var fact in m_ProgressInfo.ImportableFacts) {
                        profile.ImportFact(fact);
                    }

                    profile.FinishFacts();

                    SimSnapshot initialSnapshot = Simulation.GenerateInitialSnapshot(profile, m_ProgressInfo.Sim);;
                    Simulation.Prepare(buffer, profile, initialSnapshot);

                    SimSnapshot current = initialSnapshot;
                    for(int i = 0; i < m_ProgressInfo.Sim.SyncTickCount; i++) {
                        current = Simulation.Simulate(buffer, profile, current, SimulationFlags.Debug);
                        Log.Msg(buffer.DebugReport?.Flush());
                    }

                    for(int i = 0; i < m_ProgressInfo.Sim.PredictTickCount; i++) {
                        current = Simulation.Simulate(buffer, profile, current, SimulationFlags.Debug);
                        Log.Msg(buffer.DebugReport?.Flush());
                    }
                }

                m_ProgressInfo.Load(null, null);

            }
        }
    }
}