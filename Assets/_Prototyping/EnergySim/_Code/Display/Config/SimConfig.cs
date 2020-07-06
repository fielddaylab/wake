using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class SimConfig : MonoBehaviour
    {
        #region Inspector

        [Header("Top Bar")]

        [SerializeField] private Button m_ConfigButtion = null;

        [Header("Panel")]

        [SerializeField] private CanvasGroup m_PanelGroup = null;
        [SerializeField] private ConfigTab m_ScenarioTab = null;
        [SerializeField] private ConfigTab m_RulesTab = null;
        [SerializeField] private ScenarioConfigPanel m_ScenarioPanel = null;
        [SerializeField] private RulesConfigPanel m_RulesPanel = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Open;
        
        [NonSerialized] private ScenarioPackage m_Scenario;
        [NonSerialized] private ISimDatabase m_Database;

        public bool IsPaused() { return m_Open; }

        #region Unity Events

        private void Awake()
        {
            m_ConfigButtion.onClick.AddListener(OnConfigClicked);
            m_ScenarioTab.Button.onClick.AddListener(OnScenarioClicked);
            m_RulesTab.Button.onClick.AddListener(OnRulesClicked);
        }

        #endregion // Unity Events

        public void Initialize(ScenarioPackage inScenario, ISimDatabase inDatabase)
        {
            m_Scenario = inScenario;
            m_Database = inDatabase;

            m_ScenarioTab.SetSelected(true);
            m_RulesTab.SetSelected(false);
        }

        #region Listeners

        private void OnConfigClicked()
        {
            if (m_Open)
            {
                m_PanelGroup.gameObject.SetActive(false);
                m_Open = false;
            }
            else
            {
                m_PanelGroup.gameObject.SetActive(true);
                m_Open = true;

                m_ScenarioPanel.Populate(m_Scenario, m_Database);
                m_RulesPanel.Populate(m_Database);
            }
        }

        private void OnScenarioClicked()
        {
            m_ScenarioTab.SetSelected(true);
            m_RulesTab.SetSelected(false);
        }

        private void OnRulesClicked()
        {
            m_ScenarioTab.SetSelected(false);
            m_RulesTab.SetSelected(true);
        }

        #endregion // Listeners
    }
}