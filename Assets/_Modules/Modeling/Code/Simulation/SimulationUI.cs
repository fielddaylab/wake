using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using UnityEngine.UI;
using BeauUtil.Debugger;
using BeauUtil;
using TMPro;

namespace Aqua.Modeling {
    public unsafe class SimulationUI : BasePanel {
        #region Inspector

        [Header("Graphs")]
        [SerializeField] private SimLineGraph m_Graph = null;
        [SerializeField] private GameObject m_GraphFader = null;

        [Header("Sync")]
        [SerializeField] private CanvasGroup m_SyncInputGroup = null;
        [SerializeField] private DivergencePopup m_SyncDivergencePopup = null;
        [SerializeField] private Button m_SimulateButton = null;
        [SerializeField] private GameObject m_AccuracyDisplay = null;
        [SerializeField] private TickDisplay m_AccuracyTicks = null;
        [SerializeField] private GameObject m_SyncViewGroup = null;
        [SerializeField] private Toggle m_SyncViewNormalToggle = null;
        [SerializeField] private Toggle m_SyncViewFillToggle = null;
        
        [Header("Predict")]
        [SerializeField] private Button m_PredictButton = null;

        [Header("Intervene")]
        [SerializeField] private LayoutGroup m_InterveneButtonGroup = null;
        [SerializeField] private ActiveGroup m_InterveneAddToggleGroup = default;
        [SerializeField] private Toggle m_InterveneAddToggle = null;
        [SerializeField] private BestiaryAddPanel m_InterveneAddPanel = null;
        [SerializeField] private Button m_InterveneResetButton = null;
        [SerializeField] private Button m_InterveneRunButton = null;
        [SerializeField] private InlinePopupPanel m_IntervenePopup = null;

        [Header("Settings")]
        [SerializeField] private TextId m_MissingPopulationsLabel = default;
        [SerializeField] private TextId m_MissingWaterChemistryLabel = default;
        [SerializeField] private TextId m_MissingPopulationsWaterChemistryLabel = default;

        [Header("Global")]
        [SerializeField] private Button m_SaveButton = null;

        #endregion // Inspector

        [NonSerialized] private bool m_IsPredicting;
        [NonSerialized] private bool m_IsIntervention;
        [NonSerialized] private BaseInputLayer m_InputLayer;
        [NonSerialized] private int m_TargetStars = -1;

        private ModelState m_State;
        private ModelProgressInfo m_ProgressInfo;
        private Routine m_PhaseRoutine;

        public void SetData(ModelState state, ModelProgressInfo info) {
            m_State = state;
            m_ProgressInfo = info;

            m_State.Simulation.OnInterventionUpdated += OnInterventionUpdated;
        }

        public Action<SimSnapshot> OnTickDisplay;

        public Action OnSyncAchieved;
        public Action OnSyncUnsuccessful;

        public Action OnPredictCompleted;

        public Action OnInterventionSuccessful;
        public Action OnInterventionUnsuccessful;
        
        public Action OnAnimationStart;
        public Action OnAnimationFinished;
        public Action OnInterventionReset;

        #region Unity Events

        protected override void Awake() {
            m_InputLayer = BaseInputLayer.Find(this);

            m_SimulateButton.onClick.AddListener(OnSimulateClicked);
            m_PredictButton.onClick.AddListener(OnPredictClicked);
            m_InterveneRunButton.onClick.AddListener(OnInterveneRunClicked);
            m_InterveneResetButton.onClick.AddListener(OnInterveneResetClicked);
            m_SaveButton.onClick.AddListener(OnSaveClicked);

            m_InterveneAddPanel.Filter = (b) => m_State.Simulation.CanIntroduceForIntervention(b);
            m_InterveneAddPanel.OnAdded = OnIntervenePanelAdded;
            m_InterveneAddPanel.OnRemoved = OnIntervenePanelRemoved;

            m_SyncViewNormalToggle.onValueChanged.AddListener(OnSimulateViewToggled);
            m_SyncViewFillToggle.onValueChanged.AddListener(OnEvaluateViewToggled);

            m_SyncDivergencePopup.Panel.OnHideEvent.AddListener((_) => {
                m_SyncInputGroup.blocksRaycasts = true;
            });
            m_SyncDivergencePopup.Panel.OnShowCompleteEvent.AddListener((_) => {
                m_SyncInputGroup.blocksRaycasts = false;
            });

            m_Graph.OnDivergenceClicked = (p) => {
                m_SyncDivergencePopup.DisplayDivergence(p.Sign);
            };
            m_Graph.OnDiscrepancyClicked = () => {
                PopupContent content = default;
                content.Header = Loc.Find("modeling.noIntervenePopup.header");
                content.Text = Loc.Find("modeling.noIntervenePopup.description");
                m_IntervenePopup.Present(ref content, PopupFlags.ShowCloseButton);
            };
        }

        protected override void Start() {
            base.Start();
            
            m_InterveneButtonGroup.gameObject.SetActive(false);
            m_InterveneAddToggleGroup.ForceActive(false);
            m_SaveButton.gameObject.SetActive(false);

            m_SyncViewGroup.SetActive(false);
        }

        private void OnDestroy() {
        }

        #if UNITY_EDITOR

        private Routine m_DebugReload;

        private void LateUpdate() {
            if (m_InputLayer.Device.KeyPressed(KeyCode.F8) || (m_InputLayer.Device.KeyPressed(KeyCode.M) && m_InputLayer.Device.KeyDown(KeyCode.LeftShift))) {
                Log.Msg("[SimulationUI] Debug reloading simulation data");
                m_DebugReload.Replace(this, DebugReloadGraph());
            }
        }

        private IEnumerator DebugReloadGraph() {
            m_State.Simulation.LoadSite();
            m_State.Simulation.LoadConceptualModel();
            
            RenderSyncPredictDivider();

            m_State.Simulation.ClearHistorical();
            m_State.Simulation.ClearPlayer();
            m_State.Simulation.ClearPredict();

            m_State.Simulation.GenerateHistorical();
            m_State.Simulation.GeneratePlayerData();

            if (m_IsPredicting) {
                m_State.Simulation.GeneratePredictData();
            }

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            m_Graph.AllocateBlocks(m_State);
            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.AllData);
            
            if (m_IsPredicting) {
                if (m_IsIntervention) {
                    m_Graph.RenderData(SimRenderMask.PredictIntervene);
                } else {
                    m_Graph.RenderData(SimRenderMask.Predict);
                }
            } else {
                m_Graph.RenderData(SimRenderMask.HistoricalPlayer);
            }

        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events

        #region Phases

        public void SetPhase(ModelPhases phase) {
            m_IsPredicting = phase > ModelPhases.Sync;
            m_IsIntervention = phase == ModelPhases.Intervene;
            bool alreadyCompleted = (m_State.CompletedPhases & phase) != 0;

            m_State.Simulation.EnsureHistorical();
            m_Graph.AllocateBlocks(m_State);

            m_SimulateButton.gameObject.SetActive(phase == ModelPhases.Sync);
            m_AccuracyDisplay.SetActive(phase == ModelPhases.Sync);
            m_PredictButton.gameObject.SetActive(phase == ModelPhases.Predict);
            m_InterveneButtonGroup.gameObject.SetActive(phase == ModelPhases.Intervene);
            m_InterveneAddToggleGroup.SetActive(phase == ModelPhases.Intervene);
            m_SyncViewGroup.SetActive(false);
            HideSaveButton();

            if (phase != ModelPhases.Predict) {
                m_InterveneAddPanel.ClearSelection();
                m_InterveneAddPanel.Hide();
            }

            m_SyncDivergencePopup.Panel.InstantHide();
            m_IntervenePopup.InstantHide();

            m_PhaseRoutine.Stop();
            switch(phase) {
                case ModelPhases.Sync: {
                    m_State.LastKnownAccuracy = 0;
                    m_AccuracyTicks.Display(0, m_TargetStars);
                    RenderAccuracy();

                    m_State.Display.FilterNodes(WorldFilterMask.HasRate | WorldFilterMask.Missing | WorldFilterMask.Organism, WorldFilterMask.Relevant, WorldFilterMask.AnyWaterChem, true);

                    ModelMissingReasons missing;
                    
                    if (alreadyCompleted) {
                        m_SimulateButton.gameObject.SetActive(false);
                        m_PhaseRoutine.Replace(this, Sync_AlreadyCompleted()).Tick();
                    } else if ((missing = m_State.Simulation.EvaluateHistoricalDataMissing()) != 0) {
                        m_SimulateButton.gameObject.SetActive(false);
                        m_Graph.RenderData(0);

                        switch(missing) {
                            case ModelMissingReasons.HistoricalPopulations: {
                                m_State.Display.Status(m_MissingPopulationsLabel, AQColors.Red);
                                break;
                            }
                            case ModelMissingReasons.HistoricalWaterChem: {
                                m_State.Display.Status(m_MissingWaterChemistryLabel, AQColors.Red);
                                break;
                            }
                            case ModelMissingReasons.HistoricalWaterChem | ModelMissingReasons.HistoricalPopulations: {
                                m_State.Display.Status(m_MissingPopulationsWaterChemistryLabel, AQColors.Red);
                                break;
                            }
                        }
                    } else {
                        m_State.Display.Status(null);
                        m_PhaseRoutine.Replace(this, Sync_Boot()).Tick();
                    }
                    
                    break;
                }

                case ModelPhases.Predict: {
                    m_State.Display.FilterNodes(WorldFilterMask.HasRate | WorldFilterMask.Missing | WorldFilterMask.Organism, WorldFilterMask.Relevant, WorldFilterMask.AnyWaterChem, true);

                    if (alreadyCompleted) {
                        m_PredictButton.gameObject.SetActive(false);
                        m_PhaseRoutine.Replace(this, Predict_AlreadyCompleted()).Tick();
                    } else {
                        m_PredictButton.gameObject.SetActive(true);
                        m_PhaseRoutine.Replace(this, Predict_Boot()).Tick();
                    }
                    break;
                }
            
                case ModelPhases.Intervene: {
                    m_State.Display.FilterNodes(WorldFilterMask.HasRate | WorldFilterMask.Missing | WorldFilterMask.Organism, WorldFilterMask.Relevant, WorldFilterMask.AnyWaterChem, true);

                    m_InterveneButtonGroup.gameObject.SetActive(true);
                    m_Graph.RenderData(0);
                    m_State.Simulation.ClearIntervention();
                    m_PhaseRoutine.Replace(this, Intervene_Boot());
                    OnInterventionUpdated();
                    break;
                }
            }
        }

        private void TryDisplaySaveButton(StringHash32 modelId) {
            if (!modelId.IsEmpty && !Save.Bestiary.HasFact(modelId)) {
                m_SaveButton.gameObject.SetActive(true);
            } else {
                m_SaveButton.gameObject.SetActive(false);
            }
        }

        private void HideSaveButton() {
            m_SaveButton.gameObject.SetActive(false);
        }

        #endregion // Phases

        #region Sync

        private IEnumerator Sync_AlreadyCompleted() {
            m_GraphFader.SetActive(true);
            m_Graph.RenderData(0);
            m_State.LastKnownAccuracy = 0;
            RenderAccuracy();
            m_SyncViewGroup.SetActive(false);

            m_State.Simulation.EnsurePlayerData();
            m_SimulateButton.gameObject.SetActive(false);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.HistoricalPlayer);
            m_Graph.RenderData(SimRenderMask.HistoricalPlayer);

            m_State.LastKnownAccuracy = m_State.Simulation.CalculateAccuracy(m_ProgressInfo.Sim.SyncTickCount + 1);
            RenderAccuracy();
            m_GraphFader.SetActive(false);
            m_SyncViewGroup.SetActive(true);
            m_SyncViewNormalToggle.SetIsOnWithoutNotify(true);
        }

        private IEnumerator Sync_Boot() {
            m_Graph.RenderData(0);
            m_State.LastKnownAccuracy = 0;
            RenderAccuracy();
            m_AccuracyTicks.Display(0, m_TargetStars);
            m_SyncViewGroup.SetActive(false);

            m_State.Simulation.EnsureHistorical();
            m_SimulateButton.gameObject.SetActive(false);
            m_GraphFader.SetActive(true);

            Services.Data.SetVariable(ModelingConsts.Var_SimulationSync, 0);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.Historical);
            m_Graph.RenderData(SimRenderMask.Historical);

            m_SimulateButton.gameObject.SetActive(true);
        }

        private IEnumerator Sync_Attempt() {
            m_Graph.RenderData(SimRenderMask.Historical);
            m_State.LastKnownAccuracy = 0;
            RenderAccuracy();

            m_State.Simulation.EnsurePlayerData();
            m_SimulateButton.gameObject.SetActive(false);
            m_GraphFader.SetActive(true);

            Services.Data.SetVariable(ModelingConsts.Var_SimulationSync, 0);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.HistoricalPlayer);

            OnAnimationStart?.Invoke();

            m_Graph.RenderData(SimRenderMask.HistoricalPlayer);
            m_State.LastKnownAccuracy = m_State.Simulation.CalculateAccuracy(m_ProgressInfo.Sim.SyncTickCount + 1);
            Log.Msg("[SimulationUI] Calculated accuracy: {0}", m_State.LastKnownAccuracy);
            RenderAccuracy();

            m_GraphFader.SetActive(false);
            m_SyncViewGroup.SetActive(true);
            m_SyncViewNormalToggle.SetIsOnWithoutNotify(true);

            yield return 0.2f;

            Services.Data.SetVariable(ModelingConsts.Var_SimulationSync, m_State.LastKnownAccuracy);

            OnAnimationFinished?.Invoke();

            if (m_ProgressInfo.Scope != null && !m_ProgressInfo.Scope.SyncModelId.IsEmpty) {
                if (m_ProgressInfo.Scope.MinimumSyncAccuracy <= m_State.LastKnownAccuracy) {
                    TryDisplaySaveButton(m_ProgressInfo.Scope.SyncModelId);
                } else {
                    OnSyncUnsuccessful?.Invoke();
                }
            }
        }

        #endregion // Sync

        #region Predict

        private IEnumerator Predict_AlreadyCompleted() {
            m_Graph.RenderData(0);
            m_GraphFader.SetActive(true);

            m_State.Simulation.EnsurePredictData();
            
            m_PredictButton.gameObject.SetActive(false);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.Predict);
            m_Graph.RenderData(SimRenderMask.Predict);
            m_GraphFader.SetActive(false);
        }

        private IEnumerator Predict_Boot() {
            m_Graph.RenderData(0);

            m_State.Simulation.EnsurePredictData();
            m_PredictButton.gameObject.SetActive(false);
            m_GraphFader.SetActive(true);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.Predict);
            m_Graph.RenderData(SimRenderMask.Predict, true);

            m_PredictButton.gameObject.SetActive(true);
        }

        private IEnumerator Predict_Attempt() {
            m_Graph.RenderData(SimRenderMask.Predict, true);

            m_State.Simulation.EnsurePredictData();

            m_PredictButton.gameObject.SetActive(false);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.Predict);
            m_Graph.RenderData(SimRenderMask.Predict);

            m_GraphFader.SetActive(false);

            yield return 0.2f;

            if (m_ProgressInfo.Scope != null) {
                TryDisplaySaveButton(m_ProgressInfo.Scope.PredictModelId);
            }
        }

        #endregion // Predict

        #region Intervene

        private IEnumerator Intervene_Boot() {
            m_Graph.RenderData(0);
            m_GraphFader.SetActive(true);
            
            m_State.Simulation.EnsurePlayerData();
            m_SimulateButton.gameObject.SetActive(false);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.PredictIntervene);
            m_Graph.RenderData(SimRenderMask.PredictIntervene, true);

            m_GraphFader.SetActive(true);
        }

        private IEnumerator Intervene_Attempt() {
            m_Graph.RenderData(SimRenderMask.PredictIntervene, true);

            m_State.Simulation.EnsurePredictData();

            m_PredictButton.gameObject.SetActive(false);
            m_GraphFader.SetActive(true);

            OnAnimationStart?.Invoke();

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.PredictIntervene);
            m_Graph.RenderData(SimRenderMask.PredictIntervene);
            m_GraphFader.SetActive(false);

            OnAnimationFinished?.Invoke();

            yield return 0.2f;

            bool bSuccess = m_State.Simulation.EvaluateInterventionGoals();

            if (m_ProgressInfo.Scope != null) {
                if (bSuccess) {
                    Log.Msg("[SimulationUI] Intervention hit target!");
                    TryDisplaySaveButton(m_ProgressInfo.Scope.InterveneModelId);
                } else {
                    Services.Audio.PostEvent("syncDenied");
                    OnInterventionUnsuccessful?.Invoke();
                }
            }
        }

        #endregion // Intervene

        #region Rendering

        private void RenderSyncPredictDivider() {
            if (m_ProgressInfo.Scope && m_ProgressInfo.Scope.MinimumSyncAccuracy > 0) {
                m_TargetStars = m_State.Simulation.CalculateAccuracyStars(m_ProgressInfo.Scope.MinimumSyncAccuracy);
            } else {
                m_TargetStars = -1;
            }
        }

        private Rect GetInterventionTargetRect() {
            Rect r = default;
            if (m_IsIntervention && m_ProgressInfo.Scope != null) {
                float maxHeight = 0;
                ActorCountRange count;
                for(int i = 0; i < m_ProgressInfo.Scope.InterventionTargets.Length; i++) {
                    count = m_ProgressInfo.Scope.InterventionTargets[i];
                    maxHeight = Math.Max(BestiaryUtils.PopulationToMass(count.Id, count.Population + count.Range), maxHeight);
                }
                r.height = maxHeight;
            }
            return r;
        }

        private void RenderAccuracy() {
            m_AccuracyTicks.Display(m_State.Simulation.CalculateAccuracyStars(m_State.LastKnownAccuracy), m_TargetStars);
        }

        #endregion // Rendering

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

        protected override void OnShow(bool _) {   
            m_AccuracyDisplay.SetActive(false);
            m_SyncViewGroup.SetActive(false);
            m_SimulateButton.gameObject.SetActive(false);
            RenderSyncPredictDivider();
            Services.Events.Queue(ModelingConsts.Event_Simulation_Begin);
            Services.Script.TriggerResponse(ModelingConsts.Trigger_GraphStarted);
        }

        protected override void OnHide(bool _) {
            m_PhaseRoutine.Stop();
            m_Graph.RenderData(0);
            m_InterveneAddPanel.ClearSelection();
            m_InterveneAddPanel.Hide();
        }

        #endregion // BasePanel

        #region Callbacks

        private void OnSaveClicked() {
            switch(m_State.Phase) {
                case ModelPhases.Sync: {
                    Services.Events.Dispatch(ModelingConsts.Event_Simulation_Complete);
                    OnSyncAchieved?.Invoke();
                    break;
                }

                case ModelPhases.Predict: {
                    OnPredictCompleted?.Invoke();
                    break;
                }

                case ModelPhases.Intervene: {
                    OnInterventionSuccessful?.Invoke();
                    break;
                }
            }

            m_SaveButton.gameObject.SetActive(false);
        }

        private void OnSimulateClicked() {
            m_SimulateButton.gameObject.SetActive(false);
            m_PhaseRoutine.Replace(this, Sync_Attempt());
        }

        private void OnSimulateViewToggled(bool b) {
            if (!b) {
                return;
            }

            m_Graph.RenderData(SimRenderMask.HistoricalPlayer);
        }

        private void OnEvaluateViewToggled(bool b) {
            if (!b) {
                return;
            }

            m_Graph.RenderData(SimRenderMask.HistoricalPlayerFill);
        }

        private void OnPredictClicked() {
            m_PredictButton.gameObject.SetActive(false);
            m_PhaseRoutine.Replace(this, Predict_Attempt());
        }

        private void OnInterveneRunClicked() {
            m_InterveneRunButton.interactable = false;
            m_InterveneAddToggleGroup.SetActive(false);
            m_PhaseRoutine.Replace(this, Intervene_Attempt());
        }

        private void OnInterveneErrorClicked() {
            Services.UI.Popup.Display(
                // TODO: modify description depending on whether the player is above or below the target
                Loc.Find("modeling.noIntervenePopup.header"), Loc.Find("modeling.noIntervenePopup.description")
            );
        }

        private void OnInterveneResetClicked() {
            m_InterveneButtonGroup.gameObject.SetActive(false);
            m_InterveneAddToggleGroup.SetActive(true);
            m_State.Simulation.ClearIntervention();
            m_PhaseRoutine.Stop();
            HideSaveButton();
            m_IntervenePopup.Hide();

            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.PredictIntervene);
            m_Graph.RenderData(SimRenderMask.PredictIntervene, true);
            
            m_InterveneAddPanel.ClearSelection();
            m_InterveneAddPanel.Hide();
            m_InterveneAddToggle.SetIsOnWithoutNotify(false);
            OnInterventionReset?.Invoke();
        }

        private void OnIntervenePanelAdded(BestiaryDesc desc) {
            if (Ref.Replace(ref m_State.Simulation.Intervention.Target, desc)) {
                m_State.Simulation.Intervention.Amount = 0;
                m_State.Simulation.RegenerateIntervention();
                m_State.Simulation.GeneratePredictProfile();
                m_InterveneAddPanel.Hide();
            }
        }

        private void OnIntervenePanelRemoved(BestiaryDesc desc) {
            if (Ref.CompareExchange(ref m_State.Simulation.Intervention.Target, desc, null)) {
                Async.InvokeAsync(() => {
                    if (m_State.Simulation.Intervention.Target != null) {
                        return;
                    }

                    m_State.Simulation.RegenerateIntervention();
                    m_State.Simulation.GeneratePredictProfile();
                });
            }
        }

        private void OnInterventionUpdated() {
            m_InterveneButtonGroup.gameObject.SetActive(m_State.Simulation.Intervention.Target != null);
            if (m_InterveneButtonGroup.isActiveAndEnabled) {
                m_InterveneButtonGroup.ForceRebuild(true);
            }
            if (m_State.Simulation.Intervention.Target != null && !m_State.Simulation.IsInterventionNewOrganism()) {
                m_InterveneAddPanel.Hide();
            }
            m_InterveneRunButton.interactable = m_State.Simulation.Intervention.Target != null && m_State.Simulation.Intervention.Amount != 0;
            m_InterveneResetButton.interactable = m_State.Simulation.Intervention.Target != null;
            m_InterveneAddToggleGroup.SetActive(m_State.Simulation.Intervention.Target == null);

            m_Graph.Intervene(m_State.Simulation.Intervention, m_State);
            m_Graph.PopulateData(m_State, m_ProgressInfo, SimRenderMask.PredictIntervene);
            m_Graph.RenderData(SimRenderMask.PredictIntervene, true);
        }

        #endregion // Callbacks
    }
}