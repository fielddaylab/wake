using System;
using Aqua.Profile;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class ModelingUI : MonoBehaviour
    {
        #region Inspector

        [SerializeField] public ConceptMapUI ConceptMap = null;
        [SerializeField] public ScenarioPanelUI ScenarioPanel = null;

        #endregion // Inspector

        [NonSerialized] private ModelingScenarioData m_Scenario;
        [NonSerialized] private UniversalModelState m_UniversalModel;

        public void Awake()
        {
            ConceptMap.OnGraphUpdated += (s) => UpdateScenarioPanel();
        }
        
        public void PopulateMap(BestiaryData inPlayerData, UniversalModelState inModelState)
        {
            m_UniversalModel = inModelState;

            ConceptMap.SetInitialFacts(inPlayerData.GraphedFacts(), inModelState);
            ScenarioPanel.SetUniversalModel(inModelState);
        }

        public void SetScenario(ModelingScenarioData inScenario, bool inbOverride)
        {
            m_Scenario = inScenario;
            ScenarioPanel.SetScenario(inScenario, inbOverride);
            UpdateScenarioReady();
        }

        public void UpdateScenarioPanel()
        {
            ScenarioPanel.UpdateCritterIcons();
            UpdateScenarioReady();
        }

        private void UpdateScenarioReady()
        {
            if (!m_Scenario)
                return;
            
            bool bIsReady = false;
            if (m_UniversalModel.UngraphedFactCount() == 0)
            {
                foreach(var critter in m_Scenario.Actors())
                {
                    if (m_UniversalModel.IsCritterGraphed(critter.Id))
                    {
                        bIsReady = true;
                        break;
                    }
                }
            }

            ScenarioPanel.SetSimulationReady(bIsReady);
        }
    }
}