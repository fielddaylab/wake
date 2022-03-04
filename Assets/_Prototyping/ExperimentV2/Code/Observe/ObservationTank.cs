using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Animation;
using Aqua.Profile;
using Aqua.Scripting;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class ObservationTank : MonoBehaviour, IScriptComponent
    {
        private enum SetupPhase : byte {
            Begin,
            Environment,
            Critters
        }

        private const float StartingIdleDuration = 30;
        private const float ResetIdleDuration = 20;
        private const float NoFactsLeftIdleDuration = 10;

        [Serializable] private class BehaviorCirclePool : SerializablePool<BehaviorCaptureCircle> { }
        
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private SelectableTank m_ParentTank = null;
        
        [Header("Setup")]
        [SerializeField, Required] private Button m_BeginButton = null;
        [SerializeField, Required] private CanvasGroup m_SetupPanelGroup = null;
        [SerializeField, Required] private BestiaryAddPanel m_SelectEnvPanel = null;
        [SerializeField, Required] private Button m_BackButton = null;
        [SerializeField, Required] private Button m_CrittersButton = null;
        [SerializeField, Required] private BestiaryAddPanel m_AddCrittersPanel = null;
        [SerializeField, Required] private Button m_RunButton = null;

        [Header("Running")]
        [SerializeField, Required] private CanvasGroup m_InProgressGroup = null;
        [SerializeField, Required] private ObservationBehaviorSystem m_ActorBehavior = null;
        [SerializeField] private BehaviorCirclePool m_BehaviorCircles = null;
        [SerializeField, Required] private ParticleSystem m_EatEmojis = null;
        [SerializeField, Required] private Button m_FinishButton = null;
        [SerializeField, Required] private AmbientRenderer m_FinishButtonHighlight = null;
        [SerializeField, Required] private TMP_Text m_UnobservedStateLabel = null;

        [Header("Summary")]
        [SerializeField, Required] private SummaryPanel m_SummaryPanel = null;

        #endregion // Inspector

        [NonSerialized] private SetupPhase m_SetupPhase;
        [NonSerialized] private ActorWorld m_World;
        [NonSerialized] private BestiaryDesc m_SelectedEnvironment;
        [NonSerialized] private HashSet<BFBase> m_PotentialNewFacts = new HashSet<BFBase>();
        [NonSerialized] private int m_MissedFactCount = 0;
        [NonSerialized] private readonly List<ExperimentFactResult> m_FactResults = new List<ExperimentFactResult>();
        [NonSerialized] private Routine m_IdleRoutine;
        [NonSerialized] private float m_IdleUpdateCounter;
        [NonSerialized] private Routine m_DrainRoutine;
        [NonSerialized] private bool m_PlayerHadObservationChance;

        private void Awake()
        {
            m_ParentTank.ActivateMethod = Activate;
            m_ParentTank.DeactivateMethod = Deactivate;
            m_ParentTank.CanDeactivate = () => (m_ParentTank.CurrentState & TankState.Running) == 0;
            m_ParentTank.HasCritter = (s) => m_AddCrittersPanel.IsSelected(Assets.Bestiary(s));
            m_ParentTank.HasEnvironment = (s) => m_SelectedEnvironment?.Id() == s;

            m_AddCrittersPanel.OnAdded = OnCritterAdded;
            m_AddCrittersPanel.OnRemoved = OnCritterRemoved;
            m_AddCrittersPanel.OnCleared = OnCrittersCleared;

            m_SelectEnvPanel.OnAdded = OnEnvironmentAdded;
            m_SelectEnvPanel.OnRemoved = OnEnvironmentRemoved;
            m_SelectEnvPanel.OnCleared = OnEnvironmentCleared;

            m_CrittersButton.interactable = false;
            m_RunButton.interactable = false;

            m_BehaviorCircles.Initialize(null, null, 0);
            m_BehaviorCircles.Config.RegisterOnConstruct(OnCaptureConstructed);
            m_BehaviorCircles.Config.RegisterOnAlloc(OnCaptureAlloc);
            m_BehaviorCircles.Config.RegisterOnFree(OnCaptureFree);

            m_BeginButton.onClick.AddListener(OnBeginClick);
            m_CrittersButton.onClick.AddListener(OnSelectCrittersClick);
            m_BackButton.onClick.AddListener(OnBackClick);
            m_RunButton.onClick.AddListener(OnRunClick);
            m_FinishButton.onClick.AddListener(OnFinishClick);
            m_SummaryPanel.Button.onClick.AddListener(OnSummaryCloseClick);

            m_UnobservedStateLabel.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if ((m_ParentTank.CurrentState & TankState.Running) == 0 || Script.IsPaused)
                return;

            m_ActorBehavior.TickBehaviors(Time.deltaTime);
        }

        #region Tank

        private void Activate()
        {
            m_ActorBehavior.Initialize(this);
            ObservationBehaviorSystem.ConfigureStates();
            m_World = m_ActorBehavior.World;

            m_SetupPanelGroup.Hide();
            m_InProgressGroup.Hide();
            m_UnobservedStateLabel.gameObject.SetActive(false);

            m_BeginButton.gameObject.SetActive(true);
            m_DrainRoutine.Stop();
            TankWaterSystem.SetWaterHeight(m_ParentTank, 0);

            m_SetupPhase = SetupPhase.Begin;

            m_FactResults.Clear();
        }

        private void Deactivate()
        {
            m_FinishButtonHighlight.gameObject.SetActive(false);
            m_IdleRoutine.Stop();
            m_ActorBehavior.ClearAll();
            m_SelectEnvPanel.Hide();
            m_SelectEnvPanel.ClearSelection();
            m_AddCrittersPanel.Hide();
            m_AddCrittersPanel.ClearSelection();
            m_BehaviorCircles.Reset();
            m_FactResults.Clear();
            m_PotentialNewFacts.Clear();
            if (m_SummaryPanel.gameObject.activeSelf)
            {
                m_SummaryPanel.gameObject.SetActive(false);
                m_SummaryPanel.FactPools.FreeAll();
            }
            if (m_ParentTank.WaterFillProportion > 0) {
                m_DrainRoutine.Replace(this, m_ParentTank.WaterSystem.DrainWaterOverTime(m_ParentTank, 1.5f));
            }
            m_ParentTank.CurrentState = 0;
        }
        
        #endregion // Tank

        #region Critter Callbacks

        private void OnCritterAdded(BestiaryDesc inDesc)
        {
            m_RunButton.interactable = true;
            m_ActorBehavior.Alloc(inDesc.Id());
            Services.Events.Dispatch(ExperimentEvents.ExperimentAddCritter, inDesc.Id());
        }

        private void OnCritterRemoved(BestiaryDesc inDesc)
        {
            m_ActorBehavior.FreeAll(inDesc.Id());
            m_RunButton.interactable = m_World.Actors.Count > 0;
            Services.Events.Dispatch(ExperimentEvents.ExperimentRemoveCritter, inDesc.Id());
        }

        private void OnCrittersCleared()
        {
            m_RunButton.interactable = false;
            m_ActorBehavior.ClearActors();
            Services.Events.Dispatch(ExperimentEvents.ExperimentCrittersCleared);
        }

        #endregion // Critter Callbacks

        #region Environment Callbacks

        private void OnEnvironmentAdded(BestiaryDesc inDesc)
        {
            m_SelectedEnvironment = inDesc;
            m_CrittersButton.interactable = true;
            m_ActorBehavior.UpdateEnvState(inDesc.GetEnvironment());
            m_ParentTank.WaterColor.SetColor(inDesc.WaterColor().WithAlpha(m_ParentTank.DefaultWaterColor.a));
            Services.Events.Dispatch(ExperimentEvents.ExperimentAddEnvironment, inDesc.Id());
        }

        private void OnEnvironmentRemoved(BestiaryDesc inDesc)
        {
            if (Ref.CompareExchange(ref m_SelectedEnvironment, inDesc, null))
            {
                m_CrittersButton.interactable = false;
                m_ParentTank.WaterColor.SetColor(m_ParentTank.DefaultWaterColor);
                m_ActorBehavior.ClearEnvState();
                Services.Events.Dispatch(ExperimentEvents.ExperimentRemoveEnvironment, inDesc.Id());
            }
        }

        private void OnEnvironmentCleared()
        {
            m_SelectedEnvironment = null;
            m_CrittersButton.interactable = false;
            m_ParentTank.WaterColor.SetColor(m_ParentTank.DefaultWaterColor);
            m_ActorBehavior.ClearEnvState();
            Services.Events.Dispatch(ExperimentEvents.ExperimentEnvironmentCleared);
        }

        #endregion // Environment Callbacks

        #region Behavior Capture

        #region Pool Events

        private Action<BehaviorCaptureCircle> m_CachedOnCaptureClicked;
        private Action<BehaviorCaptureCircle> m_CachedOnCaptureDisposed;

        ScriptObject IScriptComponent.Parent => throw new NotImplementedException();

        private void OnCaptureConstructed(IPool<BehaviorCaptureCircle> inPool, BehaviorCaptureCircle inCircle)
        {
            inCircle.OnClick = m_CachedOnCaptureClicked ?? (m_CachedOnCaptureClicked = OnCaptureClicked);
            inCircle.OnDispose = m_CachedOnCaptureDisposed ?? (m_CachedOnCaptureDisposed = OnCaptureDisposed);
        }

        private void OnCaptureAlloc(IPool<BehaviorCaptureCircle> inPool, BehaviorCaptureCircle inCircle)
        {
            inCircle.Active = true;
            inCircle.UseCount++;
        }

        private void OnCaptureFree(IPool<BehaviorCaptureCircle> inPool, BehaviorCaptureCircle inCircle)
        {
            inCircle.Active = false;
            inCircle.Animation.Stop();
        }

        #endregion // Pool Events

        static public BehaviorCaptureCircle.TempAlloc CaptureCircle(StringHash32 inFactId, ActorInstance inLocation, ActorWorld inWorld, bool inbAlreadyHas)
        {
            ObservationTank tank = (ObservationTank) inWorld.Tag;
            if (inbAlreadyHas)
            {
                return default(BehaviorCaptureCircle.TempAlloc);
            }
            else
            {
                Services.Data.AddVariable("temp:behaviorCirclesSeen", 1);
                return tank.AllocCircle(inFactId, inLocation.CachedCollider.bounds.center);
            }
        }

        private BehaviorCaptureCircle.TempAlloc AllocCircle(StringHash32 inFactId, Vector3 inLocation)
        {
            BehaviorCaptureCircle circle = m_BehaviorCircles.Alloc();
            circle.FactId = inFactId;
            circle.Animation.Replace(circle, AnimateCircleOn(circle)).TryManuallyUpdate(0);
            circle.transform.localPosition = inLocation;
            return new BehaviorCaptureCircle.TempAlloc(circle);
        }

        private void OnCaptureClicked(BehaviorCaptureCircle inCircle)
        {
            Services.UI.WorldFaders.Flash(Color.white.WithAlpha(0.5f), 0.25f);
            Services.Audio.PostEvent("capture_flash");
            inCircle.OnDispose?.Invoke(inCircle);
            AttemptCaptureBehavior(inCircle.FactId);
            Services.Data.AddVariable("temp:behaviorCirclesClicked", 1);
        }

        private void OnCaptureDisposed(BehaviorCaptureCircle inCircle)
        {
            inCircle.Animation.Replace(inCircle, AnimateCircleOff(inCircle)).TryManuallyUpdate(0);
        }

        private void AttemptCaptureBehavior(StringHash32 inFactId)
        {
            if (Save.Bestiary.RegisterFact(inFactId))
            {
                foreach(var circle in m_BehaviorCircles.ActiveObjects)
                {
                    if (circle.Active && circle.FactId == inFactId)
                    {
                        circle.OnDispose?.Invoke(circle);
                    }
                }

                var factDef = Assets.Fact(inFactId);
                m_PotentialNewFacts.Remove(factDef);
                m_IdleUpdateCounter = m_PotentialNewFacts.Count > 0 ? ResetIdleDuration : NoFactsLeftIdleDuration;

                m_FactResults.Add(new ExperimentFactResult(inFactId, ExperimentFactResultType.NewFact, 0));

                Services.Audio.PostEvent("capture_new");

                Services.UI.Popup.PresentFact("'experiment.observation.newBehavior.header", null, null, factDef, BFType.DefaultDiscoveredFlags(factDef))
                    .OnComplete((r) => {
                        m_FinishButton.interactable = true;
                        using(var table = TempVarTable.Alloc())
                        {
                            table.Set("factId", inFactId);
                            Services.Script.TriggerResponse(ExperimentTriggers.NewBehaviorObserved, table);
                        }
                    });
            }
        }

        #region Animations

        private IEnumerator AnimateCircleOn(BehaviorCaptureCircle inCircle)
        {
            inCircle.Color.BlocksRaycasts = true;
            inCircle.Pointer.gameObject.SetActive(true);
            inCircle.Scale.SetScale(0.8f, Axis.XY);
            inCircle.Color.SetAlpha(0);
            yield return Routine.Combine(
                Tween.Float(inCircle.Color.GetAlpha(), 1, inCircle.Color.SetAlpha, 0.2f),
                inCircle.Scale.ScaleTo(1, 0.2f, Axis.XY).Ease(Curve.CubeOut)
            );
        }

        private IEnumerator AnimateCircleOff(BehaviorCaptureCircle inCircle)
        {
            inCircle.Active = false;
            inCircle.Color.BlocksRaycasts = false;
            inCircle.Pointer.gameObject.SetActive(false);
            yield return Routine.Combine(
                Tween.Float(inCircle.Color.GetAlpha(), 0, inCircle.Color.SetAlpha, 0.2f),
                inCircle.Scale.ScaleTo(0.8f, 0.2f, Axis.XY).Ease(Curve.CubeIn)
            );
            m_BehaviorCircles.Free(inCircle);
        }

        #endregion // Animations

        #endregion // Behavior Capture

        static public void EmitEmoji(BFBase inFact, ActorInstance inActor, ActorWorld inWorld)
        {
            if (!Save.Bestiary.HasFact(inFact.Id))
                return;
            
            ObservationTank tank = (ObservationTank) inWorld.Tag;
            ParticleSystem.EmitParams emit = default;
            emit.position = inActor.CachedCollider.bounds.center;
            tank.m_EatEmojis.Emit(emit, 1);
        }

        #region Sequence

        private void OnBeginClick() {
            Services.Input.PauseAll();
            m_BeginButton.gameObject.SetActive(false);
            m_RunButton.gameObject.SetActive(false);
            m_CrittersButton.gameObject.SetActive(true);
            m_SetupPhase = SetupPhase.Environment;
            Routine.Start(this, BeginSequence());
        }

        private IEnumerator BeginSequence() {
            m_SetupPanelGroup.interactable = false;
            yield return Routine.Combine(
                m_SetupPanelGroup.Show(0.2f),
                m_SelectEnvPanel.Show()
            );
            yield return m_DrainRoutine;
            m_SetupPanelGroup.interactable = true;
            Services.Input.ResumeAll();
        }

        private void OnSelectCrittersClick() {
            Services.Input.PauseAll();
            m_SetupPanelGroup.interactable = false;
            m_SetupPhase = SetupPhase.Critters;
            Routine.Start(this, FillTankSequence());
        }

        private IEnumerator FillTankSequence() {
            yield return Routine.Combine(
                m_SetupPanelGroup.Hide(0.2f),
                m_SelectEnvPanel.Hide()
            );

            yield return m_DrainRoutine;

            m_CrittersButton.gameObject.SetActive(false);
            m_RunButton.gameObject.SetActive(true);

            yield return m_ParentTank.WaterSystem.RequestFill(m_ParentTank);
            yield return 0.2f;

            yield return Routine.Combine(
                m_SetupPanelGroup.Show(0.2f),
                m_AddCrittersPanel.Show()
            );

            m_SetupPanelGroup.interactable = true;
            Services.Input.ResumeAll();
        }

        private void OnBackClick() {
            Services.Input.PauseAll();
            m_SetupPanelGroup.interactable = false;

            switch(m_SetupPhase) {
                case SetupPhase.Critters: {
                    m_SetupPhase = SetupPhase.Environment;
                    Routine.Start(this, BackToEnvironment());
                    break;
                }

                case SetupPhase.Environment: {
                    m_SetupPhase = SetupPhase.Begin;
                    Routine.Start(this, BackToBegin());
                    break;
                }
            }
        }

        private IEnumerator BackToEnvironment() {
            m_DrainRoutine.Replace(this, m_ParentTank.WaterSystem.DrainWaterOverTime(m_ParentTank, 1.5f));
            m_AddCrittersPanel.ClearSelection();
            
            yield return Routine.Combine(
                m_SetupPanelGroup.Hide(0.2f),
                m_AddCrittersPanel.Hide()
            );

            m_CrittersButton.gameObject.SetActive(true);
            m_RunButton.gameObject.SetActive(false);

            yield return m_DrainRoutine;

            yield return Routine.Combine(
                m_SelectEnvPanel.Show(),
                m_SetupPanelGroup.Show(0.2f)
            );
            m_SetupPanelGroup.interactable = true;
            Services.Input.ResumeAll();
        }

        private IEnumerator BackToBegin() {
            m_SelectEnvPanel.ClearSelection();
            
            yield return Routine.Combine(
                m_SetupPanelGroup.Hide(0.2f),
                m_SelectEnvPanel.Hide()
            );

            yield return m_DrainRoutine;

            m_BeginButton.gameObject.SetActive(true);

            Services.Input.ResumeAll();
        }

        private void OnRunClick()
        {
            m_ParentTank.CurrentState |= TankState.Running;

            m_AddCrittersPanel.Hide();
            m_SelectEnvPanel.Hide();

            Services.Input.PauseAll();

            Routine.Start(this, StartExperiment()).TryManuallyUpdate(0);
        }

        private IEnumerator StartExperiment() {
            m_SetupPanelGroup.blocksRaycasts = false;
            m_InProgressGroup.blocksRaycasts = false;
            m_FinishButton.interactable = false;

            while(!m_ActorBehavior.IsSpawningCompleted()) {
                yield return null;
            }

            Services.Input.ResumeAll();

            m_PotentialNewFacts.Clear();
            int potentialNewObservationsCount;
            using(Profiling.Time("getting potential observations")) {
                potentialNewObservationsCount = ObservationBehaviorSystem.GetPotentialNewObservations(m_World, Save.Bestiary.HasFact, m_PotentialNewFacts);
                Log.Msg("[ObservationTank] {0} potentially observable facts", potentialNewObservationsCount);
            }
            m_MissedFactCount = 0;

            using (var table = TempVarTable.Alloc())
            {
                table.Set("tankType", m_ParentTank.Type.ToString());
                table.Set("tankId", m_ParentTank.Id);
                table.Set("newFactsLeft", potentialNewObservationsCount);
                Services.Script.TriggerResponse(ExperimentTriggers.ExperimentStarted, table);
            }

            Services.Events.Dispatch(ExperimentEvents.ExperimentBegin, m_ParentTank.Type);

            m_UnobservedStateLabel.alpha = 0;
            m_UnobservedStateLabel.gameObject.SetActive(true);

            if (potentialNewObservationsCount > 0) {
                m_UnobservedStateLabel.SetText("?");
                m_UnobservedStateLabel.SetColor(ColorBank.Yellow);
            } else {
                m_UnobservedStateLabel.SetText("-");
                m_UnobservedStateLabel.SetColor(ColorBank.DarkGray);
            }

            yield return Routine.Combine(
                m_SetupPanelGroup.Hide(0.1f),
                m_InProgressGroup.Show(0.1f),
                m_UnobservedStateLabel.FadeTo(1, 0.1f)
            );

            m_IdleRoutine.Replace(this, IdleUpdate());

            if (potentialNewObservationsCount > 0) {
                Services.Audio.PostEvent("Experiment.HasNewBehaviors");
                yield return m_UnobservedStateLabel.transform.ScaleTo(1.02f, 0.2f, Axis.XY).Ease(Curve.CubeOut).Yoyo(true).RevertOnCancel();
                yield return 20;
            }

            m_FinishButton.interactable = true;
        }

        private IEnumerator IdleUpdate() {
            m_IdleUpdateCounter = StartingIdleDuration;
            bool bHadFacts = m_PotentialNewFacts.Count > 0;
            while(true) {
                while(m_IdleUpdateCounter > 0) {
                    if (!Script.ShouldBlock()) {
                        m_IdleUpdateCounter -= Routine.DeltaTime;
                    }
                    yield return null;
                }

                CullPotentialFactSet();
                Log.Msg("[ObservationTank] {0} new facts remaining", m_PotentialNewFacts.Count);
                m_IdleUpdateCounter = ResetIdleDuration;

                using (var table = TempVarTable.Alloc())
                {
                    table.Set("tankType", m_ParentTank.Type.ToString());
                    table.Set("tankId", m_ParentTank.Id);
                    table.Set("newFactsLeft", m_PotentialNewFacts.Count);
                    table.Set("missedFacts", m_MissedFactCount);
                    Services.Script.TriggerResponse(ExperimentTriggers.ExperimentIdle, table);
                }

                if (m_PotentialNewFacts.Count == 0 && bHadFacts) {
                    m_UnobservedStateLabel.SetText("-");
                    m_UnobservedStateLabel.SetColor(ColorBank.DarkGray);
                    m_FinishButtonHighlight.gameObject.SetActive(true);
                    bHadFacts = false;
                    Services.Audio.PostEvent("Experiment.FinishPrompt");
                }
            }
        }

        private void CullPotentialFactSet() {
            m_MissedFactCount += m_PotentialNewFacts.RemoveWhere((f) => !m_ActorBehavior.IsFactObservable(f));
        }

        private void OnFinishClick()
        {
            m_FinishButtonHighlight.gameObject.SetActive(false);
            m_IdleRoutine.Stop();
            m_ParentTank.CurrentState &= ~TankState.Running;

            foreach(var instance in m_World.Actors)
            {
                instance.ActionAnimation.Stop();
            }

            ExperimentResult result = new ExperimentResult();
            result.Facts = m_FactResults.ToArray();
            m_FactResults.Clear();

            Routine.Start(this, FinishExperiment(result)).TryManuallyUpdate(0);
        }

        private IEnumerator FinishExperiment(ExperimentResult inResult)
        {
            m_InProgressGroup.blocksRaycasts = false;
            Services.Input.PauseAll();
            Services.UI.ShowLetterbox();
            Services.Script.KillLowPriorityThreads();
            using(var fader = Services.UI.WorldFaders.AllocFader())
            {
                yield return fader.Object.Show(Color.black, 0.5f);
                yield return m_ParentTank.WaterSystem.DrainWaterOverTime(m_ParentTank, 1f);
                ClearStateAfterExperiment();
                yield return 0.5f;
                InitializeSummaryScreen(inResult);
                yield return fader.Object.Hide(0.5f, false);
            }
            yield return PopulateSummaryScreen(inResult);
            Services.UI.HideLetterbox();
            Services.Input.ResumeAll();
            
            using (var table = TempVarTable.Alloc())
            {
                table.Set("tankType", m_ParentTank.Type.ToString());
                table.Set("tankId", m_ParentTank.Id);
                Services.Script.TriggerResponse(ExperimentTriggers.ExperimentFinished, table);
            }

            Services.Events.Dispatch(ExperimentEvents.ExperimentEnded, m_ParentTank.Type);
        }

        private void ClearStateAfterExperiment()
        {
            TankWaterSystem.SetWaterHeight(m_ParentTank, 0);

            m_SelectEnvPanel.Hide();
            m_SelectEnvPanel.ClearSelection();

            m_AddCrittersPanel.Hide();
            m_AddCrittersPanel.ClearSelection();
            m_ActorBehavior.ClearActors();
            
            m_BehaviorCircles.Reset();
            m_ParentTank.CurrentState &= ~TankState.Running;

            m_InProgressGroup.alpha = 0;
            m_InProgressGroup.gameObject.SetActive(false);

            m_UnobservedStateLabel.gameObject.SetActive(false);
            m_PotentialNewFacts.Clear();
            m_MissedFactCount = 0;
        }

        private void InitializeSummaryScreen(ExperimentResult inResult)
        {
            m_SummaryPanel.gameObject.SetActive(true);

            if (inResult.Facts.Length == 0)
            {
                m_SummaryPanel.FactGroup.SetActive(false);
                m_SummaryPanel.HintGroup.SetActive(true);

                m_SummaryPanel.HeaderText.SetText("experiment.summary.header.noFacts");
                m_SummaryPanel.HeaderText.Graphic.color = AQColors.BrightBlue;
                return;
            }

            m_SummaryPanel.HintGroup.SetActive(false);
            m_SummaryPanel.FactGroup.SetActive(true);

            m_SummaryPanel.HeaderText.SetText("experiment.summary.header");
            m_SummaryPanel.HeaderText.Graphic.color = AQColors.HighlightYellow;
        }

        private IEnumerator PopulateSummaryScreen(ExperimentResult inResult)
        {
            MonoBehaviour newFact;
            foreach(var fact in inResult.Facts)
            {
                newFact = m_SummaryPanel.FactPools.Alloc(Assets.Fact(fact.Id), null, 0, m_SummaryPanel.FactListRoot);
                m_SummaryPanel.FactListLayout.ForceRebuild();
                yield return ExperimentUtil.AnimateFeedbackItemToOn(newFact, 1);
                yield return 0.2f;
            }
        }

        private void OnSummaryCloseClick()
        {
            m_SummaryPanel.gameObject.SetActive(false);
            m_SummaryPanel.FactPools.FreeAll();
            m_BeginButton.gameObject.SetActive(true);
        }

        #endregion // Sequence

        #region IScriptComponent

        void IScriptComponent.OnRegister(ScriptObject inObject) { }
        void IScriptComponent.OnDeregister(ScriptObject inObject) { }

        #endregion // IScriptComponent
    }
}