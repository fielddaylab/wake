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

namespace ProtoAqua.ExperimentV2
{
    public class MeasurementTank : MonoBehaviour
    {
        [Flags]
        public enum FeatureMask
        {
            Stabilizer = 0x01,
            AutoFeeder = 0x02
        }

        public const FeatureMask DefaultFeatures = FeatureMask.Stabilizer;

        private enum SetupPhase : byte
        {
            Begin,
            Environment,
            Critters,
            Features,
            Run
        }

        public enum IsolatedVariable
        {
            Unknown,
            Eating,
            Reproduction,
            Chemistry
        }

        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private SelectableTank m_ParentTank = null;

        [Header("Setup")]
        [SerializeField, Required] private ExperimentHeaderUI m_HeaderUI = null;
        [SerializeField, Required] private ExperimentScreen m_BeginScreen = null;
        [SerializeField, Required] private ExperimentScreen m_EnvironmentScreen = null;
        [SerializeField, Required] private ExperimentScreen m_OrganismScreen = null;
        [SerializeField, Required] private ExperimentScreen m_FeatureScreen = null;
        [SerializeField, Required] private MeasurementFeaturePanel m_FeaturePanel = null;
        [SerializeField, Required] private ParticleSystem m_AutoFeederParticles = null;
        // [SerializeField, Required] private ToggleableTankFeature m_StabilizerFeature = null;
        // [SerializeField, Required] private ToggleableTankFeature m_FeederFeature = null;

        [Header("In Progress")]
        [SerializeField, Required] private ExperimentScreen m_InProgressScreen = null;
        [SerializeField, Required] private LocText m_InProgressAnalysisLabel = null;
        [SerializeField, Required] private Graphic m_InProgressMeterBG = null;
        [SerializeField, Required] private Graphic m_InProgressMeter = null;
        [SerializeField, Required] private Graphic m_InProgressMeterFlash = null;
        [SerializeField] private ColorPalette2 m_InProgressAnalyzingColors = default;
        [SerializeField] private ColorPalette2 m_InProgressSuccessColors = default;
        [SerializeField] private ColorPalette2 m_InProgressFailureColors = default;

        [Header("Requirements")]
        [SerializeField] private int m_EatEventMeasureRequirement = 8;
        [SerializeField] private int m_ChemistryEventMeasureRequirement = 8;
        [SerializeField] private int m_ReproduceEventMeasureRequirement = 4;
        [SerializeField] private int m_BackgroundMeasureTimeRequirement = 30;

        #endregion // Inspector

        [NonSerialized] private ActorWorld m_World;
        [NonSerialized] private SetupPhase m_SetupPhase;
        [NonSerialized] private BestiaryDesc m_SelectedEnvironment;

        [NonSerialized] private RunningExperimentData m_ExperimentData;
        [NonSerialized] private IsolatedVariable m_IsolatedVar;
        [NonSerialized] private float m_Progress = 0;
        [NonSerialized] private bool m_CollectingMeasurements;
        private Routine m_MeterFlashAnim;

        private void Awake() {
            m_ParentTank.ActivateMethod = Activate;
            m_ParentTank.DeactivateMethod = Deactivate;
            m_ParentTank.HasCritter = (s) => m_OrganismScreen.Panel.IsSelected(Assets.Bestiary(s));
            m_ParentTank.HasEnvironment = (s) => m_SelectedEnvironment?.Id() == s;
            m_ParentTank.OnEmitEmoji = (e) => OnEmojiEmit(e);
            m_ParentTank.ActorBehavior.ActionAvailable = (e) => {
                switch (e) {
                    case ActorActionId.Hungry:
                    case ActorActionId.Parasiting: {
                            return (m_ExperimentData.Settings & RunningExperimentData.Flags.Feeder) == 0;
                        }

                    default: {
                            return true;
                        }
                }
            };
            m_ParentTank.ActorBehavior.ReproAvailable = () => {
                return m_IsolatedVar == IsolatedVariable.Reproduction || m_IsolatedVar == IsolatedVariable.Unknown;
            };

            m_EnvironmentScreen.Panel.OnAdded += OnEnvironmentAdded;
            m_EnvironmentScreen.Panel.OnRemoved += OnEnvironmentRemoved;
            m_EnvironmentScreen.Panel.OnCleared += OnEnvironmentCleared;

            m_OrganismScreen.Panel.HighlightFilter = EvaluateOrganismHighlight;

            // m_FeaturePanel.OnUpdated = OnFeaturesUpdated;
            m_FeatureScreen.OnReset += (s, w) => m_FeaturePanel.ClearSelection();

            m_BeginScreen.CustomButton.onClick.AddListener(OnNextClick);
            m_HeaderUI.NextButton.onClick.AddListener(OnNextClick);
            m_HeaderUI.BackButton.onClick.AddListener(OnBackClick);
            m_InProgressScreen.CustomButton.onClick.AddListener(OnFinishClick);

            m_InProgressScreen.OnOpen = m_InProgressScreen.OnReset = (s, w) => {
                ApplyProgressMeterStyle("experiment.measure.analyzing", m_InProgressAnalyzingColors);
                ApplyProgressMeterProgress(0);
                m_Progress = 0;
                m_InProgressMeterFlash.gameObject.SetActive(false);
                s.CustomButton.interactable = false;
                m_CollectingMeasurements = false;
            };

            SelectableTank.InitNavArrows(m_ParentTank);
        }

        #region Tank

        private void Activate() {
            m_World = m_ParentTank.ActorBehavior.World;
            TankWaterSystem.SetWaterHeight(m_ParentTank, 0);
            m_SetupPhase = SetupPhase.Begin;
            ExperimentScreen.Transition(m_BeginScreen, m_World);
            m_ExperimentData = null;
        }

        private void Deactivate() {
            if (m_ParentTank.WaterFillProportion > 0) {
                m_ParentTank.WaterSystem.DrainWaterOverTime(m_ParentTank, 1.5f);
            }

            m_AutoFeederParticles.Stop();

            m_ParentTank.CurrentState = 0;
            m_ExperimentData = null;
        }

        #endregion // Tank

        #region Environment Callbacks

        private void OnEnvironmentAdded(BestiaryDesc inDesc) {
            m_SelectedEnvironment = inDesc;
            m_ParentTank.ActorBehavior.UpdateEnvState(inDesc.GetEnvironment());
            m_ParentTank.WaterColor.SetColor(inDesc.WaterColor().WithAlpha(m_ParentTank.DefaultWaterColor.a));
            m_OrganismScreen.Panel.Refresh();
        }

        private void OnEnvironmentRemoved(BestiaryDesc inDesc) {
            if (Ref.CompareExchange(ref m_SelectedEnvironment, inDesc, null)) {
                m_ParentTank.ActorBehavior.ClearEnvState();
                m_ParentTank.WaterColor.SetColor(m_ParentTank.DefaultWaterColor);
            }
        }

        private void OnEnvironmentCleared() {
            m_SelectedEnvironment = null;
            m_ParentTank.ActorBehavior.ClearEnvState();
            m_ParentTank.WaterColor.SetColor(m_ParentTank.DefaultWaterColor);
        }

        private bool EvaluateOrganismHighlight(BestiaryDesc organism) {
            return m_SelectedEnvironment?.HasOrganism(organism.Id()) ?? false;
        }

        #endregion // Environment Callbacks

        #region Feature Callbacks

        // private void OnFeaturesUpdated(FeatureMask inMask) {
        //     if (m_SetupPhase == SetupPhase.Features) {
        //         UpdateFeature(inMask, FeatureMask.Stabilizer, m_StabilizerFeature);
        //         UpdateFeature(inMask, FeatureMask.AutoFeeder, m_FeederFeature);
        //     } else {
        //         ToggleableTankFeature.Disable(m_StabilizerFeature);
        //         ToggleableTankFeature.Disable(m_FeederFeature);
        //     }
        // }

        // static private void UpdateFeature(FeatureMask inSet, FeatureMask inMask, ToggleableTankFeature inFeature) {
        //     if ((inSet & inMask) != 0) {
        //         ToggleableTankFeature.Enable(inFeature);
        //     } else {
        //         ToggleableTankFeature.Disable(inFeature);
        //     }
        // }

        #endregion // Feature Callbacks

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
                case SetupPhase.Features: {
                        ExperimentScreen.Transition(m_FeatureScreen, m_World, SelectableTank.SpawnSequence(m_ParentTank, m_OrganismScreen.Panel));
                        break;
                    }
                case SetupPhase.Run: {
                        SelectableTank.SetNavArrowsActive(m_ParentTank, false);
                        ExperimentScreen.Transition(m_InProgressScreen, m_World, null, () => {
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
                        ExperimentScreen.Transition(m_EnvironmentScreen, m_World, SelectableTank.DrainTankSequence(m_ParentTank));
                        break;
                    }
                case SetupPhase.Critters: {
                        ExperimentScreen.Transition(m_OrganismScreen, m_World, SelectableTank.DespawnSequence(m_ParentTank));
                        break;
                    }
            }
        }

        #endregion // Sequence

        #region Run

        private IEnumerator StartExperiment() {
            m_ParentTank.CurrentState |= TankState.Running;
            m_ExperimentData = GenerateData();
            m_IsolatedVar = (IsolatedVariable)m_ExperimentData.CustomData;

            if ((m_ExperimentData.Settings & RunningExperimentData.Flags.Feeder) != 0) {
                m_AutoFeederParticles.Play();
            }
            yield return null;

            Services.Camera.MoveToPose(m_ParentTank.ZoomPose, 0.4f);

            m_ParentTank.ActorBehavior.Begin();
            yield return null;

            ScriptThreadHandle thread;
            using (var table = TempVarTable.Alloc()) {
                table.Set("tankType", m_ParentTank.Type.ToString());
                table.Set("tankId", m_ParentTank.Id);
                thread = Services.Script.TriggerResponse(ExperimentTriggers.ExperimentStarted, table);
            }

            Services.Events.Dispatch(ExperimentEvents.ExperimentBegin, m_ParentTank.Type);

            yield return thread.Wait();

            float duration = m_IsolatedVar > 0 ? 1.5f : 1;
            while (!AddProgressFirstPhase(Routine.DeltaTime / duration)) {
                yield return null;
            }

            if (m_CollectingMeasurements) {
                float mult = m_World.EnvDeaths > 0 ? 8 : 1;
                while (!AddProgressCollection(mult * Routine.DeltaTime / m_BackgroundMeasureTimeRequirement, false)) {
                    yield return null;
                }
            }
        }

        private RunningExperimentData GenerateData() {
            RunningExperimentData experimentData = new RunningExperimentData();
            experimentData.EnvironmentId = m_SelectedEnvironment.Id();
            experimentData.TankType = RunningExperimentData.Type.Measurement;
            experimentData.TankId = m_ParentTank.Id;
            experimentData.Settings = 0;
            if (m_FeaturePanel.IsSelected(FeatureMask.Stabilizer))
                experimentData.Settings |= RunningExperimentData.Flags.Stabilizer;
            if (m_FeaturePanel.IsSelected(FeatureMask.AutoFeeder))
                experimentData.Settings |= RunningExperimentData.Flags.Feeder;
            experimentData.CritterIds = ArrayUtils.MapFrom<BestiaryDesc, StringHash32>(m_OrganismScreen.Panel.Selected, (a) => a.Id());
            experimentData.CustomData = (int)IsolateVariable(experimentData);
            return experimentData;
        }

        private void ApplyProgressMeterStyle(TextId label, ColorPalette2 colors) {
            m_InProgressAnalysisLabel.SetText(label);
            m_InProgressAnalysisLabel.Graphic.color = colors.Content;
            m_InProgressMeter.color = colors.Background;
            m_InProgressMeterBG.SetColor(colors.Background, ColorUpdate.PreserveAlpha);
        }

        private void ApplyProgressMeterProgress(float progress) {
            m_InProgressMeter.rectTransform.anchorMax = new Vector2(progress, 1);
        }

        private bool AddProgressFirstPhase(float percentage) {
            m_Progress = Math.Min(m_Progress + percentage, 1);
            ApplyProgressMeterProgress(m_Progress);
            if (m_Progress >= 1) {
                IsolatedVariable iso = m_IsolatedVar;
                ApplyProgressMeterStyle(IsolatedVariableLabels[(int)iso], iso == 0 ? m_InProgressFailureColors : m_InProgressSuccessColors);
                if (iso == 0) {
                    FlashMeter(ColorBank.Black);
                    m_InProgressScreen.CustomButton.interactable = true;
                    m_CollectingMeasurements = false;
                    Services.Audio.PostEvent("Experiment.FinishPrompt");
                }
                else {
                    FlashMeter(ColorBank.White);
                    m_Progress = 0;
                    m_CollectingMeasurements = true;
                    ApplyProgressMeterProgress(m_Progress);
                    m_InProgressScreen.CustomButton.interactable = false;
                    Services.Audio.PostEvent("Experiment.HasNewBehaviors");
                }
                return true;
            }

            return false;
        }

        private bool AddProgressCollection(float percentage, bool playFeedback) {
            m_Progress = Math.Min(m_Progress + percentage, 1);
            ApplyProgressMeterProgress(m_Progress);
            if (m_Progress >= 1) {
                FlashMeter(ColorBank.White);
                m_CollectingMeasurements = false;
                m_InProgressScreen.CustomButton.interactable = true;
                Services.Audio.PostEvent("Experiment.FinishPrompt");
                return true;
            }

            if (playFeedback) {
                Services.Audio.PostEvent("Experiment.Accumulate");
                FlashMeter(ColorBank.White.WithAlpha(0.5f));
            }

            return false;
        }

        private void OnEmojiEmit(StringHash32 id) {
            if (!m_CollectingMeasurements) {
                return;
            }

            switch (m_IsolatedVar) {
                case IsolatedVariable.Eating: {
                        if (id == SelectableTank.Emoji_Eat || id == SelectableTank.Emoji_Parasite) {
                            AddProgressCollection(0.01f + 1f / m_EatEventMeasureRequirement, true);
                        }
                        break;
                    }

                case IsolatedVariable.Chemistry: {
                        if (id == SelectableTank.Emoji_Breath) {
                            AddProgressCollection(0.01f + 1f / m_ChemistryEventMeasureRequirement, true);
                        }
                        break;
                    }

                case IsolatedVariable.Reproduction: {
                        if (id == SelectableTank.Emoji_Reproduce) {
                            AddProgressCollection(0.01f + 1f / m_ReproduceEventMeasureRequirement, true);
                        }
                        break;
                    }
            }
        }

        private void FlashMeter(Color color) {
            m_MeterFlashAnim.Replace(this, FlashMeterAnim(m_InProgressMeterFlash, color)).Tick();
        }

        static private IEnumerator FlashMeterAnim(Graphic effect, Color color) {
            effect.gameObject.SetActive(true);
            effect.color = color;
            yield return null;
            yield return effect.FadeTo(0, 0.2f).Ease(Curve.CubeIn);
            effect.gameObject.SetActive(false);
        }

        static private readonly TextId[] IsolatedVariableLabels = new TextId[] {
            "experiment.measure.unknownVar", "experiment.measure.eatVar", "experiment.measure.reproVar", "experiment.measure.chemVar"
        };

        private void OnFinishClick() {
            m_ParentTank.CurrentState &= ~TankState.Running;
            m_ParentTank.ActorBehavior.End();

            ExperimentResult result = Evaluate(m_ExperimentData, m_World);
            if (result.Facts != null) {
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
            }
            if (result.Feedback != 0) {
                Log.Msg("[MeasurementTank] Feedback: {0}", result.Feedback);
            }

            Routine.Start(this, FinishExperiment(result)).Tick();
        }

        private IEnumerator FinishExperiment(ExperimentResult inResult) {
            using (Script.DisableInput())
            using (Script.Letterbox()) {
                Services.Script.KillLowPriorityThreads();
                using (var fader = Services.UI.WorldFaders.AllocFader()) {
                    yield return fader.Object.Show(Color.black, 1);
                    yield return m_ParentTank.WaterSystem.DrainWaterOverTime(m_ParentTank, 1f);
                    ClearStateAfterExperiment();
                    yield return 0.5f;
                    yield return fader.Object.Hide(0.5f, false);
                }
            }

            using (Script.Letterbox()) {
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
            TankWaterSystem.SetWaterHeight(m_ParentTank, 0);

            SelectableTank.Reset(m_ParentTank, true);
            Services.Camera.SnapToPose(m_ParentTank.CameraPose);

            m_AutoFeederParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            m_ParentTank.CurrentState &= ~TankState.Running;
            m_SetupPhase = 0;
            m_ExperimentData = null;
            SelectableTank.SetNavArrowsActive(m_ParentTank, true);
        }

        #endregion // Run

        #region Evaluation

        static public IsolatedVariable IsolateVariable(RunningExperimentData inData) {
            if (inData.CritterIds.Length == 1 && (inData.Settings & RunningExperimentData.Flags.ALL) == RunningExperimentData.Flags.ALL) {
                return IsolatedVariable.Reproduction;
            }

            if (inData.CritterIds.Length == 2 && (inData.Settings & RunningExperimentData.Flags.Stabilizer) != 0 && (inData.Settings & RunningExperimentData.Flags.Feeder) == 0) {
                return IsolatedVariable.Eating;
            }

            if (inData.CritterIds.Length == 1 && (inData.Settings & RunningExperimentData.Flags.Stabilizer) == 0 && (inData.Settings & RunningExperimentData.Flags.Feeder) != 0) {
                return IsolatedVariable.Chemistry;
            }

            return IsolatedVariable.Unknown;
        }

        static public ExperimentResult Evaluate(RunningExperimentData inData, ActorWorld inWorld) {
            ExperimentResult result = new ExperimentResult();
            if (inWorld.EnvDeaths > 0) {
                result.Feedback |= ExperimentFeedbackFlags.DeadOrganisms;
                result.Facts = Array.Empty<ExperimentFactResult>();
            }
            else {
                IsolatedVariable iso = (IsolatedVariable)inData.CustomData;
                if (iso != IsolatedVariable.Reproduction) {
                    result.Feedback |= ExperimentFeedbackFlags.ReproduceCategory;
                }
                if (iso != IsolatedVariable.Eating) {
                    result.Feedback |= ExperimentFeedbackFlags.EatCategory;
                }
                if (iso != IsolatedVariable.Reproduction) {
                    result.Feedback |= ExperimentFeedbackFlags.ReproduceCategory;
                }

                List<ExperimentFactResult> newFacts = new List<ExperimentFactResult>();

                switch (iso) {
                    case IsolatedVariable.Unknown: {
                            break;
                        }
                    case IsolatedVariable.Eating: {
                            EvaluateEatParasiteResult(inData, ref result.Feedback, newFacts);
                            break;
                        }
                    case IsolatedVariable.Reproduction: {
                            EvaluateReproductionResult(inData, ref result.Feedback, newFacts);
                            break;
                        }
                    case IsolatedVariable.Chemistry: {
                            EvaluateWaterChemResult(inData, ref result.Feedback, newFacts);
                            break;
                        }
                }

                result.Facts = newFacts.ToArray();
            }
            return result;
        }

        static private void EvaluateReproductionResult(RunningExperimentData inData, ref ExperimentFeedbackFlags ioFeedback, List<ExperimentFactResult> ioFacts) {
            WaterPropertyBlockF32 env = Assets.Bestiary(inData.EnvironmentId).GetEnvironment();
            BestiaryDesc critter = Assets.Bestiary(inData.CritterIds[0]);

            if (critter.HasFlags(BestiaryDescFlags.IsNotLiving)) {
                ioFeedback |= ExperimentFeedbackFlags.DeadMatter;
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
                ioFeedback |= ExperimentFeedbackFlags.NoInteraction;
            }
        }

        static private void EvaluateWaterChemResult(RunningExperimentData inData, ref ExperimentFeedbackFlags ioFeedback, List<ExperimentFactResult> ioFacts) {
            WaterPropertyBlockF32 env = Assets.Bestiary(inData.EnvironmentId).GetEnvironment();
            BestiaryDesc critter = Assets.Bestiary(inData.CritterIds[0]);
            ActorStateId critterState = critter.EvaluateActorState(env, out var _);

            if (critter.HasFlags(BestiaryDescFlags.IsNotLiving)) {
                ioFeedback |= ExperimentFeedbackFlags.DeadMatter;
                return;
            }

            bool bHadFacts = false;
            foreach (var prop in Services.Assets.WaterProp.Sorted()) {
                BFConsume consume = BestiaryUtils.FindConsumeRule(critter, prop.Index(), critterState);
                BFProduce produce = BestiaryUtils.FindProduceRule(critter, prop.Index(), critterState);
                bHadFacts |= (consume || produce);
                if (consume != null) {
                    ioFacts.Add(ExperimentUtil.NewFact(consume.Id));
                }
                if (produce != null) {
                    ioFacts.Add(ExperimentUtil.NewFact(produce.Id));
                }
            }

            if (!bHadFacts) {
                ioFeedback |= ExperimentFeedbackFlags.NoInteraction;
            }
        }

        static private void EvaluateEatParasiteResult(RunningExperimentData inData, ref ExperimentFeedbackFlags ioFeedback, List<ExperimentFactResult> ioFacts) {
            WaterPropertyBlockF32 env = Assets.Bestiary(inData.EnvironmentId).GetEnvironment();
            BestiaryDesc leftCritter = Assets.Bestiary(inData.CritterIds[0]);
            BestiaryDesc rightCritter = Assets.Bestiary(inData.CritterIds[1]);

            if (leftCritter.HasFlags(BestiaryDescFlags.IsNotLiving) && rightCritter.HasFlags(BestiaryDescFlags.IsNotLiving)) {
                ioFeedback |= ExperimentFeedbackFlags.DeadMatterEatPair;
                return;
            }

            ActorStateId leftState = leftCritter.EvaluateActorState(env, out var _);
            ActorStateId rightState = rightCritter.EvaluateActorState(env, out var _);

            BFEat leftEatsRight = BestiaryUtils.FindEatingRule(leftCritter, rightCritter, leftState);
            BFEat rightEatsLeft = BestiaryUtils.FindEatingRule(rightCritter, leftCritter, rightState);

            BFParasite leftParasitesRight = BestiaryUtils.FindParasiteRule(leftCritter, rightCritter);
            BFParasite rightParasitesLeft = BestiaryUtils.FindParasiteRule(rightCritter, leftCritter);

            BestiaryData bestiaryData = Save.Bestiary;

            bool bHadEat = leftEatsRight || rightEatsLeft;
            bool bHadParasite = leftParasitesRight || rightParasitesLeft;
            bool bAddedFact = false;

            if (leftEatsRight != null) {
                if (bestiaryData.HasFact(leftEatsRight.Id)) {
                    bAddedFact = true;
                    ioFacts.Add(ExperimentUtil.NewFactFlags(leftEatsRight.Id, BFDiscoveredFlags.Rate));
                }
                else {
                    leftEatsRight = null;
                }
            }
            if (rightEatsLeft != null) {
                if (bestiaryData.HasFact(rightEatsLeft.Id)) {
                    bAddedFact = true;
                    ioFacts.Add(ExperimentUtil.NewFactFlags(rightEatsLeft.Id, BFDiscoveredFlags.Rate));
                }
                else {
                    rightEatsLeft = null;
                }
            }
            if (leftParasitesRight) {
                if (bestiaryData.HasFact(leftParasitesRight.Id)) {
                    bAddedFact = true;
                    ioFacts.Add(ExperimentUtil.NewFactFlags(leftParasitesRight.Id, BFDiscoveredFlags.Rate));
                }
                else {
                    leftParasitesRight = null;
                }
            }
            if (rightParasitesLeft) {
                if (bestiaryData.HasFact(rightParasitesLeft.Id)) {
                    bAddedFact = true;
                    ioFacts.Add(ExperimentUtil.NewFactFlags(rightParasitesLeft.Id, BFDiscoveredFlags.Rate));
                }
                else {
                    rightParasitesLeft = null;
                }
            }

            if (!bAddedFact) {
                if (!bHadEat && !bHadParasite) {
                    ioFeedback |= ExperimentFeedbackFlags.NoInteraction;
                }
                else {
                    if (bHadEat) {
                        ioFeedback |= ExperimentFeedbackFlags.EatNeedsObserve;
                    }
                    if (bHadParasite) {
                        ioFeedback |= ExperimentFeedbackFlags.ParasiteNeedsObserve;
                    }
                }
            }
        }

        #endregion // Evaluation
    }
}