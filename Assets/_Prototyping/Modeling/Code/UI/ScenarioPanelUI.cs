using System;
using System.Collections;
using Aqua;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class ScenarioPanelUI : MonoBehaviour
    {
        #region Inspector

        [Header("Panel")]
        [SerializeField] private RectTransform m_Root = null;
        [SerializeField] private LocText m_ScenarioName = null;
        [SerializeField] private LocText m_DescriptionText = null;

        [Header("Simulator Page")]
        [SerializeField] private RectTransform m_MissingEnvPage = null;
        [SerializeField] private Image m_MissingEnvIcon = null;
        [SerializeField] private RectTransform m_AlreadyCompletePage = null;
        [SerializeField] private RectTransform m_CrittersPage = null;
        [SerializeField] private Image[] m_CritterIcons = null;
        [SerializeField] private Button m_SimulateButton = null;

        #endregion // Inspector

        [NonSerialized] private bool m_SimulationAllowed;

        [NonSerialized] private ModelingScenarioData m_Scenario = null;
        [NonSerialized] private bool m_OverrideScenarioAccess = false;

        public Action OnSimulateSelect;

        private void Awake()
        {
            m_SimulateButton.onClick.AddListener(OnSimulateClick);
        }

        public void SetScenario(ModelingScenarioData inScenario, bool inbOverride)
        {
            m_Scenario = inScenario;
            m_OverrideScenarioAccess = inbOverride;

            m_Root.gameObject.SetActive(inScenario != null);
            m_SimulateButton.interactable = inScenario != null;

            Populate(m_Scenario, m_OverrideScenarioAccess);
        }

        public void SetSimulationReady(bool inbReady)
        {
            m_SimulateButton.interactable = inbReady && m_SimulationAllowed;
        }

        public bool CanSimulate()
        {
            return m_SimulationAllowed;
        }

        #region Handlers

        private void OnSimulateClick()
        {
            OnSimulateSelect?.Invoke();
        }

        #endregion // Handlers

        #region Populate

        private void Populate(ModelingScenarioData inScenario, bool inbOverride)
        {
            m_ScenarioName.SetText(inScenario.TitleId());
            m_DescriptionText.SetText(inScenario.DescId());
            
            ConfigureSimulatorPage(inScenario, inbOverride);

            m_SimulateButton.interactable = false;
        }

        private void ConfigureSimulatorPage(ModelingScenarioData inScenario, bool inbOverride)
        {
            m_MissingEnvPage.gameObject.SetActive(false);
            m_AlreadyCompletePage.gameObject.SetActive(false);
            m_CrittersPage.gameObject.SetActive(false);

            if (inbOverride)
            {
                m_SimulationAllowed = true;
                m_CrittersPage.gameObject.SetActive(true);
                DisplayCritterIcons(inScenario);
                return;
            }
            
            StringHash32 modelId = inScenario.BestiaryModelId();
            bool bCompleted = Services.Data.Profile.Bestiary.HasFact(modelId);
            if (bCompleted)
            {
                m_SimulationAllowed = false;
                m_AlreadyCompletePage.gameObject.SetActive(true);
                return;
            }

            bool bHasEnvironment = Services.Data.Profile.Bestiary.HasEntity(inScenario.Environment().Id());
            if (!bHasEnvironment)
            {
                m_SimulationAllowed = false;
                m_MissingEnvIcon.sprite = inScenario.Environment().Icon();
                m_MissingEnvPage.gameObject.SetActive(true);
                return;
            }

            m_CrittersPage.gameObject.SetActive(true);
            m_SimulationAllowed = true;
            DisplayCritterIcons(inScenario);
        }

        private void DisplayCritterIcons(ModelingScenarioData inScenario)
        {
            var critters = inScenario.Critters();
            for(int i = 0; i < critters.Length; ++i)
            {
                m_CritterIcons[i].sprite = critters[i].Icon();
                m_CritterIcons[i].gameObject.SetActive(true);
            }

            for(int i = critters.Length; i < m_CritterIcons.Length; ++i)
            {
                m_CritterIcons[i].gameObject.SetActive(false);
            }
        }

        #endregion // Populate
    }
}