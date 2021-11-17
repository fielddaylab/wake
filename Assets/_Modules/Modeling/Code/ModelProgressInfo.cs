using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aqua.Modeling {
    public class ModelProgressInfo {

        public ConceptModelScenario ConceptScenario;
        public SimulationModelScenario SimScenario;

        public ModelPhases Phases = ModelPhases.Ecosystem | ModelPhases.Concept;
        public readonly HashSet<BestiaryDesc> RequiredEntities = new HashSet<BestiaryDesc>();
        public readonly HashSet<BFBase> RequiredFacts = new HashSet<BFBase>();

        public void Load(JobDesc desc) {
            ConceptScenario = desc.FindAsset<ConceptModelScenario>();
            SimScenario = desc.FindAsset<SimulationModelScenario>();
        }
    }
}