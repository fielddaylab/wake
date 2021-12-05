using System;
using Aqua.Profile;
using BeauUtil;

namespace Aqua.Modeling
{
    public class ModelState {

        public delegate void PhaseChangedDelegate(ModelPhases prev, ModelPhases current);

        public ModelPhases Phase;
        public ModelPhases AllowedPhases;
        public ModelPhases CompletedPhases;

        public BestiaryDesc Environment;
        public SiteSurveyData SiteData;

        public int LastKnownAccuracy;
        
        public readonly ConceptualModelState Conceptual = new ConceptualModelState();
        public SimulationDataCtrl Simulation;

        public PhaseChangedDelegate OnPhaseChanged;
    }
}