using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Profile;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class ModelingUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField] public ConceptMapUI ConceptMap = null;
        [SerializeField] public ScenarioPanelUI ScenarioPanel = null;

        #endregion // Inspector

        private void Awake()
        {
            ScenarioPanel.OnShowEvent.AddListener(OnScenarioShow);
            ScenarioPanel.OnHideEvent.AddListener(OnScenarioHide);
        }
        
        public void PopulateMap(BestiaryData inPlayerData)
        {
            ConceptMap.SetInitialFacts(inPlayerData.GraphedFacts());
        }

        public void SetScenario(ModelingScenarioData inScenario, bool inbOverride)
        {
            ScenarioPanel.SetScenario(inScenario, inbOverride);
        }

        public void OnBufferUpdate(SimulationBuffer inBuffer, SimulationBuffer.UpdateFlags inFlags)
        {
            if ((inFlags & SimulationBuffer.UpdateFlags.Model) == 0)
                return;

            ScenarioPanel.SetSimulationReady(inBuffer.PlayerCritters().Count > 0);
        }

        #region Handlers

        private void OnScenarioShow(BasePanel.TransitionType inType)
        {
            ConceptMap.SetHighlightAllowed(ScenarioPanel.CanSimulate());
        }

        private void OnScenarioHide(BasePanel.TransitionType inType)
        {
            ConceptMap.SetHighlightAllowed(false);
        }

        #endregion // Handlers
    }
}