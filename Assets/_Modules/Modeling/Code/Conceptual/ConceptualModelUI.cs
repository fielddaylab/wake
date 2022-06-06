using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class ConceptualModelUI : BasePanel, ISceneLoadHandler {

        private const WorldFilterMask DefaultFilterMask = WorldFilterMask.Organism | WorldFilterMask.Missing | WorldFilterMask.AnyBehavior | WorldFilterMask.AnyWaterChem;

        public delegate IEnumerator ImportDelegate();

        #region Inspector

        [SerializeField] private Button m_ImportButton = null;
        [SerializeField] private Button m_ExportButton = null;
        [SerializeField] private PointerListener m_InspectRegion = null;
        [SerializeField] private ConceptualFilterBox m_FilterBox = null;

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
        [SerializeField] private TextId m_ImportReadyLabel = null;
        [SerializeField] private TextId m_ExportReadyLabel = null;

        #endregion // Inspector

        private ModelState m_State;
        private ModelProgressInfo m_ProgressionInfo;
        private Routine m_ImportRoutine;

        public ImportDelegate OnRequestImport;
        public Action OnRequestExport;

        [NonSerialized] private WorldFilterMask m_CurrentFilter = DefaultFilterMask;

        public void SetData(ModelState state, ModelProgressInfo info) {
            m_State = state;
            m_ProgressionInfo = info;

            m_State.OnGraphChanged += OnGraphChanged;
        }

        protected override void Awake() {
            base.Awake();
            m_ImportButton.onClick.AddListener(OnImportClicked);
            m_ExportButton.onClick.AddListener(OnExportClicked);
            m_InspectRegion.onClick.AddListener(OnInspectClicked);

            m_ImportFader.SetActive(false);
            m_ImportGroup.SetActive(false);

            Services.Events.Register(GameEvents.BestiaryUpdated, OnShouldRefreshButtons, this)
                .Register(GameEvents.SiteDataUpdated, OnShouldRefreshButtons, this);
            Services.Events.Dispatch(ModelingConsts.Event_Begin_Model);
        }

        private void OnDestroy() {
            Services.Events?.Dispatch(ModelingConsts.Event_End_Model);
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
            m_ImportRoutine.Tick();
        }

        private void OnExportClicked() {
            OnRequestExport?.Invoke();
            UpdateButtons();
        }

        private void OnInspectClicked(PointerEventData evt) {
            GameObject press = evt.pointerPressRaycast.gameObject;
            if (press.TryGetComponent(out ModelConnectionDisplay connection)) {
                if (connection.Fact) {
                    if (connection.Fact2) {
                        m_State.Display.FactsPopup(new BFBase[] { connection.Fact, connection.Fact2 });
                    } else {
                        m_State.Display.FactsPopup(new BFBase[] { connection.Fact });
                    }
                }
            } else if (press.TryGetComponent(out ModelOrganismDisplay organism)) {
                m_State.Display.TextPopup(organism.Organism.CommonName());
            } else if (press.TryGetComponent(out ModelWaterPropertyDisplay property)) {
                m_State.Display.TextPopup(property.Property.LabelId(), property.Property.Color());
            } else if (press.TryGetComponent(out ModelAttachmentDisplay attachment)) {
                if (attachment.Fact) {
                    m_State.Display.FactsPopup(new BFBase[] { attachment.Fact });
                } else {
                    switch (attachment.Missing) {
                        case MissingFactTypes.Repro: {
                                m_State.Display.TextPopup("modeling.missing.reproduction", AQColors.Red);
                                break;
                            }
                        case MissingFactTypes.Repro_Stressed: {
                                m_State.Display.TextPopup("modeling.missing.reproduction.stressed", AQColors.Red);
                                break;
                            }

                        case MissingFactTypes.Eat: {
                                m_State.Display.TextPopup("modeling.missing.eat", AQColors.Red);
                                break;
                            }
                        case MissingFactTypes.Eat_Stressed: {
                                m_State.Display.TextPopup("modeling.missing.eat.stressed", AQColors.Red);
                                break;
                            }

                        case MissingFactTypes.WaterChem: {
                                m_State.Display.TextPopup("modeling.missing.chem", AQColors.Red);
                                break;
                            }
                        case MissingFactTypes.WaterChem_Stressed: {
                                m_State.Display.TextPopup("modeling.missing.chem.stressed", AQColors.Red);
                                break;
                            }

                        case MissingFactTypes.Parasite: {
                                m_State.Display.TextPopup("modeling.missing.parasite", AQColors.Red);
                                break;
                            }

                        case MissingFactTypes.PopulationHistory: {
                                m_State.Display.TextPopup("modeling.missing.populationHistory", AQColors.Red);
                                break;
                            }
                        case MissingFactTypes.WaterChemHistory: {
                                m_State.Display.TextPopup("modeling.missing.waterChemHistory", AQColors.Red);
                                break;
                            }
                    }
                }
            }
        }

        private void OnGraphChanged(WorldFilterMask graphed) {
            if (m_FilterBox.Toggle.isOn) {
                UpdateFilterEnabled(m_State.Conceptual.GraphedMask, m_FilterBox);
            }
        }

        #endregion // Callbacks

        #region Sequences

        private IEnumerator ImportSequence() {
            using (Script.DisableInput()) {
                m_ImportFader.SetActive(true);
                m_ImportTolerancesText.SetActive(false);
                m_ImportOrganismsText.SetActive(false);
                m_ImportBehaviorsText.SetActive(false);
                m_ImportHistoricalText.SetActive(false);
                m_ImportCompletedText.SetActive(false);
                m_ImportGroup.SetActive(true);
                m_State.Display.Status(null);
                yield return null;

                bool hadOrganisms = m_State.Conceptual.PendingEntities.Count > 0;
                bool hadTolerances = false;
                bool hadBehaviors = false;
                bool hadHistorical = false;

                foreach (var fact in m_State.Conceptual.PendingFacts) {
                    switch (fact.Type) {
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
                Services.Events.Dispatch(ModelingConsts.Event_Concept_Updated, m_State.Conceptual.Status);
                Services.Script.TriggerResponse(ModelingConsts.Trigger_ConceptUpdated);
            }
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

        #region Filters

        private void OnFilterExpandContract(bool expanded) {
            UpdateFilterEnabled(expanded ? m_State.Conceptual.GraphedMask : 0, m_FilterBox);
            m_FilterBox.Close.gameObject.SetActive(expanded);
        }

        private void OnFilterStateChanged(ConceptualFilterLine line, bool state) {
            if (state) {
                m_CurrentFilter |= line.Mask;
            } else {
                m_CurrentFilter &= ~line.Mask;
            }

            UpdateFilterToggle(m_CurrentFilter, m_State.Conceptual.GraphedMask, m_FilterBox);
            m_State.Display.FilterNodes(m_CurrentFilter);
        }

        static private void UpdateFilterToggle(WorldFilterMask current, WorldFilterMask valid, ConceptualFilterBox box) {
            WorldFilterMask a, m;
            ConceptualFilterLine line;
            for (int i = 0; i < box.Lines.Length; i++) {
                line = box.Lines[i];
                a = current & (m = line.Mask) & valid;
                line.Sync(a != 0);
            }
        }

        static private void UpdateFilterEnabled(WorldFilterMask current, ConceptualFilterBox box) {
            WorldFilterMask a;
            ConceptualFilterLine line;
            for (int i = 0; i < box.Lines.Length; i++) {
                line = box.Lines[i];
                a = current & line.Mask;
                line.gameObject.SetActive(a != 0);
            }
        }

        #endregion // Filters

        protected override void OnShow(bool inbInstant) {
            UpdateButtons();
            Services.Events.Queue(ModelingConsts.Event_Concept_Started);
            Services.Script.TriggerResponse(ModelingConsts.Trigger_ConceptStarted);

            m_State.Display.FilterNodes(m_CurrentFilter);
        }

        protected override void OnHide(bool inbInstant) {
            if (m_InspectRegion) {
                m_InspectRegion.enabled = false;
            }
        }

        private void UpdateButtons() {
            m_ExportButton.gameObject.SetActive(m_State.Conceptual.Status == ConceptualModelState.StatusId.ExportReady);
            m_ImportButton.gameObject.SetActive(m_State.Conceptual.Status == ConceptualModelState.StatusId.PendingImport);

            switch (m_State.Conceptual.Status) {
                case ConceptualModelState.StatusId.ExportReady: {
                        m_State.Display.Status(m_ExportReadyLabel);
                        m_InspectRegion.enabled = false;
                        m_FilterBox.gameObject.SetActive(false);
                        break;
                    }

                case ConceptualModelState.StatusId.PendingImport: {
                        m_State.Display.Status(m_ImportReadyLabel);
                        m_InspectRegion.enabled = false;
                        m_FilterBox.gameObject.SetActive(false);
                        break;
                    }

                case ConceptualModelState.StatusId.UpToDate: {
                        m_State.Display.Status(default);
                        m_InspectRegion.enabled = true;
                        m_FilterBox.gameObject.SetActive((m_FilterBox.RepresentedMask & m_State.Conceptual.GraphedMask) != 0);
                        break;
                    }

                case ConceptualModelState.StatusId.MissingData: {
                        switch (m_State.Conceptual.MissingReasons) {
                            case ModelMissingReasons.Organisms:
                            case ModelMissingReasons.Behaviors | ModelMissingReasons.Organisms: {
                                    m_State.Display.Status(m_MissingOrganismsLabel, AQColors.Red);
                                    break;
                                }
                            case ModelMissingReasons.Behaviors: {
                                    m_State.Display.Status(m_MissingBehaviorsLabel, AQColors.Red);
                                    break;
                                }
                        }
                        m_InspectRegion.enabled = true;
                        m_FilterBox.gameObject.SetActive((m_FilterBox.RepresentedMask & m_State.Conceptual.GraphedMask) != 0);
                        break;
                    }
            }
        }

        private void OnShouldRefreshButtons() {
            if (IsShowing() && !m_ImportRoutine) {
                UpdateButtons();
            }
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            for (int i = 0; i < m_FilterBox.Lines.Length; i++) {
                ConceptualFilterLine line = m_FilterBox.Lines[i];
                line.Toggle.onValueChanged.AddListener((b) => OnFilterStateChanged(line, b));
            }

            m_FilterBox.Toggle.onValueChanged.AddListener(OnFilterExpandContract);
            m_FilterBox.Close.onClick.AddListener(() => m_FilterBox.Toggle.isOn = false);

            UpdateFilterEnabled(0, m_FilterBox);
            m_FilterBox.Toggle.SetIsOnWithoutNotify(false);
            m_FilterBox.Close.gameObject.SetActive(false);
        }
    }
}