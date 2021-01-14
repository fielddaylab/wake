using System;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class SimEditPanel : BasePanel
    {
        #region Inspector

        [Header("Tabs")]
        [SerializeField] private EditPanelTab[] m_Tabs = null;

        [Header("Buttons")]
        [SerializeField] private Button m_ResumeButton = null;
        [SerializeField] private Button m_RestartButton = null;
        [SerializeField] private Button m_ExportButton = null;

        #endregion // Inspector

        [NonSerialized] private ScenarioPackage m_Scenario;
        [NonSerialized] private ISimDatabase m_Database;
        [NonSerialized] private bool m_RegenScenarioRulesRequested;

        public event Action OnRuleParametersChanged;
        public event Action OnRequestLevelRestart;

        #region Unity Events

        protected override void Start()
        {
            base.Start();

            m_ResumeButton.onClick.AddListener(HandleResumeClicked);
            m_RestartButton.onClick.AddListener(HandleRestartClicked);
            m_ExportButton.onClick.AddListener(HandleExportClicked);
        }

        #endregion // Unity Events

        public void Display(ScenarioPackage inPackage, ISimDatabase inDatabase)
        {
            m_Scenario = inPackage;
            m_Database = inDatabase;

            Show();
        }

        #region Handlers

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            foreach(var tab in m_Tabs)
            {
                tab.Initialize(m_Scenario, m_Database, HandleRuleRegen, HandleScenarioChanged);
            }
        }

        protected override void OnHide(bool inbInstant)
        {
            RegenScenarioRules();
            base.OnHide(inbInstant);
        }

        private void HandleExportClicked()
        {
            Export();
        }

        private void HandleRestartClicked()
        {
            OnRequestLevelRestart?.Invoke();
            Hide();
        }

        private void HandleResumeClicked()
        {
            Hide();
        }

        private void HandleRuleRegen()
        {
            HandleScenarioChanged();
        }

        private void HandleScenarioChanged()
        {
            m_RegenScenarioRulesRequested = true;
            OnRuleParametersChanged?.Invoke();
        }

        private void RegenScenarioRules()
        {
            if (!m_RegenScenarioRulesRequested)
                return;

            m_RegenScenarioRulesRequested = false;
        }

        #endregion // Handlers

        #region Export

        private void Export()
        {
            RegenScenarioRules();

            DateTime now = DateTime.UtcNow;
            long nowLong = now.ToFileTimeUtc();
            m_Scenario.Header.LastUpdated = nowLong;

            string exported = ScenarioPackage.Export(m_Scenario);
            QueryParams urlParams = new QueryParams();
            urlParams.Set(SimLoader.Param_ScenarioData, exported);

            string url = GetBaseURL() + urlParams.Encode();
            GUIUtility.systemCopyBuffer = url;

            // #if !UNITY_EDITOR
            Application.OpenURL(url);
            // #endif // !UNITY_EDITOR

            Debug.LogFormat("Exported scenario to '{0}'", url);
        }

        static private string GetBaseURL()
        {
#if !UNITY_EDITOR
            string baseUrl = Application.absoluteURL;
            int queryIdx = baseUrl.IndexOf('?');
            if (queryIdx >= 0)
            {
                baseUrl = baseUrl.Substring(0, queryIdx);
            }
            return baseUrl;
#else
            return "http://localhost/";
#endif // UNITY_EDITOR
        }

        #endregion // Export
    }
}