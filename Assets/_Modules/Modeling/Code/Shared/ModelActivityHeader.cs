using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class ModelActivityHeader : MonoBehaviour {
        #region Inspector

        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private ModelPhaseToggle m_EcosystemToggle = null;
        [SerializeField] private ModelPhaseToggle m_ConceptToggle = null;
        [SerializeField] private ModelPhaseToggle m_SyncToggle = null;
        [SerializeField] private ModelPhaseToggle m_PredictToggle = null;
        [SerializeField] private ModelPhaseToggle m_InterveneToggle = null;

        #endregion // Inspector

        public Action<ModelPhases> OnPhaseChanged;

        private void Awake() {
            m_EcosystemToggle.Toggle.onValueChanged.AddListener((b) => OnToggleUpdated(b, ModelPhases.Ecosystem, m_EcosystemToggle));
            m_ConceptToggle.Toggle.onValueChanged.AddListener((b) => OnToggleUpdated(b, ModelPhases.Concept, m_ConceptToggle));
            m_SyncToggle.Toggle.onValueChanged.AddListener((b) => OnToggleUpdated(b, ModelPhases.Sync, m_SyncToggle));
            m_PredictToggle.Toggle.onValueChanged.AddListener((b) => OnToggleUpdated(b, ModelPhases.Predict, m_PredictToggle));
            m_InterveneToggle.Toggle.onValueChanged.AddListener((b) => OnToggleUpdated(b, ModelPhases.Intervene, m_InterveneToggle));
        }

        private void OnToggleUpdated(bool state, ModelPhases phases, ModelPhaseToggle toggle) {
            toggle.Label.font = Assets.Font(state ? TMPro.FontWeight.SemiBold : TMPro.FontWeight.Regular);
            if (state && OnPhaseChanged != null) {
                OnPhaseChanged(phases);
            }
        }

        public void SetSelected(ModelPhases phase) {
            switch(phase) {
                case ModelPhases.Ecosystem: {
                    m_EcosystemToggle.Toggle.isOn = true;
                    break;
                }
                case ModelPhases.Concept: {
                    m_ConceptToggle.Toggle.isOn = true;
                    break;
                }
                case ModelPhases.Sync: {
                    m_SyncToggle.Toggle.isOn = true;
                    break;
                }
                case ModelPhases.Predict: {
                    m_PredictToggle.Toggle.isOn = true;
                    break;
                }
                case ModelPhases.Intervene: {
                    m_InterveneToggle.Toggle.isOn = true;
                    break;
                }
            }
        }

        public void UpdateAllowedMask(ModelPhases mask) {
            m_EcosystemToggle.Toggle.interactable = (mask & ModelPhases.Ecosystem) != 0;
            m_ConceptToggle.Toggle.interactable = (mask & ModelPhases.Concept) != 0;
            m_SyncToggle.Toggle.interactable = (mask & ModelPhases.Sync) != 0;
            m_PredictToggle.Toggle.interactable = (mask & ModelPhases.Predict) != 0;
            m_InterveneToggle.Toggle.interactable = (mask & ModelPhases.Intervene) != 0;
        }
    
        public void SetInputActive(bool active) {
            m_Group.blocksRaycasts = active;
        }
    }
}