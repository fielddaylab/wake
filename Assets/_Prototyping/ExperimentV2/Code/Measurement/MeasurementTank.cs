using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Profile;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class MeasurementTank : MonoBehaviour, ISceneOptimizable
    {
        static public readonly TextId FeedbackCategory_All = "experiment.measure.feedback.header.all";
        static public readonly TextId FeedbackCategory_Repro = "experiment.measure.feedback.header.repro";
        static public readonly TextId FeedbackCategory_Eat = "experiment.measure.feedback.header.eat";
        static public readonly TextId FeedbackCategory_Water = "experiment.measure.feedback.header.water";

        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private SelectableTank m_ParentTank = null;

        [Header("Setup")]
        [SerializeField, Required] private CanvasGroup m_SetupPanelGroup = null;
        [SerializeField, Required] private BestiaryAddPanel m_AddCrittersPanel = null;
        [SerializeField, Required] private BestiaryAddPanel m_SelectEnvPanel = null;
        [SerializeField, Required(ComponentLookupDirection.Children)] private EnvIconDisplay m_EnvIcon = null;
        [SerializeField, Required] private ToggleableTankFeature m_StabilizerFeature = null;
        [SerializeField, Required] private ToggleableTankFeature m_FeederFeature = null;
        [SerializeField, Required] private Button m_RunButton = null;
        [SerializeField] private ActorAllocator m_Allocator = null;

        [Header("Summary")]
        [SerializeField] private MeasurementSummary m_SummaryPanel = null;

        #endregion // Inspector

        [NonSerialized] private BestiaryDesc m_SelectedEnvironment;
        [NonSerialized] private ActorWorld m_World;

        [SerializeField, HideInInspector] private WaterPropertyDial[] m_Dials;
        [NonSerialized] private int m_DialsUsed = 0;
        [NonSerialized] private bool m_DialsDirty = true;

        private void Awake()
        {
            m_ParentTank.ActivateMethod = Activate;
            m_ParentTank.DeactivateMethod = Deactivate;

            m_AddCrittersPanel.OnAdded = OnCritterAdded;
            m_AddCrittersPanel.OnRemoved = OnCritterRemoved;
            m_AddCrittersPanel.OnCleared = OnCrittersCleared;

            m_SelectEnvPanel.OnAdded = OnEnvironmentAdded;
            m_SelectEnvPanel.OnRemoved = OnEnvironmentRemoved;
            m_SelectEnvPanel.OnCleared = OnEnvironmentCleared;

            ToggleableTankFeature.Enable(m_StabilizerFeature);
            ToggleableTankFeature.Enable(m_FeederFeature);

            ToggleableTankFeature.RegisterHandlers(m_StabilizerFeature, OnStabilizerChanged);
            ToggleableTankFeature.RegisterHandlers(m_FeederFeature, OnAutoFeederChanged);

            Services.Events.Register(GameEvents.WaterPropertiesUpdated, RebuildPropertyDials);

            m_RunButton.interactable = false;
            m_RunButton.onClick.AddListener(OnRunClicked);
            m_SummaryPanel.Base.ContinueButton.onClick.AddListener(OnSummaryCloseClick);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        #region Tank

        private void Activate()
        {
            if (m_World == null)
            {
                m_World = new ActorWorld(m_Allocator, m_ParentTank.Bounds, null, null, 16, this);
            }

            if (m_DialsDirty)
            {
                RebuildPropertyDials();
            }

            m_SetupPanelGroup.alpha = 1;
            m_SetupPanelGroup.blocksRaycasts = true;
            m_SetupPanelGroup.gameObject.SetActive(true);
        }

        private void Deactivate()
        {
            m_AddCrittersPanel.Hide();
            m_AddCrittersPanel.ClearSelection();

            m_SelectEnvPanel.Hide();
            m_SelectEnvPanel.ClearSelection();

            if (m_SummaryPanel.gameObject.activeSelf)
            {
                m_SummaryPanel.gameObject.SetActive(false);
                m_SummaryPanel.Base.FactPools.FreeAll();
            }
        }

        #endregion // Tank

        #region Critter Callbacks

        private void OnCritterAdded(BestiaryDesc inDesc)
        {
            ActorWorld.AllocWithDefaultCount(m_World, inDesc.Id());
            m_RunButton.interactable = m_World.HasEnvironment;
        }

        private void OnCritterRemoved(BestiaryDesc inDesc)
        {
            ActorWorld.FreeAll(m_World, inDesc.Id());
            m_RunButton.interactable = m_World.HasEnvironment && m_World.Actors.Count > 0;
        }

        private void OnCrittersCleared()
        {
            ActorWorld.FreeAll(m_World);
            m_RunButton.interactable = false;
        }

        #endregion // Critter Callbacks

        #region Environment Callbacks

        private void OnEnvironmentAdded(BestiaryDesc inDesc)
        {
            m_SelectedEnvironment = inDesc;
            ActorWorld.SetWaterState(m_World, inDesc.GetEnvironment());
            m_RunButton.interactable = m_World.Actors.Count > 0;
            EnvIconDisplay.Populate(m_EnvIcon, inDesc);
            DisplayWaterProperties();
        }

        private void OnEnvironmentRemoved(BestiaryDesc inDesc)
        {
            if (Ref.CompareExchange(ref m_SelectedEnvironment, inDesc, null))
            {
                m_RunButton.interactable = false;
                EnvIconDisplay.Populate(m_EnvIcon, null);
                ActorWorld.SetWaterState(m_World, null);
                DisplayWaterProperties();
            }
        }

        private void OnEnvironmentCleared()
        {
            m_SelectedEnvironment = null;
            ActorWorld.SetWaterState(m_World, null);
            m_RunButton.interactable = false;
            EnvIconDisplay.Populate(m_EnvIcon, null);
            DisplayWaterProperties();
        }

        #endregion // Environment Callbacks

        #region Tank Features

        private void OnStabilizerChanged(bool inbChanged)
        {

        }

        private void OnAutoFeederChanged(bool inbChanged)
        {

        }

        #endregion // Tank Features

        #region Run

        private void OnRunClicked()
        {
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
            experimentData.Start = GTDate.Now;
            experimentData.Duration = GTTimeSpan.Zero;

            Routine.Start(this, RunExperiment(experimentData)).TryManuallyUpdate(0);
        }

        private IEnumerator RunExperiment(InProgressExperimentData inExperiment)
        {
            ExperimentResult result = Evaluate(inExperiment);
            Services.Input.PauseAll();
            Services.UI.ShowLetterbox();
            using(var fader = Services.UI.WorldFaders.AllocFader())
            {
                yield return fader.Object.Show(Color.black, 0.5f);
                yield return 0.5f;
                ClearStateAfterExperiment();
                yield return fader.Object.Hide(0.5f, false);
            }

            foreach(var fact in result.Facts)
            {
                switch(fact.Type)
                {
                    case ExperimentFactResultType.NewFact:
                        Log.Msg("[MeasurementTank] Adding Fact {0}", fact.Id);
                        Services.Data.Profile.Bestiary.RegisterFact(fact.Id);
                        break;
                    case ExperimentFactResultType.UpgradedFact:
                        Log.Msg("[MeasurementTank] Upgrading Fact {0}", fact.Id);
                        Services.Data.Profile.Bestiary.AddDiscoveredFlags(fact.Id, fact.Flags);
                        break;
                }
            }
            
            foreach(var feedback in result.Feedback)
            {
                Log.Msg("[MeasurementTank] {0}: {1}", feedback.Category, feedback.Id);
            }

            yield return PopulateSummaryScreen(result);
            Services.UI.HideLetterbox();
            Services.Input.ResumeAll();
        }

        private void ClearStateAfterExperiment()
        {
            m_AddCrittersPanel.Hide();
            m_AddCrittersPanel.ClearSelection();

            m_SelectEnvPanel.Hide();

            m_SetupPanelGroup.alpha = 0;
            m_SetupPanelGroup.blocksRaycasts = false;
            m_SetupPanelGroup.gameObject.SetActive(false);

            m_SummaryPanel.gameObject.SetActive(true);
        }

        private IEnumerator PopulateSummaryScreen(ExperimentResult inResult)
        {
            MonoBehaviour newFact;
            foreach(var fact in inResult.Facts)
            {
                newFact = m_SummaryPanel.Base.FactPools.Alloc(Services.Assets.Bestiary.Fact(fact.Id), null, 0, m_SummaryPanel.Base.FactListRoot);
                m_SummaryPanel.Base.FactListLayout.ForceRebuild();
                yield return ExperimentUtil.AnimateFeedbackItemToOn(newFact, fact.Type == ExperimentFactResultType.Known ? 0.5f : 1);
                yield return 0.1f;
            }

            LocText newFeedback;
            foreach(var feedback in inResult.Feedback)
            {
                newFeedback = m_SummaryPanel.HeaderPool.Alloc(m_SummaryPanel.Base.FactListRoot);
                m_SummaryPanel.Base.FactListLayout.ForceRebuild();
                newFeedback.SetText(feedback.Category);
                yield return ExperimentUtil.AnimateFeedbackItemToOn(newFeedback, 1);
                newFeedback = m_SummaryPanel.TextPool.Alloc(m_SummaryPanel.Base.FactListRoot);
                m_SummaryPanel.Base.FactListLayout.ForceRebuild();
                newFeedback.SetText(feedback.Id);
                yield return ExperimentUtil.AnimateFeedbackItemToOn(newFeedback, 1);
                yield return 0.1f;
            }
        }

        private void OnSummaryCloseClick()
        {
            m_SummaryPanel.gameObject.SetActive(false);
            m_SummaryPanel.Base.FactPools.FreeAll();
            m_SummaryPanel.HeaderPool.Reset();
            m_SummaryPanel.TextPool.Reset();
            Routine.Start(this, m_SetupPanelGroup.Show(0.1f, true));
        }

        #endregion // Run

        #region Property Dials

        private void RebuildPropertyDials()
        {
            if (!isActiveAndEnabled)
            {
                m_DialsDirty = true;
                return;
            }

            Assert.True(m_Dials.Length == (int) WaterPropertyId.TRACKED_COUNT, "{0} dials to handle {1} properties", m_Dials.Length, (int) WaterPropertyId.TRACKED_COUNT);
            m_DialsUsed = 0;
            
            WaterPropertyDial dial;
            foreach(var property in Services.Assets.WaterProp.Sorted())
            {
                if (!Services.Data.Profile.Inventory.IsPropertyUnlocked(property.Index()))
                    continue;

                dial = m_Dials[m_DialsUsed++];
                dial.Property = property;
                dial.Label.SetText(property.LabelId());
                dial.Needle.gameObject.SetActive(m_World.HasEnvironment);

                dial.gameObject.SetActive(true);
            }

            for(int i = m_DialsUsed; i < m_Dials.Length; i++)
            {
                m_Dials[i].gameObject.SetActive(false);
            }

            m_DialsDirty = false;
        }
        
        private void DisplayWaterProperties()
        {
            WaterPropertyDial dial;
            bool bHasEnvironment = m_World.HasEnvironment;
            for(int i = 0; i < m_DialsUsed; i++)
            {
                dial = m_Dials[i];
                if (bHasEnvironment)
                {
                    dial.Needle.gameObject.SetActive(true);
                    dial.SetValue(m_World.Water[dial.Property.Index()]);
                }
                else
                {
                    dial.Needle.gameObject.SetActive(false);
                }
            }
        }
        
        #endregion // Property Dials

        static public ExperimentResult Evaluate(InProgressExperimentData inData)
        {
            ExperimentResult result = new ExperimentResult();
            if (ExperimentUtil.AnyDead(inData))
            {
                result.Facts = new ExperimentFactResult[0];
                result.Feedback = new ExperimentFeedback[] { new ExperimentFeedback(FeedbackCategory_All, ExperimentFeedback.DeadCritters) };
            }
            else
            {
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

        static private void EvaluateReproductionResult(InProgressExperimentData inData, List<ExperimentFeedback> ioFeedback, List<ExperimentFactResult> ioFacts)
        {
            if (inData.CritterIds.Length > 1)
            {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Repro, ExperimentFeedback.MoreThanOneSpecies));
            }
            else if ((inData.Settings & InProgressExperimentData.Flags.Feeder) == 0)
            {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Repro, ExperimentFeedback.AutoFeederDisabled));
            }
            else if ((inData.Settings & InProgressExperimentData.Flags.Stabilizer) == 0)
            {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Repro, ExperimentFeedback.StabilizerDisabled));
            }
            else
            {
                WaterPropertyBlockF32 env = Services.Assets.Bestiary[inData.EnvironmentId].GetEnvironment();
                BestiaryDesc critter = Services.Assets.Bestiary[inData.CritterIds[0]];
                ActorStateId critterState = critter.EvaluateActorState(env, out var _);

                BFReproduce reproduce = BestiaryUtils.FindReproduceRule(critter, critterState);
                BFGrow grow = BestiaryUtils.FindGrowRule(critter, critterState);

                if (reproduce != null)
                {
                    ioFacts.Add(ExperimentUtil.NewFact(reproduce.Id));
                }

                if (grow != null)
                {
                    ioFacts.Add(ExperimentUtil.NewFact(grow.Id));
                }
            }
        }

        static private void EvaluateWaterChemResult(InProgressExperimentData inData, List<ExperimentFeedback> ioFeedback, List<ExperimentFactResult> ioFacts)
        {
            if (inData.CritterIds.Length > 1)
            {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Water, ExperimentFeedback.MoreThanOneSpecies));
            }
            else if ((inData.Settings & InProgressExperimentData.Flags.Feeder) == 0)
            {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Water, ExperimentFeedback.AutoFeederDisabled));
            }
            else if ((inData.Settings & InProgressExperimentData.Flags.Stabilizer) != 0)
            {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Water, ExperimentFeedback.StabilizerEnabled));
            }
            else
            {
                WaterPropertyBlockF32 env = Services.Assets.Bestiary[inData.EnvironmentId].GetEnvironment();
                BestiaryDesc critter = Services.Assets.Bestiary[inData.CritterIds[0]];
                ActorStateId critterState = critter.EvaluateActorState(env, out var _);

                foreach(var propId in Services.Data.Profile.Inventory.GetPropertyUnlockedMask())
                {
                    BFConsume consume = BestiaryUtils.FindConsumeRule(critter, propId, critterState);
                    BFProduce produce = BestiaryUtils.FindProduceRule(critter, propId, critterState);
                    if (consume != null)
                    {
                        ioFacts.Add(ExperimentUtil.NewFact(consume.Id));
                    }
                    if (produce != null)
                    {
                        ioFacts.Add(ExperimentUtil.NewFact(produce.Id));
                    }
                }
            }
        }

        static private void EvaluateEatResult(InProgressExperimentData inData, List<ExperimentFeedback> ioFeedback, List<ExperimentFactResult> ioFacts)
        {
            if (inData.CritterIds.Length < 2)
            {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Eat, ExperimentFeedback.LessThanTwoSpecies));
            }
            else if (inData.CritterIds.Length > 2)
            {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Eat, ExperimentFeedback.MoreThanTwoSpecies));
            }
            else if ((inData.Settings & InProgressExperimentData.Flags.Feeder) != 0)
            {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Eat, ExperimentFeedback.AutoFeederEnabled));
            }
            else if ((inData.Settings & InProgressExperimentData.Flags.Stabilizer) == 0)
            {
                ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Eat, ExperimentFeedback.StabilizerDisabled));
            }
            else
            {
                WaterPropertyBlockF32 env = Services.Assets.Bestiary[inData.EnvironmentId].GetEnvironment();
                ActorStateId leftState = Services.Assets.Bestiary[inData.CritterIds[0]].EvaluateActorState(env, out var _);
                ActorStateId rightState = Services.Assets.Bestiary[inData.CritterIds[1]].EvaluateActorState(env, out var _);
                BFEat leftEatsRight = BestiaryUtils.FindEatingRule(inData.CritterIds[0], inData.CritterIds[1], leftState);
                BFEat rightEatsLeft = BestiaryUtils.FindEatingRule(inData.CritterIds[1], inData.CritterIds[0], rightState);
                BestiaryData bestiaryData = Services.Data.Profile.Bestiary;

                if (leftEatsRight != null)
                {
                    if (bestiaryData.HasFact(leftEatsRight.Id))
                    {
                        ioFacts.Add(ExperimentUtil.NewFactFlags(leftEatsRight.Id, BFDiscoveredFlags.Rate));
                    }
                    else
                    {
                        leftEatsRight = null;
                    }
                }
                if (rightEatsLeft != null)
                {
                    if (bestiaryData.HasFact(rightEatsLeft.Id))
                    {
                        ioFacts.Add(ExperimentUtil.NewFactFlags(rightEatsLeft.Id, BFDiscoveredFlags.Rate));
                    }
                    else
                    {
                        rightEatsLeft = null;
                    }
                }

                if (leftEatsRight == null && rightEatsLeft == null)
                {
                    ioFeedback.Add(new ExperimentFeedback(FeedbackCategory_Eat, ExperimentFeedback.NoRelationship));
                }
            }
        }

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            m_Allocator = FindObjectOfType<ActorAllocator>();
            m_Dials = GetComponentsInChildren<WaterPropertyDial>(true);
        }

        #endif // UNITY_EDITOR
    }
}