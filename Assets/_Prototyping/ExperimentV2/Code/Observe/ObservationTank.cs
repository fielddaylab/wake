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
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class ObservationTank : MonoBehaviour, IScriptComponent
    {
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
        [SerializeField, Required] private AmbientRenderer m_CameraBlinking = null;
        [SerializeField] private BehaviorCirclePool m_BehaviorCircles = null;
        [SerializeField, Required] private ParticleSystem m_EatEmojis = null;
        [SerializeField, Required] private Button m_FinishButton = null;

        [Header("Summary")]
        [SerializeField, Required] private SummaryPanel m_SummaryPanel = null;

        #endregion // Inspector

        [NonSerialized] private ActorWorld m_World;
        [NonSerialized] private BestiaryDesc m_SelectedEnvironment;
        [NonSerialized] private bool m_IsRunning;
        [NonSerialized] private readonly List<ExperimentFactResult> m_FactResults = new List<ExperimentFactResult>();

        private void Awake()
        {
            m_ParentTank.ActivateMethod = Activate;
            m_ParentTank.DeactivateMethod = Deactivate;
            m_ParentTank.CanDeactivate = () => !m_IsRunning;

            m_AddCrittersPanel.OnAdded = OnCritterAdded;
            m_AddCrittersPanel.OnRemoved = OnCritterRemoved;
            m_AddCrittersPanel.OnCleared = OnCrittersCleared;

            m_SelectEnvPanel.OnAdded = OnEnvironmentAdded;
            m_SelectEnvPanel.OnRemoved = OnEnvironmentRemoved;
            m_SelectEnvPanel.OnCleared = OnEnvironmentCleared;

            m_RunButton.interactable = false;
            m_CameraBlinking.enabled = false;

            m_BehaviorCircles.Initialize(null, null, 0);
            m_BehaviorCircles.Config.RegisterOnConstruct(OnCaptureConstructed);
            m_BehaviorCircles.Config.RegisterOnAlloc(OnCaptureAlloc);
            m_BehaviorCircles.Config.RegisterOnFree(OnCaptureFree);

            m_RunButton.onClick.AddListener(OnRunClick);
            m_FinishButton.onClick.AddListener(OnFinishClick);
            m_SummaryPanel.ContinueButton.onClick.AddListener(OnSummaryCloseClick);
        }

        private void LateUpdate()
        {
            if (!m_IsRunning || Services.Pause.IsPaused())
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

            m_FactResults.Clear();
        }

        private void Deactivate()
        {
            m_ActorBehavior.ClearAll();
            m_SelectEnvPanel.Hide();
            m_SelectEnvPanel.ClearSelection();
            m_AddCrittersPanel.Hide();
            m_AddCrittersPanel.ClearSelection();
            m_BehaviorCircles.Reset();
            m_FactResults.Clear();
            if (m_SummaryPanel.gameObject.activeSelf)
            {
                m_SummaryPanel.gameObject.SetActive(false);
                m_SummaryPanel.FactPools.FreeAll();
            }
            m_IsRunning = false;
            m_CameraBlinking.enabled = false;
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
        }

        private void OnEnvironmentRemoved(BestiaryDesc inDesc)
        {
            if (Ref.CompareExchange(ref m_SelectedEnvironment, inDesc, null))
            {
                m_RunButton.interactable = false;
                EnvIconDisplay.Populate(m_EnvIcon, null);
                m_ActorBehavior.ClearEnvState();
            }
        }

        private void OnEnvironmentCleared()
        {
            m_SelectedEnvironment = null;
            m_RunButton.interactable = false;
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

        static public BehaviorCaptureCircle.TempAlloc CaptureCircle(StringHash32 inFactId, ActorInstance inLocation, ActorWorld inWorld)
        {
            ObservationTank tank = (ObservationTank) inWorld.Tag;
            return tank.AllocCircle(inFactId, inLocation.CachedCollider.bounds.center);
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
        }

        private void OnCaptureDisposed(BehaviorCaptureCircle inCircle)
        {
            inCircle.Animation.Replace(inCircle, AnimateCircleOff(inCircle)).TryManuallyUpdate(0);
        }

        private void AttemptCaptureBehavior(StringHash32 inFactId)
        {
            if (Services.Data.Profile.Bestiary.RegisterFact(inFactId))
            {
                var factDef = Services.Assets.Bestiary.Fact(inFactId);
                m_FactResults.Add(new ExperimentFactResult(inFactId, ExperimentFactResultType.NewFact, 0));

                Services.Audio.PostEvent("capture_new");

                Services.UI.Popup.Display("'experiment.observation.newBehavior.header", factDef.GenerateSentence())
                    .OnComplete((r) => {
                        using(var table = TempVarTable.Alloc())
                        {
                            table.Set("factId", inFactId);
                            Services.Script.TriggerResponse(ExperimentTriggers.NewBehaviorObserved);
                        }
                    });
            }
            else
            {
                using(var table = TempVarTable.Alloc())
                {
                    table.Set("factId", inFactId);
                    Services.Script.TriggerResponse(ExperimentTriggers.BehaviorAlreadyObserved);
                }
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

        static private bool HasUnobservedFacts(ActorWorld inWorld)
        {
            BestiaryData saveData = Services.Data.Profile.Bestiary;
            foreach(var pop in inWorld.ActorCounts)
            {
                if (pop.Population == 0)
                    continue;

                // any eating facts
                ActorDefinition def = inWorld.Allocator.Define(pop.Id);
                ActorStateId state = def.StateEvaluator.Evaluate(inWorld.Water);
                var eatTargets = ActorDefinition.GetEatTargets(def, state);
                foreach(var target in eatTargets)
                {
                    if (ActorWorld.GetPopulation(inWorld, target.TargetId) > 0)
                    {
                        if (!saveData.HasFact(target.FactId))
                            return true;
                    }
                }

                // todo: parasitism??
            }

            return false;
        }

        #endregion // Behavior Capture

        static public void EmitEmoji(BFBase inFact, ActorInstance inActor, ActorWorld inWorld)
        {
            if (!Services.Data.Profile.Bestiary.HasFact(inFact.Id()))
                return;
            
            ObservationTank tank = (ObservationTank) inWorld.Tag;
            ParticleSystem.EmitParams emit = default;
            emit.position = inActor.CachedCollider.bounds.center;
            tank.m_EatEmojis.Emit(emit, 1);
        }

        #region Sequence

        private void OnRunClick()
        {
            m_IsRunning = true;

            m_AddCrittersPanel.Hide();
            m_SelectEnvPanel.Hide();
            
            m_CameraBlinking.enabled = true;
            Routine.Start(this, StartExperiment()).TryManuallyUpdate(0);
        }

        private IEnumerator StartExperiment()
        {
            m_BottomPanelGroup.blocksRaycasts = false;
            m_InProgressGroup.blocksRaycasts = false;
            m_ParentTank.BackClickable.gameObject.SetActive(false);
            yield return Routine.Combine(
                m_BottomPanelGroup.Hide(0.1f, false),
                m_InProgressGroup.Show(0.1f, true),
                Tween.OneToZero(m_ParentTank.BackIndicators.SetAlpha, 0.1f)
            );
        }

        private void OnFinishClick()
        {
            m_IsRunning = false;

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
        }

        private void ClearStateAfterExperiment()
        {
            m_SelectEnvPanel.Hide();

            m_AddCrittersPanel.Hide();
            m_AddCrittersPanel.ClearSelection();
            m_ActorBehavior.ClearActors();
            
            m_BehaviorCircles.Reset();
            m_IsRunning = false;
            m_CameraBlinking.enabled = false;

            m_ParentTank.BackClickable.gameObject.SetActive(true);
            m_ParentTank.BackIndicators.SetAlpha(1);

            m_InProgressGroup.alpha = 0;
            m_InProgressGroup.gameObject.SetActive(false);
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
                newFact = m_SummaryPanel.FactPools.Alloc(Services.Assets.Bestiary.Fact(fact.Id), null, 0, m_SummaryPanel.FactListRoot);
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