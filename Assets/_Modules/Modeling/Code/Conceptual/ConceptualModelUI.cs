using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class ConceptualModelUI : BasePanel {

        public delegate IEnumerator ImportDelegate();

        #region Inspector

        [SerializeField] private GameObject m_MissingData = null;
        [SerializeField] private LocText m_MissingDataText = null;
        [SerializeField] private Button m_ImportButton = null;
        [SerializeField] private Button m_ExportButton = null;

        [Header("Import")]
        [SerializeField] private GameObject m_ImportGroup = null;
        [SerializeField] private GameObject m_ImportFader = null;
        [SerializeField] private GameObject m_ImportOrganismsText = null;
        [SerializeField] private GameObject m_ImportTolerancesText = null;
        [SerializeField] private GameObject m_ImportBehaviorsText = null;
        [SerializeField] private GameObject m_ImportHistoricalText = null;
        [SerializeField] private GameObject m_ImportCompletedText = null;

        [Header("Settings")]
        [SerializeField] private TextId m_MissingOrganismsLabel = null;
        [SerializeField] private TextId m_MissingBehaviorsLabel = null;
        [SerializeField] private TextId m_MissingOrganismsBehaviorsLabel = null;

        #endregion // Inspector

        private ModelState m_State;
        private ModelProgressInfo m_ProgressionInfo;
        private Routine m_ImportRoutine;

        public ImportDelegate OnRequestImport;
        public Action OnRequestExport;

        public void SetData(ModelState state, ModelProgressInfo info) {
            m_State = state;
            m_ProgressionInfo = info;
        }

        protected override void Awake() {
            base.Awake();
            m_ImportButton.onClick.AddListener(OnImportClicked);
            m_ExportButton.onClick.AddListener(OnExportClicked);

            m_ImportFader.SetActive(false);
            m_ImportGroup.SetActive(false);

            Services.Events.Register(GameEvents.BestiaryUpdated, OnShouldRefreshButtons, this)
                .Register(GameEvents.SiteDataUpdated, OnShouldRefreshButtons, this);
            Services.Events.Dispatch(ModelingConsts.Event_Modeling_Start);
        }

        private void OnDestroy() {
            Services.Events.Dispatch(ModelingConsts.Event_Modeling_End);
            Services.Events?.DeregisterAll(this);
        }

        #region BasePanel

        protected override void InstantTransitionToShow() {
            CanvasGroup.Show();
        }

        protected override void InstantTransitionToHide() {
            CanvasGroup.Hide();
        }

        protected override IEnumerator TransitionToShow() {
            return CanvasGroup.Show(0.2f);
        }

        protected override IEnumerator TransitionToHide() {
            return CanvasGroup.Hide(0.2f);
        }

        #endregion // BasePanel

        #region Callbacks

        private void OnImportClicked() {
            m_ImportButton.gameObject.SetActive(false);
            m_ImportRoutine = Routine.Start(this, ImportSequence());
            m_ImportRoutine.TryManuallyUpdate(0);
        }

        private void OnExportClicked() {
            OnRequestExport?.Invoke();
            UpdateButtons();
        }

        #endregion // Callbacks

        #region Sequences

        private IEnumerator ImportSequence() {
            Services.Input.PauseAll();
            m_ImportFader.SetActive(true);
            m_ImportTolerancesText.SetActive(false);
            m_ImportOrganismsText.SetActive(false);
            m_ImportBehaviorsText.SetActive(false);
            m_ImportHistoricalText.SetActive(false);
            m_ImportCompletedText.SetActive(false);
            m_ImportGroup.SetActive(true);
            yield return null;

            bool hadOrganisms = m_State.Conceptual.PendingEntities.Count > 0;
            bool hadTolerances = false;
            bool hadBehaviors = false;
            bool hadHistorical = false;

            foreach(var fact in m_State.Conceptual.PendingFacts) {
                switch(fact.Type) {
                    case BFTypeId.State: {
                        hadTolerances = true;
                        break;
                    }

                    case BFTypeId.Population:
                    case BFTypeId.PopulationHistory:
                    case BFTypeId.WaterPropertyHistory: {
                        hadHistorical = true;
                        break;
                    }

                    default: {
                        hadBehaviors = true;
                        break;
                    }
                }
            }

            yield return 1f;

            IEnumerator requestProcess = OnRequestImport?.Invoke();
            yield return Routine.Combine(
                requestProcess, ImportTextSequence(hadOrganisms, hadTolerances, hadBehaviors, hadHistorical)
            );

            yield return 0.2f;
            m_ImportCompletedText.SetActive(true);
            yield return 1f;

            m_ImportGroup.SetActive(false);
            m_ImportFader.SetActive(false);
            UpdateButtons();
            Services.Input.ResumeAll();
            Services.Events.Dispatch(ModelingConsts.Event_Concept_Updated, m_State.Conceptual.Status);
            Services.Script.TriggerResponse(ModelingConsts.Trigger_ConceptUpdated);
        }

        private IEnumerator ImportTextSequence(bool hadOrganisms, bool hadTolerances, bool hadBehaviors, bool hadHistorical) {
            if (hadOrganisms) {
                m_ImportOrganismsText.SetActive(true);
                yield return 0.3f;
            }

            if (hadTolerances) {
                m_ImportTolerancesText.SetActive(true);
                yield return 0.3f;
            }

            if (hadBehaviors) {
                m_ImportBehaviorsText.SetActive(true);
                yield return 0.3f;
            }

            if (hadHistorical) {
                m_ImportHistoricalText.SetActive(true);
                yield return 0.3f;
            }
        }

        #endregion // Sequences

        protected override void OnShow(bool inbInstant) {
            UpdateButtons();
            Services.Events.QueueForDispatch(ModelingConsts.Event_Model_Begin);
            Services.Script.TriggerResponse(ModelingConsts.Trigger_ConceptStarted);
        }

        private void UpdateButtons() {
            m_ExportButton.gameObject.SetActive(m_State.Conceptual.Status == ConceptualModelState.StatusId.ExportReady);
            m_MissingData.SetActive(m_State.Conceptual.Status == ConceptualModelState.StatusId.MissingData);
            m_ImportButton.gameObject.SetActive(m_State.Conceptual.Status == ConceptualModelState.StatusId.PendingImport);

            switch(m_State.Conceptual.MissingReasons) {
                case ModelMissingReasons.Organisms: {
                    m_MissingDataText.SetText(m_MissingOrganismsLabel);
                    break;
                }
                case ModelMissingReasons.Behaviors: {
                    m_MissingDataText.SetText(m_MissingBehaviorsLabel);
                    break;
                }
                case ModelMissingReasons.Behaviors | ModelMissingReasons.Organisms: {
                    m_MissingDataText.SetText(m_MissingOrganismsBehaviorsLabel);
                    break;
                }
            }
        }

        private void OnShouldRefreshButtons() {
            if (IsShowing() && !m_ImportRoutine) {
                UpdateButtons();
            }
        }
    }
}