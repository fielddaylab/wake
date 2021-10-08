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
        private const float StartingIdleDuration = 30;
        private const float ResetIdleDuration = 20;
        private const float NoFactsLeftIdleDuration = 10;

        [Serializable] private class BehaviorCirclePool : SerializablePool<BehaviorCaptureCircle> { }
        
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private SelectableTank m_ParentTank = null;
        
        [Header("Setup")]
        [SerializeField, Required] private CanvasGroup m_BottomPanelGroup = null;
        [SerializeField, Required] private BestiaryAddPanel m_AddCrittersPanel = null;
        [SerializeField, Required] private BestiaryAddPanel m_SelectEnvPanel = null;
        [SerializeField, Required(ComponentLookupDirection.Children)] private EnvIconDisplay m_EnvIcon = null;
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

        [NonSerialized] private ActorWorld m_World;
        [NonSerialized] private BestiaryDesc m_SelectedEnvironment;
        [NonSerialized] private HashSet<BFBase> m_PotentialNewFacts = new HashSet<BFBase>();
        [NonSerialized] private int m_MissedFactCount = 0;
        [NonSerialized] private readonly List<ExperimentFactResult> m_FactResults = new List<ExperimentFactResult>();
        [NonSerialized] private Routine m_IdleRoutine;
        [NonSerialized] private float m_IdleUpdateCounter;

        private void Awake()
        {
            m_ParentTank.ActivateMethod = Activate;
            m_ParentTank.DeactivateMethod = Deactivate;
            m_ParentTank.CanDeactivate = () => !m_ParentTank.IsRunning;
            m_ParentTank.HasCritter = (s) => m_AddCrittersPanel.IsSelected(Assets.Bestiary(s));
            m_ParentTank.HasEnvironment = (s) => m_SelectedEnvironment?.Id() == s;

            m_AddCrittersPanel.OnAdded = OnCritterAdded;
            m_AddCrittersPanel.OnRemoved = OnCritterRemoved;
            m_AddCrittersPanel.OnCleared = OnCrittersCleared;

            m_SelectEnvPanel.OnAdded = OnEnvironmentAdded;
            m_SelectEnvPanel.OnRemoved = OnEnvironmentRemoved;
            m_SelectEnvPanel.OnCleared = OnEnvironmentCleared;

            m_RunButton.interactable = false;

            m_BehaviorCircles.Initialize(null, null, 0);
            m_BehaviorCircles.Config.RegisterOnConstruct(OnCaptureConstructed);
            m_BehaviorCircles.Config.RegisterOnAlloc(OnCaptureAlloc);
            m_BehaviorCircles.Config.RegisterOnFree(OnCaptureFree);

            m_RunButton.onClick.AddListener(OnRunClick);
            m_FinishButton.onClick.AddListener(OnFinishClick);
            m_SummaryPanel.ContinueButton.onClick.AddListener(OnSummaryCloseClick);

            m_UnobservedStateLabel.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!m_ParentTank.IsRunning || Services.Pause.IsPaused())
                return;

            m_ActorBehavior.TickBehaviors(Time.deltaTime);
        }

        #region Tank

        private void Activate()
        {
            m_ActorBehavior.Initialize(this);
            ObservationBehaviorSystem.ConfigureStates();
            m_World = m_ActorBehavior.World;

            EnvIconDisplay.Populate(m_EnvIcon, null);

            m_BottomPanelGroup.alpha = 1;
            m_BottomPanelGroup.blocksRaycasts = true;
            m_BottomPanelGroup.gameObject.SetActive(true);

            m_InProgressGroup.alpha = 0;
            m_InProgressGroup.blocksRaycasts = false;
            m_InProgressGroup.gameObject.SetActive(false);
            m_UnobservedStateLabel.gameObject.SetActive(false);

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
            m_ParentTank.IsRunning = false;
        }
        
        #endregion // Tank

        #region Critter Callbacks

        private void OnCritterAdded(BestiaryDesc inDesc)
        {
            m_RunButton.interactable = m_SelectedEnvironment != null;
            m_ActorBehavior.Alloc(inDesc.Id());
        }

        private void OnCritterRemoved(BestiaryDesc inDesc)
        {
            m_ActorBehavior.FreeAll(inDesc.Id());
            m_RunButton.interactable = m_World.Actors.Count > 0 && m_SelectedEnvironment != null;
        }

        private void OnCrittersCleared()
        {
            m_RunButton.interactable = false;
            m_ActorBehavior.ClearActors();
        }

        #endregion // Critter Callbacks

        #region Environment Callbacks

        private void OnEnvironmentAdded(BestiaryDesc inDesc)
        {
            m_SelectedEnvironment = inDesc;
            m_RunButton.interactable = m_World.Actors.Count > 0;
            EnvIconDisplay.Populate(m_EnvIcon, inDesc);
            m_ActorBehavior.UpdateEnvState(inDesc.GetEnvironment());
            m_ParentTank.WaterColor.SetColor(inDesc.WaterColor().WithAlpha(m_ParentTank.DefaultWaterColor.a));
        }

        private void OnEnvironmentRemoved(BestiaryDesc inDesc)
        {
            if (Ref.CompareExchange(ref m_SelectedEnvironment, inDesc, null))
            {
                m_RunButton.interactable = false;
                m_ParentTank.WaterColor.SetColor(m_ParentTank.DefaultWaterColor);
                EnvIconDisplay.Populate(m_EnvIcon, null);
                m_ActorBehavior.ClearEnvState();
            }
        }

        private void OnEnvironmentCleared()
        {
            m_SelectedEnvironment = null;
            m_RunButton.interactable = false;
            m_ParentTank.WaterColor.SetColor(m_ParentTank.DefaultWaterColor);
            EnvIconDisplay.Populate(m_EnvIcon, null);
            m_ActorBehavior.ClearEnvState();
        }

        #endregion // Environment Callbacks\

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
            if (Services.Data.Profile.Bestiary.RegisterFact(inFactId))
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

                Services.UI.Popup.PresentFact("'experiment.observation.newBehavior.header", null, factDef, BFType.DefaultDiscoveredFlags(factDef))
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
            if (!Services.Data.Profile.Bestiary.HasFact(inFact.Id))
                return;
            
            ObservationTank tank = (ObservationTank) inWorld.Tag;
            ParticleSystem.EmitParams emit = default;
            emit.position = inActor.CachedCollider.bounds.center;
            tank.m_EatEmojis.Emit(emit, 1);
        }

        #region Sequence

        private void OnRunClick()
        {
            m_ParentTank.IsRunning = true;

            m_AddCrittersPanel.Hide();
            m_SelectEnvPanel.Hide();

            Routine.Start(this, StartExperiment()).TryManuallyUpdate(0);
        }

        private IEnumerator StartExperiment()
        {
            m_BottomPanelGroup.blocksRaycasts = false;
            m_InProgressGroup.blocksRaycasts = false;
            m_ParentTank.BackClickable.gameObject.SetActive(false);
            m_FinishButton.interactable = false;

            while(!m_ActorBehavior.IsSpawningCompleted()) {
                yield return null;
            }

            m_PotentialNewFacts.Clear();
            int potentialNewObservationsCount;
            using(Profiling.Time("getting potential observations")) {
                potentialNewObservationsCount = m_ActorBehavior.GetPotentialNewObservations(Services.Data.Profile.Bestiary.HasFact, m_PotentialNewFacts);
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
                m_BottomPanelGroup.Hide(0.1f, false),
                m_InProgressGroup.Show(0.1f, true),
                m_UnobservedStateLabel.FadeTo(1, 0.1f),
                Tween.OneToZero(m_ParentTank.BackIndicators.SetAlpha, 0.1f)
            );

            m_IdleRoutine.Replace(this, IdleUpdate());

            if (potentialNewObservationsCount > 0) {
                Services.Audio.PostEvent("Experiment.HasNewBehaviors");
                yield return m_UnobservedStateLabel.transform.ScaleTo(1.02f, 0.2f, Axis.XY).Ease(Curve.CubeOut).Yoyo(true).RevertOnCancel();
                yield return 15;
            }

            m_FinishButton.interactable = true;
        }

        private IEnumerator IdleUpdate() {
            m_IdleUpdateCounter = StartingIdleDuration;
            bool bHadFacts = m_PotentialNewFacts.Count > 0;
            while(true) {
                while(m_IdleUpdateCounter > 0) {
                    if (!Services.UI.IsLetterboxed() && !Services.UI.Popup.IsShowing()) {
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
            m_ParentTank.IsRunning = false;

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
            m_SelectEnvPanel.Hide();

            m_AddCrittersPanel.Hide();
            m_AddCrittersPanel.ClearSelection();
            m_ActorBehavior.ClearActors();
            
            m_BehaviorCircles.Reset();
            m_ParentTank.IsRunning = false;

            m_ParentTank.BackClickable.gameObject.SetActive(true);
            m_ParentTank.BackIndicators.SetAlpha(1);

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
                m_SummaryPanel.HasFacts.SetActive(false);
                m_SummaryPanel.NoFacts.SetActive(true);
                return;
            }

            m_SummaryPanel.NoFacts.SetActive(false);
            m_SummaryPanel.HasFacts.SetActive(true);
        }

        private IEnumerator PopulateSummaryScreen(ExperimentResult inResult)
        {
            MonoBehaviour newFact;
            foreach(var fact in inResult.Facts)
            {
                newFact = m_SummaryPanel.FactPools.Alloc(Assets.Fact(fact.Id), null, 0, m_SummaryPanel.FactListRoot);
                m_SummaryPanel.FactListLayout.ForceRebuild();
                yield return ExperimentUtil.AnimateFeedbackItemToOn(newFact, 1);
                yield return 0.1f;
            }
        }

        private void OnSummaryCloseClick()
        {
            m_SummaryPanel.gameObject.SetActive(false);
            m_SummaryPanel.FactPools.FreeAll();
            Routine.Start(this, m_BottomPanelGroup.Show(0.1f, true));
        }

        #endregion // Sequence

        #region IScriptComponent

        void IScriptComponent.OnRegister(ScriptObject inObject) { }
        void IScriptComponent.OnDeregister(ScriptObject inObject) { }

        #endregion // IScriptComponent
    }
}