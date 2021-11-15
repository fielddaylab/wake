using System;
using System.Collections.Generic;
using Aqua.Profile;

namespace Aqua.Modeling {
    public class ConceptualModelState {

        // copy of site state
        private readonly HashSet<BFBase> m_GraphedFacts = new HashSet<BFBase>();
        private readonly HashSet<BestiaryDesc> m_GraphedCritters = new HashSet<BestiaryDesc>();

        // list of facts required to create model
        private readonly HashSet<BFBase> m_FactsRequiredForModel = new HashSet<BFBase>();

        public void LoadFrom(SiteSurveyData siteData) {
            m_GraphedCritters.Clear();
            m_GraphedFacts.Clear();

            foreach(var critterId in siteData.GraphedCritters) {
                m_GraphedCritters.Add(Assets.Bestiary(critterId));
            }

            foreach(var factId in siteData.GraphedFacts) {
                m_GraphedFacts.Add(Assets.Fact(factId));
            }
        }
    }
}