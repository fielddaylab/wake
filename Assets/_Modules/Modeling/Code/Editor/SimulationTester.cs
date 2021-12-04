using BeauUtil;
using System;
using UnityEngine;
using UnityEditor;
using BeauUtil.Debugger;

namespace Aqua.Modeling.Editor {
    public class SimulationTester : EditorWindow {
        public BFSim Sim;
        private readonly ModelProgressInfo m_ProgressInfo = new ModelProgressInfo();

        [MenuItem("Aqualab/Test Modeling Simulation")]
        static private void CreateWizard() {
            EditorWindow.GetWindow<SimulationTester>().Show();
        }

        private void OnEnable() {
            titleContent = new GUIContent("Simulation Tester");
        }

        private void OnGUI() {
            Sim = (BFSim) EditorGUILayout.ObjectField("Sim", Sim, typeof(BFSim), false);

            EditorGUILayout.Space();
            
            using(new EditorGUI.DisabledScope(Sim == null)) {
                if (GUILayout.Button("Simulate")) {
                    Simulate();
                }
            }
        }

        private void Simulate() {
            using(Profiling.Time("generating report")) {
                m_ProgressInfo.LoadFromScope(Sim.Parent, null);

                using(var buffer = new Simulation.Buffer())
                using(var profile = new SimProfile()) {
                    profile.ImportSim(m_ProgressInfo.Sim);

                    foreach(var entity in m_ProgressInfo.ImportableEntities) {
                        profile.ImportActor(entity);
                    }

                    profile.FinishActors();

                    foreach(var entity in m_ProgressInfo.ImportableEntities) {
                        foreach(var internalFact in entity.InternalFacts) {
                            profile.ImportFact(internalFact, BFDiscoveredFlags.All);
                        }

                        foreach(var alwaysFact in entity.AssumedFacts) {
                            profile.ImportFact(alwaysFact, BFDiscoveredFlags.All);
                        }
                    }

                    foreach(var fact in m_ProgressInfo.ImportableFacts) {
                        profile.ImportFact(fact, BFDiscoveredFlags.All);
                    }

                    profile.FinishFacts();

                    SimSnapshot initialSnapshot = Simulation.GenerateInitialSnapshot(profile, m_ProgressInfo.Sim);;
                    Simulation.Prepare(buffer, profile, initialSnapshot);

                    SimSnapshot current = initialSnapshot;
                    for(uint i = 0; i < m_ProgressInfo.Sim.SyncTickCount; i++) {
                        current = Simulation.Simulate(buffer, profile, current, i + 1, SimulationFlags.Debug);
                        Log.Msg(buffer.DebugReport?.Flush());
                    }

                    for(uint i = 0; i < m_ProgressInfo.Sim.PredictTickCount; i++) {
                        current = Simulation.Simulate(buffer, profile, current, m_ProgressInfo.Sim.SyncTickCount + i + 1, SimulationFlags.Debug);
                        Log.Msg(buffer.DebugReport?.Flush());
                    }
                }

                m_ProgressInfo.Reset(null);
            }
        }
    }
}