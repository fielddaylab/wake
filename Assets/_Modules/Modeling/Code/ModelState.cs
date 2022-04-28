using System;
using Aqua.Profile;
using BeauUtil;
using UnityEngine;

namespace Aqua.Modeling
{
    public class ModelState {

        public delegate void PhaseChangedDelegate(ModelPhases prev, ModelPhases current);
        public delegate void UpdateStatusInfoDelegate(TextId text, Color? color = null);
        public delegate void DisplayInlinePopupDelegate(TextId text, Color? color = null);
        public delegate void DisplayInlineFactsDelegate(BFBase[] facts);

        public ModelPhases Phase;
        public ModelPhases AllowedPhases;
        public ModelPhases CompletedPhases;

        public BestiaryDesc Environment;
        public SiteSurveyData SiteData;

        public int LastKnownAccuracy;
        
        public readonly ConceptualModelState Conceptual = new ConceptualModelState();
        public SimulationDataCtrl Simulation;

        public PhaseChangedDelegate OnPhaseChanged;

        public UpdateStatusInfoDelegate UpdateStatus;
        public DisplayInlinePopupDelegate PopupText;
        public DisplayInlineFactsDelegate PopupFacts;
        public Action ClearPopup;
    }
}