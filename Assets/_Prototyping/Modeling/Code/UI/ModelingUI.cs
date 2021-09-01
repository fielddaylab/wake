using System;
using Aqua;
using Aqua.Profile;
using BeauUtil;
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
                var actors = m_Scenario.Actors();
                for(int i = 0; i < actors.Length; i++)
                {
                    var critter = actors[i];
                    if (m_UniversalModel.IsCritterGraphed(critter.Id) && HasPopulationHistory(m_Scenario, i))
                    {
                        bIsReady = true;
                        break;
                    }
                }
            }

            ScenarioPanel.SetSimulationReady(bIsReady);
        }

        static public bool HasPopulationHistory(ModelingScenarioData inScenario, int inCritterIndex)
        {
            StringHash32 factId = inScenario.PopulationHistoryFacts()[inCritterIndex];
            return factId.IsEmpty || Services.Data.Profile.Bestiary.HasFact(factId);
        }
    }
}