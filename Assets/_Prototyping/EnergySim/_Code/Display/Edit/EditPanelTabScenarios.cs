using System.Collections.Generic;
using BeauUtil;
using ProtoCP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class EditPanelTabScenarios : EditPanelTab
    {
        [Header("Edit Panel Tab Scenarios Dependencies")]
        [SerializeField] private SimLoader m_SimLoader = null;
        [SerializeField] private Button m_ReloadButton = null;
        
        private QueryParams scenarioParams = new QueryParams();
        private CPLabeledValue[] values = null;

        private Dictionary<string, string> ids = new Dictionary<string, string>();
        private string[] scenarioIds = null;
        private string[] databaseIds = null;
        private string scenarioToLoad;
        private string databaseToLoad;

        protected override void Populate()
        {
            m_PropertyBox.BeginControls();
            {
                m_PropertyBox.BeginGroup("scenarioIds", "Ecosystem To Use");
                {
                    InitializeValues();

                    CPEnumSpinner.Configuration scenarioIdConfig = new CPEnumSpinner.Configuration()
                    {
                        Name = "Ecosystem Name",
                        Values = values,
                        DefaultValue = null,
                        ValueType = typeof(string),
                        Get = () => databaseToLoad,
                        Set = (v) => { 
                            databaseToLoad = (string)v; 
                            scenarioToLoad = ids[databaseToLoad];
                            scenarioParams.Set("scenarioId", scenarioToLoad);
                            m_ReloadButton.onClick.AddListener(() => Services.State.LoadScene(SceneManager.GetActiveScene().name, scenarioParams));
                        }
                    };
                    m_PropertyBox.EnumField("scenarioIds", scenarioIdConfig);
                }
                m_PropertyBox.EndGroup();
            }
            m_PropertyBox.EndControls();
        }

        private void InitializeValues()
        {
            if (scenarioIds == null)
            {
                scenarioIds = m_SimLoader.GetScenarioIds();
                databaseIds = m_SimLoader.GetDatabaseIds();
            
                scenarioParams.Set("scenarioId", scenarioIds[0]);
                m_ReloadButton.onClick.AddListener(() => Services.State.LoadScene(SceneManager.GetActiveScene().name, scenarioParams));

                values = new CPLabeledValue[databaseIds.Length];
                for (int i = 0; i < databaseIds.Length; ++i)
                {
                    CPLabeledValue value = CPLabeledValue.Make(databaseIds[i], databaseIds[i]);
                    values[i] = value;
                    ids[databaseIds[i]] = scenarioIds[i];
                }
            }
        }
    }
}
