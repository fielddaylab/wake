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
        [SerializeField] private SimLineGraph m_HistoricalGraph = null;
        [SerializeField] private SimLineGraph m_PlayerGraph = null;
        [SerializeField] private SimLineGraph m_PredictGraph = null;
        [SerializeField] private SimTargetGraph m_TargetGraph = null;
        [SerializeField] private RectTransform m_SyncPredictLine = null;
        [SerializeField] private TMP_Text m_SyncTimeLabelDisplay = null;
        [SerializeField] private TMP_Text m_PredictTimeDisplay = null;

        [Header("Sync")]
        [SerializeField] private Button m_SimulateButton = null;
        [SerializeField] private GameObject m_HistoricalMissingDisplay = null;
        [SerializeField] private GameObject m_AccuracyDisplay = null;
        [SerializeField] private RectTransform m_AccuracyMeter = null;
        [SerializeField] private RectTransform m_AccuracyGoal = null;
        
        [Header("Predict")]
        [SerializeField] private Button m_PredictButton = null;

        [Header("Intervene")]
        [SerializeField] private RectTransform m_InterveneButtonGroup = null;
        [SerializeField] private Toggle m_InterveneAddToggle = null;
        [SerializeField] private BestiaryAddPanel m_InterveneAddPanel = null;
        [SerializeField] private Button m_InterveneResetButton = null;
        [SerializeField] private Button m_InterveneRunButton = null;

        [Header("Settings")]
        [SerializeField] private TextId m_SyncTimeLabel = default;
        [SerializeField] private TextId m_PredictTimeLabel = default;

        #endregion // Inspector

        [NonSerialized] private bool m_IsPredicting;
        [NonSerialized] private bool m_IsIntervention;
        [NonSerialized] private BaseInputLayer m_InputLayer;

        private ModelState m_State;
        private ModelProgressInfo m_ProgressInfo;
        private Routine m_PhaseRoutine;

        public void SetData(ModelState state, ModelProgressInfo info) {
            m_State = state;
            m_ProgressInfo = info;

            m_State.Simulation.OnInterventionUpdated += OnInterventionUpdated;
        }

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

            m_InterveneAddPanel.Filter = (b) => m_State.Simulation.CanIntroduceForIntervention(b);
            m_InterveneAddPanel.OnAdded = OnIntervenePanelAdded;
            m_InterveneAddPanel.OnRemoved = OnIntervenePanelRemoved;
        }

        private void OnDestroy() {
        }

        #if UNITY_EDITOR

        private Routine m_DebugReload;

        private void LateUpdate() {
            if (m_InputLayer.Device.KeyPressed(KeyCode.F8)) {
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

            PopulateHistoricalGraph();
            PopulatePlayerGraph();
            
            if (m_IsPredicting) {
                PopulatePredictGraph();
                if (m_IsIntervention) {
                    PopulateTargetGraph();
                }
                RenderLines((int) m_ProgressInfo.Sim.PredictTickCount, true);
            } else {
                RenderLines((int) m_ProgressInfo.Sim.SyncTickCount, false);
            }

        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events

        #region Phases

        public void SetPhase(ModelPhases phase) {
            m_IsPredicting = phase > ModelPhases.Sync;
            m_IsIntervention = phase == ModelPhases.Intervene;
            m_PredictTimeDisplay.gameObject.SetActive(m_IsPredicting);
            bool alreadyCompleted = (m_State.CompletedPhases & phase) != 0;

            m_State.Simulation.EnsureHistorical();

            m_SimulateButton.gameObject.SetActive(phase == ModelPhases.Sync);
            m_AccuracyDisplay.SetActive(phase == ModelPhases.Sync);
            m_PredictButton.gameObject.SetActive(phase == ModelPhases.Predict);
            m_InterveneButtonGroup.gameObject.SetActive(phase == ModelPhases.Intervene);
            m_InterveneResetButton.gameObject.SetActive(phase == ModelPhases.Intervene);
            m_HistoricalMissingDisplay.gameObject.SetActive(false);

            if (phase != ModelPhases.Predict) {
                m_InterveneAddPanel.ClearSelection();
                m_InterveneAddPanel.Hide();
            }

            m_PhaseRoutine.Stop();
            switch(phase) {
                case ModelPhases.Sync: {
                    m_State.LastKnownAccuracy = 0;
                    RenderAccuracy();
                    
                    if (alreadyCompleted) {
                        m_SimulateButton.gameObject.SetActive(false);
                        m_HistoricalMissingDisplay.SetActive(false);
                        m_PhaseRoutine.Replace(this, Sync_AlreadyCompleted()).TryManuallyUpdate(0);
                    } else if (m_State.Simulation.IsAnyHistoricalDataMissing()) {
                        m_SimulateButton.gameObject.SetActive(false);
                        m_HistoricalMissingDisplay.SetActive(true);
                        ClearLines();
                    } else {
                        m_SimulateButton.gameObject.SetActive(true);
                        m_HistoricalMissingDisplay.SetActive(false);
                        ClearLines();
                    }
                    
                    break;
                }

                case ModelPhases.Predict: {
                    if (alreadyCompleted) {
                        m_PredictButton.gameObject.SetActive(false);
                        m_PhaseRoutine.Replace(this, Predict_AlreadyCompleted()).TryManuallyUpdate(0);
                    } else {
                        m_PredictButton.gameObject.SetActive(true);
                        m_PredictGraph.Clear();
                    }
                    break;
                }
            
                case ModelPhases.Intervene: {
                    m_InterveneButtonGroup.gameObject.SetActive(true);
                    m_PredictGraph.Clear();
                    m_State.Simulation.ClearIntervention();
                    m_PhaseRoutine.Replace(this, Intervene_Boot());
                    OnInterventionUpdated();
                    break;
                }
            }
        }

        #endregion // Phases

        #region Sync

        private IEnumerator Sync_AlreadyCompleted() {
            ClearLines();
            m_State.LastKnownAccuracy = 0;
            RenderAccuracy();

            m_State.Simulation.EnsurePlayerData();
            m_SimulateButton.gameObject.SetActive(false);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            PopulateHistoricalGraph();
            PopulatePlayerGraph();
            RenderLines((int) m_ProgressInfo.Sim.SyncTickCount, false);

            m_State.LastKnownAccuracy = m_State.Simulation.CalculateAccuracy(m_ProgressInfo.Sim.SyncTickCount + 1);
            RenderAccuracy();
        }

        private IEnumerator Sync_Attempt() {
            ClearLines();
            m_State.LastKnownAccuracy = 0;
            RenderAccuracy();

            m_State.Simulation.EnsurePlayerData();
            m_SimulateButton.gameObject.SetActive(false);

            Services.Data.SetVariable(ModelingConsts.Var_SimulationSync, 0);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            PopulateHistoricalGraph();
            PopulatePlayerGraph();

            OnAnimationStart?.Invoke();

            for(uint i = 0; i <= m_ProgressInfo.Sim.SyncTickCount; i++) {
                RenderLines((int) i, false);
                m_State.LastKnownAccuracy = m_State.Simulation.CalculateAccuracy(i + 1);
                RenderAccuracy();
                yield return 0.2f;
            }

            Services.Data.SetVariable(ModelingConsts.Var_SimulationSync, m_State.LastKnownAccuracy);

            OnAnimationFinished?.Invoke();

            if (m_ProgressInfo.Scope != null && m_ProgressInfo.Scope.MinimumSyncAccuracy > 0) {
                if (m_ProgressInfo.Scope.MinimumSyncAccuracy <= m_State.LastKnownAccuracy) {
                    OnSyncAchieved?.Invoke();
                } else {
                    OnSyncUnsuccessful?.Invoke();
                    Services.Script.TriggerResponse(ModelingConsts.Trigger_SyncError);
                }
            }
        }

        #endregion // Sync

        #region Predict

        private IEnumerator Predict_AlreadyCompleted() {
            m_PredictGraph.Clear();

            m_State.Simulation.EnsurePredictData();
            
            m_PredictButton.gameObject.SetActive(false);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            PopulateHistoricalGraph();
            PopulatePlayerGraph();
            PopulatePredictGraph();
            RenderLines((int) m_ProgressInfo.Sim.PredictTickCount, true);
        }

        private IEnumerator Predict_Attempt() {
            m_PredictGraph.Clear();

            m_State.Simulation.EnsurePredictData();

            m_PredictButton.gameObject.SetActive(false);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            PopulateHistoricalGraph();
            PopulatePlayerGraph();
            PopulatePredictGraph();

            OnAnimationStart?.Invoke();

            for(uint i = 0; i <= m_ProgressInfo.Sim.PredictTickCount; i++) {
                RenderLines((int) i, true);
                yield return 0.2f;
            }

            OnAnimationFinished?.Invoke();

            if (m_ProgressInfo.Scope != null) {
                OnPredictCompleted?.Invoke();
            }
        }

        #endregion // Predict

        #region Intervene

        private IEnumerator Intervene_Boot() {
            ClearLines();
            
            m_State.Simulation.EnsurePlayerData();
            m_SimulateButton.gameObject.SetActive(false);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            PopulateHistoricalGraph();
            PopulatePlayerGraph();
            PopulateTargetGraph();
            
            RenderLines((int) m_ProgressInfo.Sim.SyncTickCount, false);
        }

        private IEnumerator Intervene_Attempt() {
            m_PredictGraph.Clear();

            m_State.Simulation.EnsurePredictData();

            m_PredictButton.gameObject.SetActive(false);

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            PopulateHistoricalGraph();
            PopulatePlayerGraph();
            PopulatePredictGraph();

            OnAnimationStart?.Invoke();

            for(uint i = 0; i <= m_ProgressInfo.Sim.PredictTickCount; i++) {
                RenderLines((int) i, true);
                yield return 0.2f;
            }

            OnAnimationFinished?.Invoke();

            bool bSuccess = m_State.Simulation.EvaluateInterventionGoals();

            if (m_ProgressInfo.Scope != null) {
                if (bSuccess) {
                    Log.Msg("[SimulationUI] Intervention hit target!");
                    OnInterventionSuccessful?.Invoke();
                } else {
                    OnInterventionUnsuccessful?.Invoke();
                    Services.Script.TriggerResponse(ModelingConsts.Trigger_InterveneError);
                    Services.Events.QueueForDispatch(ModelingConsts.Event_Simulation_Complete);
                }
            }
        }

        #endregion // Intervene

        #region Rendering

        private void RenderLines(int ticksToRender, bool predicting) {
            GraphingUtils.AxisRangePair axis;
            uint totalTicks = m_ProgressInfo.Sim.SyncTickCount + m_ProgressInfo.Sim.PredictTickCount;
            
            int historicalRange, predictRange;
            if (predicting) {
                predictRange = 1 + ticksToRender;
                historicalRange = 1 + (int) m_ProgressInfo.Sim.SyncTickCount;
                axis = CalculateGraphRect(m_HistoricalGraph.Range, m_PlayerGraph.Range, m_PredictGraph.BoundedRange(predictRange), GetInterventionTargetRect(), default, totalTicks, 8);
            } else {
                historicalRange = 1 + ticksToRender;
                predictRange = 0;
                axis = CalculateGraphRect(m_HistoricalGraph.BoundedRange(historicalRange), GetInterventionTargetRect(), default, default, default, totalTicks, 8);
            }

            Rect fullRect = axis.ToRect();
            m_HistoricalGraph.RenderLines(fullRect, historicalRange);
            m_PlayerGraph.RenderLines(fullRect, historicalRange);

            if (m_IsIntervention) {
                m_TargetGraph.RenderTargets(fullRect);
            } else {
                m_TargetGraph.Clear();
            }

            if (predicting) {
                m_PredictGraph.RenderLines(fullRect, predictRange, m_IsIntervention);
            } else {
                m_PredictGraph.Clear();
            }
        }

        private void ClearLines() {
            m_PlayerGraph.Clear();
            m_HistoricalGraph.Clear();
            m_PredictGraph.Clear();
            m_TargetGraph.Clear();
        }

        private void RenderSyncPredictDivider() {
            float divide = (float) m_ProgressInfo.Sim.SyncTickCount / (m_ProgressInfo.Sim.SyncTickCount + m_ProgressInfo.Sim.PredictTickCount);
            m_SyncPredictLine.SetAnchorX(divide);

            float left = divide / 2;
            float right = divide + (1 - divide) / 2;
            m_SyncTimeLabelDisplay.rectTransform.SetAnchorX(left);
            m_PredictTimeDisplay.rectTransform.SetAnchorX(right);

            m_SyncTimeLabelDisplay.text = Loc.Format(m_SyncTimeLabel, m_ProgressInfo.Sim.SyncTickCount);
            m_PredictTimeDisplay.text = Loc.Format(m_PredictTimeLabel, m_ProgressInfo.Sim.PredictTickCount);

            if (m_ProgressInfo.Scope && m_ProgressInfo.Scope.MinimumSyncAccuracy > 0) {
                m_AccuracyGoal.SetAnchorX((float) m_ProgressInfo.Scope.MinimumSyncAccuracy / 100);
                m_AccuracyGoal.gameObject.SetActive(true);
            } else {
                m_AccuracyGoal.gameObject.SetActive(false);
            }

            ((RectTransform) m_SimulateButton.transform).SetAnchorX(left);
            ((RectTransform) m_HistoricalMissingDisplay.transform).SetAnchorX(left);
            ((RectTransform) m_PredictButton.transform).SetAnchorX(right);
            m_InterveneButtonGroup.SetAnchorX(right);
        }

        private Rect GetInterventionTargetRect() {
            Rect r = default;
            if (m_IsIntervention && m_ProgressInfo.Scope != null) {
                float maxHeight = 0;
                ActorCountRange count;
                for(int i = 0; i < m_ProgressInfo.Scope.InterventionTargets.Length; i++) {
                    count = m_ProgressInfo.Scope.InterventionTargets[i];
                    maxHeight = Math.Max(count.Population + count.Range, maxHeight);
                }
                r.height = maxHeight;
            }
            return r;
        }

        private void RenderAccuracy() {
            m_AccuracyMeter.anchorMax = new Vector2(m_State.LastKnownAccuracy / 100f, 1);
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
            m_SimulateButton.gameObject.SetActive(false);
            RenderSyncPredictDivider();
            Services.Events.QueueForDispatch(ModelingConsts.Event_Simulation_Begin);
            Services.Script.TriggerResponse(ModelingConsts.Trigger_GraphStarted);
        }

        protected override void OnHide(bool _) {
            m_PhaseRoutine.Stop();
            ClearLines();
            m_InterveneAddPanel.ClearSelection();
            m_InterveneAddPanel.Hide();
        }

        #endregion // BasePanel

        #region Callbacks

        private void OnSimulateClicked() {
            m_SimulateButton.gameObject.SetActive(false);
            m_PhaseRoutine.Replace(this, Sync_Attempt());
        }

        private void OnPredictClicked() {
            m_PredictButton.gameObject.SetActive(false);
            m_PhaseRoutine.Replace(this, Predict_Attempt());
        }

        private void OnInterveneRunClicked() {
            m_InterveneButtonGroup.gameObject.SetActive(false);
            m_PhaseRoutine.Replace(this, Intervene_Attempt());
        }

        private void OnInterveneResetClicked() {
            m_InterveneButtonGroup.gameObject.SetActive(true);
            m_State.Simulation.ClearIntervention();
            m_PhaseRoutine.Stop();
            m_PredictGraph.Clear();
            m_InterveneAddPanel.ClearSelection();
            m_InterveneAddPanel.Hide();
            m_InterveneAddToggle.SetIsOnWithoutNotify(false);
            RenderLines((int) m_ProgressInfo.Sim.SyncTickCount, false);
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
            m_InterveneRunButton.interactable = m_State.Simulation.Intervention.Target != null && m_State.Simulation.Intervention.Amount != 0;
            m_InterveneResetButton.interactable = m_State.Simulation.Intervention.Target != null;
            m_InterveneAddToggle.interactable = m_State.Simulation.Intervention.Target == null;
        }

        #endregion // Callbacks

        #region Graphing

        private void PopulateHistoricalGraph() {
            SimSnapshot* data = m_State.Simulation.RetrieveHistoricalData(out uint count);
            m_HistoricalGraph.LoadOrganisms(data, count, 0, m_State.Simulation.HistoricalProfile, m_State.Simulation.ShouldGraphHistorical);
        }

        private void PopulatePlayerGraph() {
            SimSnapshot* data = m_State.Simulation.RetrievePlayerData(out uint count);
            m_PlayerGraph.LoadOrganisms(data, count, 0, m_State.Simulation.PlayerProfile, m_State.Simulation.ShouldGraphHistorical);
        }

        private void PopulatePredictGraph() {
            SimSnapshot* data = m_State.Simulation.RetrievePredictData(out uint count);
            m_PredictGraph.LoadOrganisms(data, count, m_ProgressInfo.Sim.SyncTickCount, m_State.Simulation.PredictProfile, (b) => true);
        }

        private void PopulateTargetGraph() {
            if (m_ProgressInfo.Scope) {
                m_TargetGraph.LoadTargets(m_ProgressInfo.Scope);
            } else {
                m_TargetGraph.Clear();
            }
        }

        static private GraphingUtils.AxisRangePair CalculateGraphRect(in Rect inA, in Rect inB, in Rect inC, in Rect inD, in Rect inE, uint inTickCountX, uint inTickCountY) {
            Rect rect = inA;
            Geom.Encapsulate(ref rect, inB);
            Geom.Encapsulate(ref rect, inC);
            Geom.Encapsulate(ref rect, inD);
            Geom.Encapsulate(ref rect, inE);

            GraphingUtils.AxisRangePair pair;
            pair.X = new GraphingUtils.AxisRangeInfo() { Min = 0, Max = inTickCountX, TickCount = inTickCountX + 1, TickInterval = 1 };
            pair.Y = GraphingUtils.CalculateAxis(rect.yMin, rect.yMax, inTickCountY);
            pair.Y.SetMinAtOrigin();
            return pair;
        }

        #endregion // Graphing
    }
}