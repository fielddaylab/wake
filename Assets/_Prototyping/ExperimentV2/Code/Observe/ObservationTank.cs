using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Animation;
using Aqua.Scripting;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2 {
    public class ObservationTank : MonoBehaviour, IScriptComponent {
        private enum SetupPhase : byte {
            Begin,
            Environment,
            Critters,
            Run
        }

        private const float StartingIdleDuration = 25;
        private const float ResetIdleDuration = 15;
        private const float NoFactsLeftIdleDuration = 5;

        [Serializable] private class BehaviorCirclePool : SerializablePool<BehaviorCaptureCircle> { }

        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private SelectableTank m_ParentTank = null;

        [Header("Setup")]
        [SerializeField, Required] private ExperimentHeaderUI m_HeaderUI = null;
        [SerializeField, Required] private ExperimentScreen m_BeginScreen = null;
        [SerializeField, Required] private ExperimentScreen m_EnvironmentScreen = null;
        [SerializeField, Required] private ExperimentScreen m_OrganismScreen = null;

        [Header("Running")]
        [SerializeField, Required] private ExperimentScreen m_RunningScreen = null;
        [SerializeField] private BehaviorCirclePool m_BehaviorCircles = null;
        [SerializeField, Required] private AmbientRenderer m_FinishButtonHighlight = null;
        [SerializeField, Required] private TMP_Text m_UnobservedStateLabel = null;

        #endregion // Inspector

        [NonSerialized] private ActorWorld m_World;
        [NonSerialized] private SetupPhase m_SetupPhase;
        
        [NonSerialized] private BestiaryDesc m_SelectedEnvironment;
        [NonSerialized] private HashSet<BFBase> m_PotentialNewFacts = Collections.NewSet<BFBase>(8);
        [NonSerialized] private int m_MissedFactCount = 0;
        [NonSerialized] private int m_AlreadyKnownFactCount = 0;
        [NonSerialized] private readonly List<ExperimentFactResult> m_FactResults = new List<ExperimentFactResult>();
        
        [NonSerialized] private Routine m_IdleRoutine;
        [NonSerialized] private float m_IdleUpdateCounter;

        private void Awake() {
            m_ParentTank.ActivateMethod = Activate;
            m_ParentTank.DeactivateMethod = Deactivate;
            m_ParentTank.CanDeactivate = () => (m_ParentTank.CurrentState & TankState.Running) == 0;
            m_ParentTank.HasCritter = (s) => m_OrganismScreen.Panel.IsSelected(Assets.Bestiary(s));
            m_ParentTank.HasEnvironment = (s) => m_SelectedEnvironment?.Id() == s;

            m_EnvironmentScreen.Panel.OnAdded += OnEnvironmentAdded;
            m_EnvironmentScreen.Panel.OnRemoved += OnEnvironmentRemoved;
            m_EnvironmentScreen.Panel.OnCleared += OnEnvironmentCleared;

            m_OrganismScreen.Panel.HighlightFilter = EvaluateOrganismHighlight;
            m_OrganismScreen.Panel.MarkerFilter = EvaluateOrganismMarker;

            m_BehaviorCircles.Initialize(null, null, 0);
            m_BehaviorCircles.Config.RegisterOnConstruct(OnCaptureConstructed);
            m_BehaviorCircles.Config.RegisterOnAlloc(OnCaptureAlloc);
            m_BehaviorCircles.Config.RegisterOnFree(OnCaptureFree);

            m_BeginScreen.CustomButton.onClick.AddListener(OnNextClick);
            m_HeaderUI.BackButton.onClick.AddListener(OnBackClick);
            m_HeaderUI.NextButton.onClick.AddListener(OnNextClick);
            m_RunningScreen.CustomButton.onClick.AddListener(OnFinishClick);

            m_UnobservedStateLabel.gameObject.SetActive(false);

            SelectableTank.InitNavArrows(m_ParentTank);
        }

        #region Tank

        private void Activate() {
            m_World = m_ParentTank.ActorBehavior.World;

            m_UnobservedStateLabel.gameObject.SetActive(false);

            m_SetupPhase = SetupPhase.Begin;
            ExperimentScreen.Transition(m_BeginScreen, m_World);

            m_FactResults.Clear();
        }

        private void Deactivate() {
            m_FinishButtonHighlight.gameObject.SetActive(false);
            m_IdleRoutine.Stop();
            m_BehaviorCircles.Reset();
            m_FactResults.Clear();
            m_PotentialNewFacts.Clear();
            m_ParentTank.CurrentState = 0;
        }

        #endregion // Tank

        #region Environment Callbacks

        private void OnEnvironmentAdded(BestiaryDesc inDesc) {
            m_SelectedEnvironment = inDesc;
            m_ParentTank.ActorBehavior.UpdateEnvState(inDesc.GetEnvironment());
            m_OrganismScreen.Panel.Refresh();
        }

        private void OnEnvironmentRemoved(BestiaryDesc inDesc) {
            if (Ref.CompareExchange(ref m_SelectedEnvironment, inDesc, null)) {
                m_ParentTank.ActorBehavior.ClearEnvState();
            }
        }

        private void OnEnvironmentCleared() {
            m_SelectedEnvironment = null;
            m_ParentTank.ActorBehavior.ClearEnvState();
        }

        private bool EvaluateOrganismHighlight(BestiaryDesc organism) {
            return m_SelectedEnvironment?.HasOrganism(organism.Id()) ?? false;
        }

        private bool EvaluateOrganismMarker(BestiaryDesc organism) {
            StringHash32 stressFactToCheck = organism.FirstStressedFactId();
            if (!stressFactToCheck.IsEmpty && m_SelectedEnvironment && Save.Bestiary.HasFact(stressFactToCheck)) {
                return organism.EvaluateActorState(m_SelectedEnvironment.GetEnvironment(), out var _) >= ActorStateId.Stressed;
            }
            return false;
        }

        #endregion // Environment Callbacks

        #region Behavior Capture

        #region Pool Events

        private Action<BehaviorCaptureCircle> m_CachedOnCaptureClicked;
        private Action<BehaviorCaptureCircle> m_CachedOnCaptureDisposed;

        ScriptObject IScriptComponent.Parent => throw new NotImplementedException();

        private void OnCaptureConstructed(IPool<BehaviorCaptureCircle> inPool, BehaviorCaptureCircle inCircle) {
            inCircle.OnClick = m_CachedOnCaptureClicked ?? (m_CachedOnCaptureClicked = OnCaptureClicked);
            inCircle.OnDispose = m_CachedOnCaptureDisposed ?? (m_CachedOnCaptureDisposed = OnCaptureDisposed);
        }

        private void OnCaptureAlloc(IPool<BehaviorCaptureCircle> inPool, BehaviorCaptureCircle inCircle) {
            inCircle.Active = true;
            inCircle.UseCount++;
        }

        private void OnCaptureFree(IPool<BehaviorCaptureCircle> inPool, BehaviorCaptureCircle inCircle) {
            inCircle.Active = false;
            inCircle.Animation.Stop();
        }

        #endregion // Pool Events

        static public BehaviorCaptureCircle.TempAlloc CaptureCircle(StringHash32 inFactId, ActorInstance inLocation, ActorWorld inWorld, bool inbAlreadyHas) {
            if (inWorld.Tank.Type != TankType.Observation) {
                return default(BehaviorCaptureCircle.TempAlloc);
            }

            ObservationTank tank = (ObservationTank) inWorld.Tag;
            if (inbAlreadyHas) {
                return default(BehaviorCaptureCircle.TempAlloc);
            } else {
                Services.Data.AddVariable("temp:behaviorCirclesSeen", 1);
                return tank.AllocCircle(inFactId, inLocation.CachedCollider.bounds.center);
            }
        }

        private BehaviorCaptureCircle.TempAlloc AllocCircle(StringHash32 inFactId, Vector3 inLocation) {
            BehaviorCaptureCircle circle = m_BehaviorCircles.Alloc();
            circle.FactId = inFactId;
            circle.Animation.Replace(circle, AnimateCircleOn(circle)).Tick();
            circle.transform.localPosition = inLocation;
            return new BehaviorCaptureCircle.TempAlloc(circle);
        }

        private void OnCaptureClicked(BehaviorCaptureCircle inCircle) {
            Services.UI.WorldFaders.Flash(Color.white.WithAlpha(0.5f), 0.25f);
            Services.Audio.PostEvent("capture_flash");
            inCircle.OnDispose?.Invoke(inCircle);
            AttemptCaptureBehavior(inCircle.FactId);
            Services.Data.AddVariable("temp:behaviorCirclesClicked", 1);
        }

        private void OnCaptureDisposed(BehaviorCaptureCircle inCircle) {
            inCircle.Animation.Replace(inCircle, AnimateCircleOff(inCircle)).Tick();
        }

        private void AttemptCaptureBehavior(StringHash32 inFactId) {
            if (Save.Bestiary.RegisterFact(inFactId)) {
                foreach (var circle in m_BehaviorCircles.ActiveObjects) {
                    if (circle.Active && circle.FactId == inFactId) {
                        circle.OnDispose?.Invoke(circle);
                    }
                }

                var factDef = Assets.Fact(inFactId);
                m_PotentialNewFacts.Remove(factDef);
                m_IdleUpdateCounter = m_PotentialNewFacts.Count > 0 ? ResetIdleDuration : NoFactsLeftIdleDuration;

                m_FactResults.Add(new ExperimentFactResult(inFactId, ExperimentFactResultType.NewFact, 0));

                Services.Audio.PostEvent("capture_new");

                Services.UI.Popup.PresentFact(Loc.Find("experiment.observation.newBehavior.header"), null, null, factDef, BFType.DefaultDiscoveredFlags(factDef))
                    .OnComplete((r) => {
                        m_RunningScreen.CustomButton.interactable = true;
                        using (var table = TempVarTable.Alloc()) {
                            table.Set("factId", inFactId);
                            Services.Script.TriggerResponse(ExperimentTriggers.NewBehaviorObserved, table);
                        }
                    });
            }
        }

        #region Animations

        private IEnumerator AnimateCircleOn(BehaviorCaptureCircle inCircle) {
            inCircle.Color.BlocksRaycasts = true;
            inCircle.Pointer.gameObject.SetActive(true);
            inCircle.Scale.SetScale(0.8f, Axis.XY);
            inCircle.Color.SetAlpha(0);
            yield return Routine.Combine(
                Tween.Float(inCircle.Color.GetAlpha(), 1, inCircle.Color.SetAlpha, 0.2f),
                inCircle.Scale.ScaleTo(1, 0.2f, Axis.XY).Ease(Curve.CubeOut)
            );
        }

        private IEnumerator AnimateCircleOff(BehaviorCaptureCircle inCircle) {
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

        #region Sequence

        private void OnNextClick() {
            m_SetupPhase++;
            switch (m_SetupPhase) {
                case SetupPhase.Environment: {
                        ExperimentScreen.Transition(m_EnvironmentScreen, m_World);
                        break;
                    }
                case SetupPhase.Critters: {
                        ExperimentScreen.Transition(m_OrganismScreen, m_World, SelectableTank.FillTankSequence(m_ParentTank));
                        break;
                    }
                case SetupPhase.Run: {
                        SelectableTank.SetNavArrowsActive(m_ParentTank, false);
                        m_ParentTank.CurrentState |= TankState.Running;
                        ExperimentScreen.Transition(null, m_World, SelectableTank.SpawnSequence(m_ParentTank, m_OrganismScreen.Panel), () => {
                            Routine.Start(this, StartExperiment()).Tick();
                        });
                        break;
                    }
            }
        }

        private void OnBackClick() {
            m_SetupPhase--;
            switch (m_SetupPhase) {
                case SetupPhase.Environment: {
                        ExperimentScreen.Transition(m_EnvironmentScreen, m_World);
                        break;
                    }
            }
        }

        private IEnumerator StartExperiment() {
            m_ParentTank.ActorBehavior.Begin();
            yield return null;

            Services.Camera.MoveToPose(m_ParentTank.ZoomPose, 0.4f);
            m_ParentTank.Guide.MoveTo(m_ParentTank.GuideTargetZoomed);
            
            m_PotentialNewFacts.Clear();
            int potentialNewObservationsCount, alreadyKnownFacts;
            using (Profiling.Time("getting potential observations")) {
                potentialNewObservationsCount = ActorBehaviorSystem.GetPotentialNewObservations(m_World, Save.Bestiary.HasFact, m_PotentialNewFacts, out alreadyKnownFacts);
                Log.Msg("[ObservationTank] {0} potentially observable facts", potentialNewObservationsCount);
            }
            m_MissedFactCount = 0;
            m_AlreadyKnownFactCount = alreadyKnownFacts;

            using (var table = TempVarTable.Alloc()) {
                table.Set("tankType", m_ParentTank.Type.ToString());
                table.Set("tankId", m_ParentTank.Id);
                table.Set("newFactsLeft", potentialNewObservationsCount);
                Services.Script.TriggerResponse(ExperimentTriggers.ExperimentStarted, table);
            }

            Services.Events.Dispatch(ExperimentEvents.ExperimentBegin, m_ParentTank.Type);

            // m_UnobservedStateLabel.alpha = 0;
            // m_UnobservedStateLabel.gameObject.SetActive(true);

            // if (potentialNewObservationsCount > 0) {
            //     m_UnobservedStateLabel.SetText("?");
            //     m_UnobservedStateLabel.SetColor(ColorBank.Yellow);
            // } else {
            //     m_UnobservedStateLabel.SetText("-");
            //     m_UnobservedStateLabel.SetColor(ColorBank.DarkGray);
            // }

            m_RunningScreen.CustomButton.interactable = false;

            yield return ExperimentScreen.Transition(m_RunningScreen, m_World);

            m_IdleRoutine.Replace(this, IdleUpdate());

            if (potentialNewObservationsCount > 0) {
                Services.Audio.PostEvent("Experiment.HasNewBehaviors");
                m_RunningScreen.CustomButton.interactable = false;
                // yield return m_UnobservedStateLabel.transform.ScaleTo(1.02f, 0.2f, Axis.XY).Ease(Curve.CubeOut).Yoyo(true).RevertOnCancel();
                yield return 15;
            }

            m_RunningScreen.CustomButton.interactable = true;
        }

        private IEnumerator IdleUpdate() {
            m_IdleUpdateCounter = StartingIdleDuration;
            bool bHadFacts = m_PotentialNewFacts.Count > 0;
            while (true) {
                while (m_IdleUpdateCounter > 0) {
                    if (!Script.ShouldBlock()) {
                        m_IdleUpdateCounter -= Routine.DeltaTime;
                    }
                    yield return null;
                }

                CullPotentialFactSet();
                Log.Msg("[ObservationTank] {0} new facts remaining", m_PotentialNewFacts.Count);
                m_IdleUpdateCounter = ResetIdleDuration;

                using (var table = TempVarTable.Alloc()) {
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
            m_MissedFactCount += m_PotentialNewFacts.RemoveWhere((f) => !m_ParentTank.ActorBehavior.IsFactObservable(f));
        }

        private void OnFinishClick() {
            m_FinishButtonHighlight.gameObject.SetActive(false);
            m_IdleRoutine.Stop();
            m_ParentTank.CurrentState &= ~TankState.Running;
            m_ParentTank.ActorBehavior.End();

            ExperimentResult result = new ExperimentResult();
            result.Facts = m_FactResults.ToArray();
            if (m_MissedFactCount > 0) {
                result.Feedback |= ExperimentFeedbackFlags.MissedObservations;
            } else if (result.Facts.Length == 0) {
                if (m_World.EnvDeaths > 0) {
                    result.Feedback |= ExperimentFeedbackFlags.DeadOrganisms;
                } else if (m_PotentialNewFacts.Count > 0) {
                    result.Feedback |= ExperimentFeedbackFlags.HadObservationsRemaining;
                } else if (m_OrganismScreen.Panel.Selected.Count > 1) {
                    if (m_AlreadyKnownFactCount > 0) {
                        result.Feedback |= ExperimentFeedbackFlags.AlreadyObserved;
                    } else {
                        result.Feedback |= ExperimentFeedbackFlags.NoNewObservations;
                    }
                } else {
                    result.Feedback |= ExperimentFeedbackFlags.SingleOrganism;
                }
            }
            m_FactResults.Clear();

            Routine.Start(this, FinishExperiment(result)).Tick();
        }

        private IEnumerator FinishExperiment(ExperimentResult inResult) {
            using(Script.DisableInput())
            using(Script.Letterbox()) {
                Services.Script.KillLowPriorityThreads();
                using (var fader = Services.UI.WorldFaders.AllocFader()) {
                    yield return fader.Object.Show(Color.black, 0.5f);
                    ClearStateAfterExperiment();
                    yield return 0.5f;
                    yield return fader.Object.Hide(0.5f, false);
                }
            }

            using(Script.Letterbox()) {
                yield return ExperimentUtil.DisplaySummaryPopup(inResult);

                using (var table = TempVarTable.Alloc()) {
                    table.Set("tankType", m_ParentTank.Type.ToString());
                    table.Set("tankId", m_ParentTank.Id);
                    Services.Script.TriggerResponse(ExperimentTriggers.ExperimentFinished, table);
                }

                Services.Events.Dispatch(ExperimentEvents.ExperimentEnded, m_ParentTank.Type);
                ExperimentScreen.Transition(m_BeginScreen, m_World);
            }
        }

        private void ClearStateAfterExperiment() {
            SelectableTank.Reset(m_ParentTank, true);
            Services.Camera.SnapToPose(m_ParentTank.CameraPose);
            m_ParentTank.Guide.SnapTo(m_ParentTank.GuideTarget);

            m_BehaviorCircles.Reset();
            m_ParentTank.CurrentState &= ~TankState.Running;
            m_SetupPhase = 0;

            m_UnobservedStateLabel.gameObject.SetActive(false);
            m_PotentialNewFacts.Clear();
            m_MissedFactCount = 0;

            SelectableTank.SetNavArrowsActive(m_ParentTank, true);
        }

        #endregion // Sequence

        #region IScriptComponent

        void IScriptComponent.OnRegister(ScriptObject inObject) { }
        void IScriptComponent.OnDeregister(ScriptObject inObject) { }
        void IScriptComponent.PostRegister() { }

        #endregion // IScriptComponent
    }
}