using System;
using Aqua.Profile;
using BeauUtil;
using UnityEngine;

namespace Aqua.Modeling
{
    public class ModelState {

        public delegate void PhaseChangedDelegate(ModelPhases prev, ModelPhases current);
        public delegate void GraphChangedDelegate(WorldFilterMask graphed);

        public ModelPhases Phase;
        public ModelPhases AllowedPhases;
        public ModelPhases CompletedPhases;

        public BestiaryDesc Environment;
        public SiteSurveyData SiteData;

        public int LastKnownAccuracy;
        
        public readonly ConceptualModelState Conceptual = new ConceptualModelState();
        public SimulationDataCtrl Simulation;

        public PhaseChangedDelegate OnPhaseChanged;
        public GraphChangedDelegate OnGraphChanged;
        public ModelVisualCallbacks Display;
    }

    public struct ModelVisualCallbacks {
        public delegate void UpdateStatusInfoDelegate(TextId text, Color? color = null);
        public delegate void DisplayInlinePopupDelegate(TextId text, Color? color = null);
        public delegate void DisplayInlineFactsDelegate(BFBase[] facts);
        public delegate void FilterNodesDelegate(WorldFilterMask any, WorldFilterMask all = 0, WorldFilterMask none = 0, bool force = false);
        
        public UpdateStatusInfoDelegate Status;
        public DisplayInlinePopupDelegate TextPopup;
        public DisplayInlineFactsDelegate FactsPopup;
        public FilterNodesDelegate FilterNodes;
        public Action ClearPopup;
    }
}