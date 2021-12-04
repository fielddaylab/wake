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

        [Header("Settings")]
        [SerializeField] private TextId m_SyncTimeLabel = default;
        [SerializeField] private TextId m_PredictTimeLabel = default;

        #endregion // Inspector

        [NonSerialized] private bool m_IsPredicting;
        [NonSerialized] private BaseInputLayer m_InputLayer;

        private ModelState m_State;
        private ModelProgressInfo m_ProgressInfo;
        private Routine m_PhaseRoutine;

        public void SetData(ModelState state, ModelProgressInfo info) {
            m_State = state;
            m_ProgressInfo = info;
        }

        public Action OnSyncAchieved;
        public Action OnPredictCompleted;

        #region Unity Events

        protected override void Awake() {
            m_InputLayer = BaseInputLayer.Find(this);

            m_SimulateButton.onClick.AddListener(OnSimulateClicked);
            m_PredictButton.onClick.AddListener(OnPredictClicked);
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
            RenderSyncPredictDivider();

            m_State.Simulation.ClearHistorical();
            m_State.Simulation.ClearPlayer();
            m_State.Simulation.ClearPredict();

            m_State.Simulation.GenerateHistorical();
            m_State.Simulation.GeneratePlayerData();

            if (m_IsPredicting) {
                m_State.Simulation.GeneratePredictData();
            }

            while(!m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            PopulateHistoricalGraph();
            PopulatePlayerGraph();

            RenderLines((int) m_ProgressInfo.Sim.SyncTickCount, m_IsPredicting);
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events

        #region Phases

        public void SetPhase(ModelPhases phase) {
            m_IsPredicting = phase > ModelPhases.Sync;
            m_PredictTimeDisplay.gameObject.SetActive(m_IsPredicting);
            bool alreadyCompleted = (m_State.CompletedPhases & phase) != 0;

            m_State.Simulation.EnsureHistorical();

            m_PhaseRoutine.Stop();
            switch(phase) {
                case ModelPhases.Sync: {
                    m_PredictButton.gameObject.SetActive(false);
                    m_AccuracyDisplay.gameObject.SetActive(true);
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
                    m_SimulateButton.gameObject.SetActive(false);
                    m_HistoricalMissingDisplay.SetActive(false);
                    m_AccuracyDisplay.gameObject.SetActive(false);
                    if (alreadyCompleted) {
                        m_PredictButton.gameObject.SetActive(false);
                        m_PhaseRoutine.Replace(this, Predict_AlreadyCompleted()).TryManuallyUpdate(0);
                    } else {
                        m_PredictButton.gameObject.SetActive(true);
                        m_PredictGraph.Clear();
                    }
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

            while(m_State.Simulation.IsExecutingRequests()) {
                yield return null;
            }

            PopulateHistoricalGraph();
            PopulatePlayerGraph();

            for(uint i = 0; i <= m_ProgressInfo.Sim.SyncTickCount; i++) {
                RenderLines((int) i, false);
                m_State.LastKnownAccuracy = m_State.Simulation.CalculateAccuracy(i + 1);
                RenderAccuracy();
                yield return 0.2f;
            }

            if (m_ProgressInfo.Scope != null && m_ProgressInfo.Scope.MinimumSyncAccuracy <= m_State.LastKnownAccuracy) {
                OnSyncAchieved?.Invoke();
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

            for(uint i = 0; i <= m_ProgressInfo.Sim.PredictTickCount; i++) {
                RenderLines((int) i, true);
                yield return 0.2f;
            }

            if (m_ProgressInfo.Scope != null) {
                OnPredictCompleted?.Invoke();
            }
        }

        #endregion // Predict

        #region Rendering

        private void RenderLines(int ticksToRender, bool predicting) {
            GraphingUtils.AxisRangePair axis;
            uint totalTicks = m_ProgressInfo.Sim.SyncTickCount + m_ProgressInfo.Sim.PredictTickCount;
            
            int historicalRange, predictRange;
            if (predicting) {
                axis = CalculateGraphRect(m_HistoricalGraph.Range, m_PlayerGraph.Range, m_PredictGraph.Range, default, totalTicks, 8);
                predictRange = 1 + ticksToRender;
                historicalRange = 1 + (int) m_ProgressInfo.Sim.SyncTickCount;
            } else {
                axis = CalculateGraphRect(m_HistoricalGraph.Range, default, default, default, totalTicks, 8);
                historicalRange = 1 + ticksToRender;
                predictRange = 0;
            }

            Rect fullRect = axis.ToRect();
            m_HistoricalGraph.RenderLines(fullRect, historicalRange);
            m_PlayerGraph.RenderLines(fullRect, historicalRange);

            if (predicting) {
                m_PredictGraph.RenderLines(fullRect, predictRange);
            } else {
                m_PredictGraph.Clear();
            }
        }

        private void ClearLines() {
            m_PlayerGraph.Clear();
            m_HistoricalGraph.Clear();
            m_PredictGraph.Clear();
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
        }

        protected override void OnHide(bool _) {
            m_PhaseRoutine.Stop();
            ClearLines();
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
            m_PredictGraph.LoadOrganisms(data, count, m_ProgressInfo.Sim.SyncTickCount, m_State.Simulation.PlayerProfile, m_State.Simulation.HasHistoricalPopulation);
        }

        static private GraphingUtils.AxisRangePair CalculateGraphRect(in Rect inA, in Rect inB, in Rect inC, in Rect inD, uint inTickCountX, uint inTickCountY) {
            Rect rect = inA;
            Geom.Encapsulate(ref rect, inB);
            Geom.Encapsulate(ref rect, inC);
            Geom.Encapsulate(ref rect, inD);

            GraphingUtils.AxisRangePair pair;
            pair.X = new GraphingUtils.AxisRangeInfo() { Min = 0, Max = inTickCountX, TickCount = inTickCountX + 1, TickInterval = 1 };
            pair.Y = GraphingUtils.CalculateAxis(rect.yMin, rect.yMax, inTickCountY);
            pair.Y.SetMinAtOrigin();
            return pair;
        }

        #endregion // Graphing
    }
}