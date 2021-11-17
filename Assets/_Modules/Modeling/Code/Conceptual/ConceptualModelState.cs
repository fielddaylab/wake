using System;
using System.Collections.Generic;
using Aqua.Profile;

namespace Aqua.Modeling {
    public class ConceptualModelState {
        
        public readonly HashSet<BFBase> GraphedFacts = new HashSet<BFBase>();
        public readonly HashSet<BestiaryDesc> GraphedCritters = new HashSet<BestiaryDesc>();

        public void LoadFrom(SiteSurveyData siteData) {
            GraphedCritters.Clear();
            GraphedFacts.Clear();

            foreach(var critterId in siteData.GraphedCritters) {
                GraphedCritters.Add(Assets.Bestiary(critterId));
            }

            foreach(var factId in siteData.GraphedFacts) {
                GraphedFacts.Add(Assets.Fact(factId));
            }
        }
    }
}