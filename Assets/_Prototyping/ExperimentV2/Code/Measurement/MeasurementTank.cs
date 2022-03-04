using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Profile;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2 {
    public class MeasurementTank : MonoBehaviour {
        static public readonly TextId FeedbackCategory_All = "experiment.measure.feedback.header.all";
        static public readonly TextId FeedbackCategory_ReproFailure = "experiment.measure.feedback.header.reproFail";
        static public readonly TextId FeedbackCategory_Repro = "experiment.measure.feedback.header.repro";
        static public readonly TextId FeedbackCategory_EatFailure = "experiment.measure.feedback.header.eatFail";
        static public readonly TextId FeedbackCategory_Eat = "experiment.measure.feedback.header.eat";
        static public readonly TextId FeedbackCategory_WaterFailure = "experiment.measure.feedback.header.waterFail";
        static public readonly TextId FeedbackCategory_Water = "experiment.measure.feedback.header.water";

        static public readonly TextId NextButton_Critters = "experiment.button.addOrganism";
        static public readonly TextId NextButton_Features = "experiment.button.features";

        [Flags]
        public enum FeatureMask {
            Stabilizer = 0x01,
            AutoFeeder = 0x02
        }

        public const FeatureMask DefaultFeatures = FeatureMask.Stabilizer;

        private enum SetupPhase : byte {
            Begin,
            Environment,
            Critters,
            Features
        }

        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private SelectableTank m_ParentTank = null;

        [Header("Setup")]
        [SerializeField, Required] private Button m_BeginButton = null;
        [SerializeField, Required] private CanvasGroup m_SetupPanelGroup = null;
        [SerializeField, Required] private BestiaryAddPanel m_SelectEnvPanel = null;
        [SerializeField, Required] private Button m_BackButton = null;
        [SerializeField, Required] private Button m_NextButton = null;
        [SerializeField, Required] private LocText m_NextButtonLabel = null;
        [SerializeField, Required] private BestiaryAddPanel m_AddCrittersPanel = null;
        [SerializeField, Required] private MeasurementFeaturePanel m_FeaturePanel = null;
        [SerializeField, Required] private ToggleableTankFeature m_StabilizerFeature = null;
        [SerializeField, Required] private ToggleableTankFeature m_FeederFeature = null;
        [SerializeField, Required] private Button m_RunButton = null;

        [Header("Summary")]
        [SerializeField] private TextId m_EatingHintId = default;
        [SerializeField] private TextId m_ReproHintId = default;
        [SerializeField] private TextId m_WaterHintId = default;

        #endregion // Inspector

        [NonSerialized] private SetupPhase m_SetupPhase;
        [NonSerialized] private BestiaryDesc m_SelectedEnvironment;
        [NonSerialized] private ActorWorld m_World;

        [NonSerialized] private Routine m_DrainRoutine;

        private void Awake() {
            m_ParentTank.ActivateMethod = Activate;
            m_ParentTank.DeactivateMethod = Deactivate;
            m_ParentTank.HasCritter = (s) => m_AddCrittersPanel.IsSelected(Assets.Bestiary(s));
            m_ParentTank.HasEnvironment = (s) => m_SelectedEnvironment?.Id() == s;

            m_AddCrittersPanel.OnAdded = OnCritterAdded;
            m_AddCrittersPanel.OnRemoved = OnCritterRemoved;
            m_AddCrittersPanel.OnCleared = OnCrittersCleared;

            m_SelectEnvPanel.OnAdded = OnEnvironmentAdded;
            m_SelectEnvPanel.OnRemoved = OnEnvironmentRemoved;
            m_SelectEnvPanel.OnCleared = OnEnvironmentCleared;

            m_FeaturePanel.OnUpdated = OnFeaturesUpdated;

            ToggleableTankFeature.Disable(m_StabilizerFeature, true);
            ToggleableTankFeature.Disable(m_FeederFeature, true);

            m_BeginButton.onClick.AddListener(OnBeginClick);
            m_NextButton.onClick.AddListener(OnNextClick);
            m_BackButton.onClick.AddListener(OnBackClick);
            m_RunButton.onClick.AddListener(OnRunClicked);
        }

        private void OnDestroy() {
            Services.Events?.DeregisterAll(this);
        }

        #region Tank

        private void Activate() {
            if (m_World == null) {
                m_World = new ActorWorld(m_ParentTank.ActorAllocator, m_ParentTank.Bounds, null, null, 16, this);
            }

            m_SetupPanelGroup.Hide();

            m_BeginButton.gameObject.SetActive(true);
            m_DrainRoutine.Stop();
            TankWaterSystem.SetWaterHeight(m_ParentTank, 0);

            ToggleableTankFeature.Disable(m_StabilizerFeature, true);
            ToggleableTankFeature.Disable(m_FeederFeature, true);

            m_SetupPhase = SetupPhase.Begin;
        }

        private void Deactivate() {
            m_AddCrittersPanel.Hide();
            m_AddCrittersPanel.ClearSelection();

            m_SelectEnvPanel.Hide();
            m_SelectEnvPanel.ClearSelection();

            m_FeaturePanel.Hide();
            m_FeaturePanel.ClearSelection();

            // if (m_SummaryPanel.gameObject.activeSelf) {
            //     m_SummaryPanel.gameObject.SetActive(false);
            //     m_SummaryPanel.FactPools.FreeAll();
            // }

            if (m_ParentTank.WaterFillProportion > 0) {
                m_DrainRoutine.Replace(this, m_ParentTank.WaterSystem.DrainWaterOverTime(m_ParentTank, 1.5f));
            }

            ToggleableTankFeature.Disable(m_StabilizerFeature, true);
            ToggleableTankFeature.Disable(m_FeederFeature, true);

            m_ParentTank.CurrentState = 0;
        }

        #endregion // Tank

        #region Critter Callbacks

        private void OnCritterAdded(BestiaryDesc inDesc) {
            ActorWorld.AllocWithDefaultCount(m_World, inDesc.Id());
            m_NextButton.interactable = true;
        }

        private void OnCritterRemoved(BestiaryDesc inDesc) {
            ActorWorld.FreeAll(m_World, inDesc.Id());
            m_NextButton.interactable = m_World.Actors.Count > 0;
        }

        private void OnCrittersCleared() {
            ActorWorld.FreeAll(m_World);
            m_NextButton.interactable = false;
        }

        #endregion // Critter Callbacks

        #region Environment Callbacks

        private void OnEnvironmentAdded(BestiaryDesc inDesc) {
            m_SelectedEnvironment = inDesc;
            ActorWorld.SetWaterState(m_World, inDesc.GetEnvironment());
            m_NextButton.interactable = true;
            m_ParentTank.WaterColor.SetColor(inDesc.WaterColor().WithAlpha(m_ParentTank.DefaultWaterColor.a));
        }

        private void OnEnvironmentRemoved(BestiaryDesc inDesc) {
            if (Ref.CompareExchange(ref m_SelectedEnvironment, inDesc, null)) {
                m_NextButton.interactable = false;
                ActorWorld.SetWaterState(m_World, null);
                m_ParentTank.WaterColor.SetColor(m_ParentTank.DefaultWaterColor);
            }
        }

        private void OnEnvironmentCleared() {
            m_SelectedEnvironment = null;
            ActorWorld.SetWaterState(m_World, null);
            m_NextButton.interactable = false;
            m_ParentTank.WaterColor.SetColor(m_ParentTank.DefaultWaterColor);
        }

        #endregion // Environment Callbacks

        #region Feature Callbacks

        private void OnFeaturesUpdated(FeatureMask inMask) {
            if (m_SetupPhase == SetupPhase.Features) {
                UpdateFeature(inMask, FeatureMask.Stabilizer, m_StabilizerFeature);
                UpdateFeature(inMask, FeatureMask.AutoFeeder, m_FeederFeature);
            } else {
                ToggleableTankFeature.Disable(m_StabilizerFeature);
                ToggleableTankFeature.Disable(m_FeederFeature);
            }
        }

        static private void UpdateFeature(FeatureMask inSet, FeatureMask inMask, ToggleableTankFeature inFeature) {
            if ((inSet & inMask) != 0) {
                ToggleableTankFeature.Enable(inFeature);
            } else {
                ToggleableTankFeature.Disable(inFeature);
            }
        }

        #endregion // Feature Callbacks

        #region Sequence

        private void OnBeginClick() {
            Services.Input.PauseAll();
            m_BeginButton.gameObject.SetActive(false);
            m_RunButton.gameObject.SetActive(false);
            m_NextButton.gameObject.SetActive(true);
            m_NextButtonLabel.SetText(NextButton_Critters);
            m_NextButton.interactable = m_SelectedEnvironment != null;
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

            ExperimentUtil.TriggerExperimentScreenViewed(m_ParentTank, "measurement.ecosystem");
        }

        private void OnNextClick() {
            Services.Input.PauseAll();
            m_SetupPanelGroup.interactable = false;
            switch(m_SetupPhase) {
                case SetupPhase.Environment: {
                    m_SetupPhase = SetupPhase.Critters;
                    Routine.Start(this, FillTankSequence());
                    break;
                }

                case SetupPhase.Critters: {
                    m_SetupPhase = SetupPhase.Features;
                    Routine.Start(this, ToFeaturesSequence());
                    break;
                }
            }
        }

        private IEnumerator FillTankSequence() {
            yield return Routine.Combine(
                m_SetupPanelGroup.Hide(0.2f),
                m_SelectEnvPanel.Hide()
            );

            yield return m_DrainRoutine;

            m_NextButtonLabel.SetText(NextButton_Features);

            yield return m_ParentTank.WaterSystem.RequestFill(m_ParentTank);
            yield return 0.2f;

            yield return Routine.Combine(
                m_SetupPanelGroup.Show(0.2f),
                m_AddCrittersPanel.Show()
            );

            m_SetupPanelGroup.interactable = true;
            Services.Input.ResumeAll();

            ExperimentUtil.TriggerExperimentScreenViewed(m_ParentTank, "measurement.organisms");
        }

        private IEnumerator ToFeaturesSequence() {
            yield return Routine.Combine(
                m_SetupPanelGroup.Hide(0.2f),
                m_AddCrittersPanel.Hide()
            );

            m_NextButton.gameObject.SetActive(false);
            m_RunButton.gameObject.SetActive(true);

            OnFeaturesUpdated(m_FeaturePanel.Selected);

            yield return Routine.Combine(
                m_SetupPanelGroup.Show(0.2f),
                m_FeaturePanel.Show()
            );

            m_SetupPanelGroup.interactable = true;
            Services.Input.ResumeAll();

            ExperimentUtil.TriggerExperimentScreenViewed(m_ParentTank, "measurement.features");
        }

        private void OnBackClick() {
            Services.Input.PauseAll();
            m_SetupPanelGroup.interactable = false;

            switch(m_SetupPhase) {
                case SetupPhase.Features: {
                    m_SetupPhase = SetupPhase.Critters;
                    Routine.Start(this, BackToCritters());
                    ExperimentUtil.TriggerExperimentScreenExited(m_ParentTank, "measurement.features");
                    break;
                }

                case SetupPhase.Critters: {
                    m_SetupPhase = SetupPhase.Environment;
                    Routine.Start(this, BackToEnvironment());
                    ExperimentUtil.TriggerExperimentScreenExited(m_ParentTank, "measurement.organisms");
                    break;
                }

                case SetupPhase.Environment: {
                    m_SetupPhase = SetupPhase.Begin;
                    Routine.Start(this, BackToBegin());
                    ExperimentUtil.TriggerExperimentScreenExited(m_ParentTank, "measurement.ecosystem");
                    break;
                }
            }
        }

        private IEnumerator BackToCritters() {
            m_FeaturePanel.ClearSelection();

            yield return Routine.Combine(
                m_SetupPanelGroup.Hide(0.2f),
                m_FeaturePanel.Hide()
            );

            OnFeaturesUpdated(0);

            m_RunButton.gameObject.SetActive(false);
            m_NextButton.gameObject.SetActive(true);
            m_NextButtonLabel.SetText(NextButton_Features);

            yield return Routine.Combine(
                m_AddCrittersPanel.Show(),
                m_SetupPanelGroup.Show(0.2f)
            );
            m_SetupPanelGroup.interactable = true;
            Services.Input.ResumeAll();

            ExperimentUtil.TriggerExperimentScreenViewed(m_ParentTank, "measurement.organisms");
        }

        private IEnumerator BackToEnvironment() {
            m_DrainRoutine.Replace(this, m_ParentTank.WaterSystem.DrainWaterOverTime(m_ParentTank, 1.5f));
            m_AddCrittersPanel.ClearSelection();
            
            yield return Routine.Combine(
                m_SetupPanelGroup.Hide(0.2f),
                m_AddCrittersPanel.Hide()
            );

            m_NextButtonLabel.SetText(NextButton_Critters);

            yield return m_DrainRoutine;

            yield return Routine.Combine(
                m_SelectEnvPanel.Show(),
                m_SetupPanelGroup.Show(0.2f)
            );
            m_SetupPanelGroup.interactable = true;
            Services.Input.ResumeAll();

            ExperimentUtil.TriggerExperimentScreenViewed(m_ParentTank, "measurement.ecosystem");
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

        #endregion // Sequence

        #region Run

        private void OnRunClicked() {
            m_ParentTank.CurrentState |= TankState.Running;

            m_AddCrittersPanel.Hide();
            m_SelectEnvPanel.Hide();
            m_FeaturePanel.Hide();

            InProgressExperimentData experimentData = new InProgressExperimentData();
            experimentData.EnvironmentId = m_SelectedEnvironment.Id();
            experimentData.TankType = InProgressExperimentData.Type.Measurement;
            experimentData.TankId = m_ParentTank.Id;
            experimentData.Settings = 0;
            if (m_StabilizerFeature.State)
                experimentData.Settings |= InProgressExperimentData.Flags.Stabilizer;
            if (m_FeederFeature.State)
                experimentData.Settings |= InProgressExperimentData.Flags.Feeder;
            experimentData.CritterIds = ArrayUtils.MapFrom<BestiaryDesc, StringHash32>(m_AddCrittersPanel.Selected, (a) => a.Id());

            using(var table = TempVarTable.Alloc()) {
                table.Set("tankType", m_ParentTank.Type.ToString());
                table.Set("tankId", m_ParentTank.Id);
                var thread = Services.Script.TriggerResponse(ExperimentTriggers.ExperimentStarted, table);
                Routine.Start(this, RunExperiment(experimentData, thread)).TryManuallyUpdate(0);
                Services.Events.Dispatch(ExperimentEvents.ExperimentBegin, m_ParentTank.Type);
            }
        }

        private IEnumerator RunExperiment(InProgressExperimentData inExperiment, ScriptThreadHandle inThread) {
            ExperimentResult result = Evaluate(inExperiment);
            Services.Input.PauseAll();
            Services.UI.ShowLetterbox();
            if (inThread.IsRunning())
                yield return inThread.Wait();

            using(var fader = Services.UI.WorldFaders.AllocFader()) {
                yield return fader.Object.Show(Color.black, 0.5f);
                yield return m_ParentTank.WaterSystem.DrainWaterOverTime(m_ParentTank, 1f);
                ClearStateAfterExperiment();
                InitializeSummaryScreen(result);
                yield return 0.5f;
                yield return fader.Object.Hide(0.5f, false);
            }

            foreach (var fact in result.Facts) {
                switch (fact.Type) {
                    case ExperimentFactResultType.NewFact:
                        Log.Msg("[MeasurementTank] Adding Fact {0}", fact.Id);
                        Save.Bestiary.RegisterFact(fact.Id);
                        break;
                    case ExperimentFactResultType.UpgradedFact:
                        Log.Msg("[MeasurementTank] Upgrading Fact {0}", fact.Id);
                        Save.Bestiary.AddDiscoveredFlags(fact.Id, fact.Flags);
                        break;
                }
            }

            foreach (var feedback in result.Feedback) {
                Log.Msg("[MeasurementTank] {0}: {1}", feedback.Category, feedback.Id);
            }

            m_ParentTank.CurrentState &= ~TankState.Running;
            yield return PopulateSummaryScreen(result);
            Services.UI.HideLetterbox();
            Services.Input.ResumeAll();

            using(var table = TempVarTable.Alloc()) {
                table.Set("tankType", m_ParentTank.Type.ToString());
                table.Set("tankId", m_ParentTank.Id);
                Services.Script.TriggerResponse(ExperimentTriggers.ExperimentFinished, table);
            }

            Services.Events.Dispatch(ExperimentEvents.ExperimentEnded, m_ParentTank.Type);
        }

        private void ClearStateAfterExperiment() {
            TankWaterSystem.SetWaterHeight(m_ParentTank, 0);

            m_AddCrittersPanel.Hide();
            m_AddCrittersPanel.ClearSelection();

            m_SelectEnvPanel.Hide();

            m_FeaturePanel.Hide();
            m_FeaturePanel.ClearSelection();

            m_SetupPanelGroup.alpha = 0;
            m_SetupPanelGroup.blocksRaycasts = false;
            m_SetupPanelGroup.gameObject.SetActive(false);

            ToggleableTankFeature.Disable(m_StabilizerFeature, true);
            ToggleableTankFeature.Disable(m_FeederFeature, true);

            // m_SummaryPanel.gameObject.SetActive(true);
        }

        private void InitializeSummaryScreen(ExperimentResult inResult) {
            // if (inResult.Facts.Length == 0)
            // {
            //     m_SummaryPanel.FactGroup.SetActive(false);
            //     m_SummaryPanel.HintGroup.SetActive(true);

            //     TempList8<TextId> labels = default;
            //     foreach(var feedback in inResult.Feedback) {
            //         if (feedback.Category == FeedbackCategory_ReproFailure) {
            //             labels.Add(m_ReproHintId);
            //         } else if (feedback.Category == FeedbackCategory_EatFailure) {
            //             labels.Add(m_EatingHintId);
            //         } else if (feedback.Category == FeedbackCategory_WaterFailure) {
            //             labels.Add(m_WaterHintId);
            //         }
            //     }

            //     m_SummaryPanel.HintText.SetText(RNG.Instance.Choose(labels));

            //     m_SummaryPanel.HeaderText.SetText("experiment.summary.header.noFacts");
            //     m_SummaryPanel.HeaderText.Graphic.color = AQColors.BrightBlue;
            //     return;
            // }

            // m_SummaryPanel.HintGroup.SetActive(false);
            // m_SummaryPanel.FactGroup.SetActive(true);

            // m_SummaryPanel.HeaderText.SetText("experiment.summary.header");
            // m_SummaryPanel.HeaderText.Graphic.color = AQColors.HighlightYellow;
        }

        private IEnumerator PopulateSummaryScreen(ExperimentResult inResult) {
            // if (inResult.Facts.Length > 0) {
            //     MonoBehaviour newFact;
            //     BFBase fact;
            //     foreach (var factResult in inResult.Facts) {
            //         fact = Assets.Fact(factResult.Id);
            //         newFact = m_SummaryPanel.FactPools.Alloc(fact, null, Save.Bestiary.GetDiscoveredFlags(fact.Id), m_SummaryPanel.FactListRoot);
            //         newFact.GetComponent<CanvasGroup>().alpha = 0;
            //         yield return null;
            //         m_SummaryPanel.FactListLayout.ForceRebuild();
            //         yield return ExperimentUtil.AnimateFeedbackItemToOn(newFact, factResult.Type == ExperimentFactResultType.Known ? 0.5f : 1);
            //         yield return 0.2f;
            //     }
            // }
            yield break;
        }

        #endregion // Run

        #region Evaluation

        static public ExperimentResult Evaluate(InProgressExperimentData inData) {
            ExperimentResult result = new ExperimentResult();
            if (ExperimentUtil.AnyDead(inData)) {
                result.Facts = new ExperimentFactResult[0];
                result.Feedback = new ExperimentFeedback[] { new ExperimentFeedback(FeedbackCategory_All, ExperimentFeedback.DeadCritters, ExperimentFeedback.FailureFlag) };
            } else {
                List<ExperimentFeedback> newFeedback = new List<ExperimentFeedback>();
                List<ExperimentFactResult> newFacts = new List<ExperimentFactResult>();
                EvaluateReproductionResult(inData, newFeedback, newFacts);
                EvaluateEatResult(inData, newFeedback, newFacts);
                EvaluateWaterChemResult(inData, newFeedback, newFacts);
                result.Facts = newFacts.ToArray();
                result.Feedback = newFeedback.ToArray();
            }
            return result;
        }

        static private void EvaluateReproductionResult(InProgressExperimentData inData, List<ExperimentFeedback> ioFeedback, List<ExperimentFactResult> ioFacts) {
            if (inData.CritterIds.Length > 1) {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_ReproFailure, ExperimentFeedback.MoreThanOneSpecies, ExperimentFeedback.FailureFlag));
            } else if ((inData.Settings & InProgressExperimentData.Flags.Feeder) == 0) {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_ReproFailure, ExperimentFeedback.AutoFeederDisabled, ExperimentFeedback.FailureFlag));
            } else if ((inData.Settings & InProgressExperimentData.Flags.Stabilizer) == 0) {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_ReproFailure, ExperimentFeedback.StabilizerDisabled, ExperimentFeedback.FailureFlag));
            } else {
                WaterPropertyBlockF32 env = Assets.Bestiary(inData.EnvironmentId).GetEnvironment();
                BestiaryDesc critter = Assets.Bestiary(inData.CritterIds[0]);

                if (critter.HasFlags(BestiaryDescFlags.IsNotLiving)) {
                    ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_ReproFailure, ExperimentFeedback.IsDeadMatter, ExperimentFeedback.FailureFlag));
                    return;
                }

                ActorStateId critterState = critter.EvaluateActorState(env, out var _);

                BFReproduce reproduce = BestiaryUtils.FindReproduceRule(critter, critterState);
                BFGrow grow = BestiaryUtils.FindGrowRule(critter, critterState);

                bool bFound = false;

                if (reproduce != null) {
                    ioFacts.Add(ExperimentUtil.NewFact(reproduce.Id));
                    bFound = true;
                }

                if (grow != null) {
                    ioFacts.Add(ExperimentUtil.NewFact(grow.Id));
                    bFound = true;
                }

                if (!bFound) {
                    ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Repro, ExperimentFeedback.DoesNotReproduce, 0));
                }
            }
        }

        static private void EvaluateWaterChemResult(InProgressExperimentData inData, List<ExperimentFeedback> ioFeedback, List<ExperimentFactResult> ioFacts) {
            if (inData.CritterIds.Length > 1) {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_WaterFailure, ExperimentFeedback.MoreThanOneSpecies, ExperimentFeedback.FailureFlag));
            } else if ((inData.Settings & InProgressExperimentData.Flags.Feeder) == 0) {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_WaterFailure, ExperimentFeedback.AutoFeederDisabled, ExperimentFeedback.FailureFlag));
            } else if ((inData.Settings & InProgressExperimentData.Flags.Stabilizer) != 0) {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_WaterFailure, ExperimentFeedback.StabilizerEnabled, ExperimentFeedback.FailureFlag));
            } else {
                WaterPropertyBlockF32 env = Assets.Bestiary(inData.EnvironmentId).GetEnvironment();
                BestiaryDesc critter = Assets.Bestiary(inData.CritterIds[0]);
                ActorStateId critterState = critter.EvaluateActorState(env, out var _);

                if (critter.HasFlags(BestiaryDescFlags.IsNotLiving)) {
                    ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_WaterFailure, ExperimentFeedback.IsDeadMatter, ExperimentFeedback.FailureFlag));
                    return;
                }

                bool bAddedFacts = false;
                bool bHadFacts = false;
                foreach (var prop in Services.Assets.WaterProp.Sorted()) {
                    BFConsume consume = BestiaryUtils.FindConsumeRule(critter, prop.Index(), critterState);
                    BFProduce produce = BestiaryUtils.FindProduceRule(critter, prop.Index(), critterState);
                    bHadFacts |= (consume || produce);
                    if (consume != null) {
                        ioFacts.Add(ExperimentUtil.NewFact(consume.Id));
                        bAddedFacts = true;
                    }
                    if (produce != null) {
                        ioFacts.Add(ExperimentUtil.NewFact(produce.Id));
                        bAddedFacts = true;
                    }
                }

                if (!bHadFacts) {
                    ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Water, ExperimentFeedback.NoWaterChemistry, 0));
                } else if (!bAddedFacts) {
                    ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_WaterFailure, ExperimentFeedback.CannotMeasureWaterChem, ExperimentFeedback.NotUnlockedFlag));
                }
            }
        }

        static private void EvaluateEatResult(InProgressExperimentData inData, List<ExperimentFeedback> ioFeedback, List<ExperimentFactResult> ioFacts) {
            if (inData.CritterIds.Length < 2) {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_EatFailure, ExperimentFeedback.LessThanTwoSpecies, ExperimentFeedback.FailureFlag));
            } else if (inData.CritterIds.Length > 2) {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_EatFailure, ExperimentFeedback.MoreThanTwoSpecies, ExperimentFeedback.FailureFlag));
            } else if ((inData.Settings & InProgressExperimentData.Flags.Feeder) != 0) {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_EatFailure, ExperimentFeedback.AutoFeederEnabled, ExperimentFeedback.FailureFlag));
            } else if ((inData.Settings & InProgressExperimentData.Flags.Stabilizer) == 0) {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_EatFailure, ExperimentFeedback.StabilizerDisabled, ExperimentFeedback.FailureFlag));
            } else {
                WaterPropertyBlockF32 env = Assets.Bestiary(inData.EnvironmentId).GetEnvironment();
                BestiaryDesc leftCritter = Assets.Bestiary(inData.CritterIds[0]);
                BestiaryDesc rightCritter = Assets.Bestiary(inData.CritterIds[1]);

                if (leftCritter.HasFlags(BestiaryDescFlags.IsNotLiving) && rightCritter.HasFlags(BestiaryDescFlags.IsNotLiving)) {
                    ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_EatFailure, ExperimentFeedback.IsDeadMatter, ExperimentFeedback.FailureFlag));
                    return;
                }

                ActorStateId leftState = leftCritter.EvaluateActorState(env, out var _);
                ActorStateId rightState = rightCritter.EvaluateActorState(env, out var _);
                BFEat leftEatsRight = BestiaryUtils.FindEatingRule(leftCritter, rightCritter, leftState);
                BFEat rightEatsLeft = BestiaryUtils.FindEatingRule(rightCritter, leftCritter, rightState);
                BestiaryData bestiaryData = Save.Bestiary;

                bool bHadFact = leftEatsRight || rightEatsLeft;
                bool bAddedFact = false;

                if (leftEatsRight != null) {
                    if (bestiaryData.HasFact(leftEatsRight.Id)) {
                        bAddedFact = true;
                        ioFacts.Add(ExperimentUtil.NewFactFlags(leftEatsRight.Id, BFDiscoveredFlags.Rate));
                    } else {
                        leftEatsRight = null;
                    }
                }
                if (rightEatsLeft != null) {
                    if (bestiaryData.HasFact(rightEatsLeft.Id)) {
                        bAddedFact = true;
                        ioFacts.Add(ExperimentUtil.NewFactFlags(rightEatsLeft.Id, BFDiscoveredFlags.Rate));
                    } else {
                        rightEatsLeft = null;
                    }
                }

                if (!bAddedFact) {
                    if (!bHadFact) {
                        ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_EatFailure, ExperimentFeedback.NoRelationship, ExperimentFeedback.NotUnlockedFlag));
                    } else {
                        ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_EatFailure, ExperimentFeedback.NoRelationship, ExperimentFeedback.FailureFlag));
                    }
                }
            }
        }
    
        #endregion // Evaluation
    }
}