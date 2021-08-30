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
        [SerializeField] private LocText m_ScenarioName = null;
        [SerializeField] private LocText m_DescriptionText = null;

        [Header("Simulator Page")]
        [SerializeField] private RectTransform m_MissingEnvPage = null;
        [SerializeField] private Image m_MissingEnvIcon = null;
        [SerializeField] private RectTransform m_AlreadyCompletePage = null;
        [SerializeField] private RectTransform m_CrittersPage = null;
        [SerializeField] private Image[] m_CritterIcons = null;
        [SerializeField] private Image[] m_CritterHistoryIcons = null;
        [SerializeField] private Sprite m_MissingCritterIcon = null;
        [SerializeField] private Button m_SimulateButton = null;

        #endregion // Inspector

        [NonSerialized] private bool m_SimulationAllowed;

        [NonSerialized] private ModelingScenarioData m_Scenario = null;
        [NonSerialized] private bool m_OverrideScenarioAccess = false;
        [NonSerialized] private UniversalModelState m_UniversalModel;

        public Action OnSimulateSelect;

        private void Awake()
        {
            m_SimulateButton.onClick.AddListener(OnSimulateClick);
        }

        public void SetScenario(ModelingScenarioData inScenario, bool inbOverride)
        {
            m_Scenario = inScenario;
            m_OverrideScenarioAccess = inbOverride;
            
            m_SimulateButton.interactable = inScenario != null;

            Populate(m_Scenario, m_OverrideScenarioAccess);
        }

        public void SetUniversalModel(UniversalModelState inModel)
        {
            m_UniversalModel = inModel;
        }

        public void SetSimulationReady(bool inbReady)
        {
            m_SimulateButton.interactable = inbReady && m_SimulationAllowed && m_UniversalModel.UngraphedFactCount() == 0;
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
            if (inScenario == null)
            {
                // TODO: Replace
                m_ScenarioName.SetText("No Scenario Loaded");
                m_DescriptionText.SetText(null);

                m_MissingEnvPage.gameObject.SetActive(false);
                m_AlreadyCompletePage.gameObject.SetActive(false);
                m_CrittersPage.gameObject.SetActive(false);
                return;
            }
            
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
                UpdateCritterIcons();
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
            UpdateCritterIcons();
        }

        public void UpdateCritterIcons()
        {
            if (!m_Scenario)
                return;
            
            var critters = m_Scenario.Actors();
            for(int i = 0; i < critters.Length; ++i)
            {
                bool bIsGraphed = m_UniversalModel.IsCritterGraphed(critters[i].Id);
                m_CritterIcons[i].sprite = m_UniversalModel.IsCritterGraphed(critters[i].Id) ? Services.Assets.Bestiary[critters[i].Id].Icon() : m_MissingCritterIcon;
                m_CritterHistoryIcons[i].gameObject.SetActive(bIsGraphed && ModelingUI.HasPopulationHistory(m_Scenario, i));
                m_CritterIcons[i].gameObject.SetActive(true);
            }

            for(int i = critters.Length; i < m_CritterIcons.Length; ++i)
            {
                m_CritterIcons[i].gameObject.SetActive(false);
                m_CritterHistoryIcons[i].gameObject.SetActive(false);
            }
        }

        #endregion // Populate
    }
}