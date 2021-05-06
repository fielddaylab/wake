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

        [NonSerialized] private ModelingScenarioData m_Scenario;

        public void Awake()
        {
            ConceptMap.OnGraphUpdated += OnCritterGraphed;
        }
        
        public void PopulateMap(BestiaryData inPlayerData)
        {
            ConceptMap.SetInitialFacts(inPlayerData.GraphedFacts());
        }

        public void SetScenario(ModelingScenarioData inScenario, bool inbOverride)
        {
            m_Scenario = inScenario;
            ScenarioPanel.SetScenario(inScenario, inbOverride);
        }

        public void OnCritterGraphed(StringHash32 inFactGraphed)
        {
            bool bIsReady = false;
            foreach(var critter in m_Scenario.Actors())
            {
                if (ConceptMap.IsGraphed(critter.Id))
                {
                    bIsReady = true;
                    break;
                }
            }

            ScenarioPanel.SetSimulationReady(bIsReady);
        }
    }
}