using System;
using System.Collections;
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

        [SerializeField] private ConceptMapUI m_ConceptMap = null;
        [SerializeField] private ScenarioPanelUI m_ScenarioPanel = null;

        #endregion // Inspector

        public Action OnSimulateClick;
        
        public void Populate(BestiaryData inPlayerData)
        {
            m_ConceptMap.SetInitialFacts(inPlayerData.GraphedFacts());
        }

        public void SetScenario(ModelingScenarioData inScenario, bool inbOverride)
        {
            m_ScenarioPanel.SetScenario(inScenario, inbOverride);
        }
    }
}