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
    public class ScenarioPanelUI : BasePanel
    {
        private enum Page
        {
            Info,
            Simulator,

            COUNT
        }

        #region Inspector

        [Header("Panel")]
        [SerializeField] private LocText m_ScenarioName = null;
        
        [Header("Controls")]
        [SerializeField] private Button m_ConfigureButton = null;
        [SerializeField] private Button m_PrevButton = null;
        [SerializeField] private Button m_NextButton = null;

        [Header("Info Page")]
        [SerializeField] private RectTransform m_InfoPage = null;
        [SerializeField] private LocText m_DescriptionText = null;

        [Header("Simulator Page")]
        [SerializeField] private RectTransform m_SimulatorPage = null;
        [SerializeField] private RectTransform m_MissingEnvPage = null;
        [SerializeField] private Image m_MissingEnvIcon = null;
        [SerializeField] private RectTransform m_AlreadyCompletePage = null;
        [SerializeField] private RectTransform m_CrittersPage = null;
        [SerializeField] private Image[] m_CritterIcons = null;
        [SerializeField] private Button m_SimulateButton = null;

        #endregion // Inspector

        [NonSerialized] private Page m_CurrentPage;
        [NonSerialized] private bool m_SimulationAllowed;

        [NonSerialized] private ModelingScenarioData m_Scenario = null;
        [NonSerialized] private bool m_OverrideScenarioAccess = false;

        protected override void Awake()
        {
            base.Awake();

            m_ConfigureButton.onClick.AddListener(OnConfigureClick);
            m_PrevButton.onClick.AddListener(OnPageToggle);
            m_NextButton.onClick.AddListener(OnPageToggle);
        }

        public void SetScenario(ModelingScenarioData inScenario, bool inbOverride)
        {
            m_Scenario = inScenario;
            m_OverrideScenarioAccess = inbOverride;

            m_ConfigureButton.interactable = inScenario != null;
        }

        public void SetSimulationReady(bool inbReady)
        {
            m_SimulateButton.interactable = inbReady && m_SimulationAllowed;
        }

        #region Handlers

        private void OnConfigureClick()
        {
            if (IsShowing())
            {
                Hide();
            }
            else
            {
                Populate(m_Scenario, m_OverrideScenarioAccess);
                Show();
            }
        }

        private void OnPageToggle()
        {
            m_CurrentPage = (Page) (((int) m_CurrentPage + 1) % (int) Page.COUNT);
            DisplayPage(m_CurrentPage);
        }

        #endregion // Handlers

        #region Populate

        private void Populate(ModelingScenarioData inScenario, bool inbOverride)
        {
            m_CurrentPage = Page.Info;
            m_ScenarioName.SetText(inScenario.TitleId());
            m_DescriptionText.SetText(inScenario.DescId());
            
            ConfigureSimulatorPage(inScenario, inbOverride);
            DisplayPage(m_CurrentPage);

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

        private void DisplayPage(Page inPage)
        {
            switch(inPage)
            {
                case Page.Info:
                    {
                        m_SimulatorPage.gameObject.SetActive(false);
                        m_InfoPage.gameObject.SetActive(true);
                        break;
                    }

                case Page.Simulator:
                    {
                        m_InfoPage.gameObject.SetActive(false);
                        m_SimulatorPage.gameObject.SetActive(true);
                        break;
                    }
            }
        }

        #endregion // Populate
    }
}