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
        [SerializeField] private ConfigTab[] m_Tabs = null;
        [SerializeField] private ToggleGroup m_TabGroup = null;
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
            foreach(var tab in m_Tabs)
            {
                tab.RegisterGroup(m_TabGroup);
            }

            if (m_Tabs.Length > 0)
            {
                m_Tabs[0].Select();
            }
        }

        #endregion // Unity Events

        public void Initialize(ScenarioPackage inScenario, ISimDatabase inDatabase)
        {
            m_Scenario = inScenario;
            m_Database = inDatabase;
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

        #endregion // Listeners
    }
}