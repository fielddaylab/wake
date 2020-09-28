using System;
using BeauRoutine.Extensions;
using BeauUtil;
using ProtoCP;
using TMPro;
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
        private string[] scenarioIds = null;
        private CPLabeledValue[] values = null;
        private string scenarioToLoad;

        protected override void Populate()
        {
            m_PropertyBox.BeginControls();
            {
                m_PropertyBox.BeginGroup("scenarioIds", "ScenarioIds");
                {
                    InitializeValues();

                    CPEnumSpinner.Configuration scenarioIdConfig = new CPEnumSpinner.Configuration()
                    {
                        Name = "ScenarioId",
                        Values = values,
                        DefaultValue = null,
                        ValueType = typeof(string),
                        Get = () => scenarioToLoad,
                        Set = (v) => { 
                            scenarioToLoad = (string)v; 
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
            
                scenarioParams.Set("scenarioId", scenarioIds[0]);
                m_ReloadButton.onClick.AddListener(() => Services.State.LoadScene(SceneManager.GetActiveScene().name, scenarioParams));

                values = new CPLabeledValue[scenarioIds.Length];
                for (int i = 0; i < scenarioIds.Length; ++i)
                {
                    CPLabeledValue value = CPLabeledValue.Make(scenarioIds[i], scenarioIds[i]);
                    values[i] = value;
                }
            }
        }
    }
}
