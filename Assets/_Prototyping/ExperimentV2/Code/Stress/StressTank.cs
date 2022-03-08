using System;
using Aqua;
using Aqua.Profile;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Aqua.Scripting;
using System.Collections;

namespace ProtoAqua.ExperimentV2
{
    public class StressTank : MonoBehaviour, IBakedComponent
    {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private SelectableTank m_ParentTank = null;
        
        [Header("Setup")]
        [SerializeField, Required] private CanvasGroup m_SetupPanelGroup = null;
        [SerializeField, Required] private Button m_BackButton = null;
        [SerializeField, Required] private BestiaryAddPanel m_AddCrittersPanel = null;
        [SerializeField, Required] private CanvasGroup m_WaterPropertyGroup = null;

        [Header("Summary Popup")]
        [SerializeField] private StressSummary m_Summary = null;

        #endregion // Inspector

        [SerializeField, HideInInspector] private WaterPropertyDial[] m_Dials;

        [NonSerialized] private BestiaryDesc m_SelectedCritter;
        [NonSerialized] private ActorStateTransitionSet m_CritterTransitions;

        [NonSerialized] private ActorInstance m_SelectedCritterInstance;
        [NonSerialized] private ActorWorld m_World;

        [NonSerialized] private Routine m_TransitionAnim;

        [NonSerialized] private int m_DialsUsed = 0;
        [NonSerialized] private WaterPropertyDial.ValueChangedDelegate m_DialChangedDelegate;
        [NonSerialized] private WaterPropertyDial.ReleasedDelegate m_DialReleasedDelegate;
        [NonSerialized] private WaterPropertyDial[] m_DialMap = new WaterPropertyDial[(int) WaterPropertyId.TRACKED_COUNT];
        [NonSerialized] private bool m_DialsDirty = true;

        [NonSerialized] private WaterPropertyMask m_RevealedLeftMask;
        [NonSerialized] private WaterPropertyMask m_RevealedRightMask;
        [NonSerialized] private WaterPropertyMask m_RequiredReveals;
        [NonSerialized] private WaterPropertyMask m_VisiblePropertiesMask;
        [NonSerialized] private ExperimentResult m_ExpResult = null;

        [NonSerialized] private BestiaryDesc m_SelectedEnvironment;

        private void Awake()
        {
            m_ParentTank.ActivateMethod = Activate;
            m_ParentTank.DeactivateMethod = Deactivate;
            m_ParentTank.HasCritter = (s) => m_AddCrittersPanel.IsSelected(Assets.Bestiary(s));
            m_ParentTank.HasEnvironment = (s) => false;

            m_AddCrittersPanel.OnAdded = OnCritterAdded;
            m_AddCrittersPanel.OnRemoved = OnCritterRemoved;
            m_AddCrittersPanel.OnCleared = OnCrittersCleared;

            Services.Events.Register(GameEvents.WaterPropertiesUpdated, RebuildPropertyDials, this);

            m_BackButton.onClick.AddListener(OnBackClick);
            m_Summary.ContinueButton.onClick.AddListener(OnDoneClicked);

            m_SetupPanelGroup.Hide();
            m_WaterPropertyGroup.Hide();
            m_Summary.gameObject.SetActive(false);
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
                m_World = new ActorWorld(m_ParentTank.ActorAllocator, m_ParentTank.Bounds, null, null, 1, this);
                m_World.Tank = m_ParentTank;
            }

            m_SetupPanelGroup.Hide();
            m_WaterPropertyGroup.Hide();
            m_Summary.gameObject.SetActive(false);

            m_TransitionAnim.Replace(this, TransitionToStart()).TryManuallyUpdate();
        }

        private void Deactivate()
        {
            m_AddCrittersPanel.ClearSelection();
            m_AddCrittersPanel.Hide();

            m_RevealedLeftMask = default;
            m_RevealedRightMask = default;

            m_ParentTank.CurrentState = 0;
        }

        #endregion // Tank

        private void CheckForAllRangesFound()
        {
            if (m_RequiredReveals.Mask == 0)
                return;

            if (m_RevealedLeftMask != m_RevealedRightMask || m_RevealedLeftMask != m_RequiredReveals)
                return;

            m_TransitionAnim.Replace(this, TransitionToDone()).TryManuallyUpdate(0);
        }

        private ExperimentResult GenerateResult() {
            List<ExperimentFactResult> experimentFacts = new List<ExperimentFactResult>();

            BFState state;
            BestiaryData saveData = Save.Bestiary;
            foreach (WaterPropertyId id in m_RequiredReveals)
            {
                state = BestiaryUtils.FindStateRangeRule(m_SelectedCritter, id);
                Assert.NotNull(state, "No BFState {0} fact found for critter {1}", id, m_SelectedCritter.Id());

                experimentFacts.Add(ExperimentUtil.NewFact(state.Id));

                saveData.RegisterFact(state.Id);
            }

            ExperimentResult result = new ExperimentResult();
            result.Facts = experimentFacts.ToArray();

            return result;
        }

        private void DisplaySummaryPopup(BestiaryDesc inCritter, ExperimentResult inResult)
        {
            m_Summary.CritterNameText.SetText(inCritter.CommonName());
            m_Summary.CritterImage.sprite = inCritter.Icon();

            int factCount = 0;
            for(int i = 0; i < inResult.Facts.Length; i++) {
                ExperimentFactResult result = inResult.Facts[i];
                StateFactDisplay display = m_Summary.StateFacts[i];

                BFState fact = Assets.Fact<BFState>(result.Id);

                display.gameObject.SetActive(true);
                display.Populate(fact);

                Transform newChild = display.transform.Find("New");
                Assert.NotNull(newChild);

                newChild.gameObject.SetActive(result.Type != ExperimentFactResultType.Known);
                factCount++;
            }

            for(int i = factCount; i < m_Summary.StateFacts.Length; i++) {
                m_Summary.StateFacts[i].gameObject.SetActive(false);
            }

            m_Summary.gameObject.SetActive(true);
        }

        private void OnDoneClicked() {
            m_Summary.gameObject.SetActive(false);
            m_TransitionAnim.Replace(this, TransitionToStart()).TryManuallyUpdate(0);;
        }

        private IEnumerator TransitionToStart() {
            Services.Input.PauseAll();
            m_BackButton.gameObject.SetActive(false);
            m_WaterPropertyGroup.Hide();
            m_SetupPanelGroup.interactable = false;
            m_BackButton.gameObject.SetActive(false);
            yield return Routine.Combine(
                m_SetupPanelGroup.Show(0.2f),
                m_AddCrittersPanel.Show()
            );
            m_SetupPanelGroup.interactable = true;
            Services.Input.ResumeAll();
        }

        private void OnBackClick() {
            m_TransitionAnim.Replace(this, TransitionBack()).TryManuallyUpdate(0);
        }

        private IEnumerator TransitionBack() {
            Services.Input.PauseAll();
            m_SetupPanelGroup.interactable = false;
            m_AddCrittersPanel.ClearSelection();
            m_BackButton.gameObject.SetActive(false);
            yield return m_WaterPropertyGroup.Hide(0.2f);
            yield return m_AddCrittersPanel.Show();
            m_SetupPanelGroup.interactable = true;
            Services.Input.ResumeAll();
        }

        private IEnumerator TransitionToExperiment() {
            Services.Input.PauseAll();
            m_SetupPanelGroup.interactable = false;
            m_WaterPropertyGroup.interactable = false;
            yield return m_AddCrittersPanel.Hide();
            yield return m_WaterPropertyGroup.Show(0.2f);
            m_BackButton.gameObject.SetActive(true);
            m_SetupPanelGroup.interactable = true;
            m_WaterPropertyGroup.interactable = true;
            Services.Input.ResumeAll();
        }

        private IEnumerator TransitionToDone() {
            m_ParentTank.CurrentState &= ~TankState.Running;

            m_SetupPanelGroup.interactable = false;
            m_WaterPropertyGroup.interactable = false;
            Services.Input.PauseAll();
            Services.UI.ShowLetterbox();
            Services.Script.KillLowPriorityThreads();
            m_ExpResult = GenerateResult();

            BestiaryDesc critter = m_SelectedCritter;
            using(var fader = Services.UI.WorldFaders.AllocFader())
            {
                yield return fader.Object.Show(Color.black, 0.5f);
                m_WaterPropertyGroup.Hide();
                m_AddCrittersPanel.ClearSelection();
                m_SetupPanelGroup.Hide();
                yield return 0.5f;
                DisplaySummaryPopup(critter, m_ExpResult);
                yield return fader.Object.Hide(0.5f, false);
            }
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

        #region Critter Callbacks

        private void OnCritterAdded(BestiaryDesc inDesc)
        {
            if (Ref.Replace(ref m_SelectedCritter, inDesc))
            {
                if (m_SelectedCritterInstance != null)
                {
                    ActorWorld.Free(m_World, ref m_SelectedCritterInstance);
                }

                if (m_DialsDirty) {
                    RebuildPropertyDials();
                }
                m_CritterTransitions = m_SelectedCritter.GetActorStateTransitions();
                ResetWaterPropertiesForCritter(Save.Bestiary);

                m_SelectedCritterInstance = ActorWorld.Alloc(m_World, inDesc.Id());

                Services.Events.Dispatch(ExperimentEvents.ExperimentAddCritter, inDesc.Id());
                Services.Events.Dispatch(ExperimentEvents.ExperimentBegin, m_ParentTank.Type);

                using (var table = TempVarTable.Alloc())
                {
                    table.Set("tankType", m_ParentTank.Type.ToString());
                    table.Set("tankId", m_ParentTank.Id);
                    Services.Script.TriggerResponse(ExperimentTriggers.ExperimentStarted, table);
                }

                m_ParentTank.CurrentState |= TankState.Running;

                m_TransitionAnim.Replace(this, TransitionToExperiment()).TryManuallyUpdate(0);
            }
        }

        private void OnCritterRemoved(BestiaryDesc inDesc)
        {
            if (Ref.CompareExchange(ref m_SelectedCritter, inDesc, null))
            {
                ActorWorld.Free(m_World, ref m_SelectedCritterInstance);
                Services.Events.Dispatch(ExperimentEvents.ExperimentRemoveCritter, inDesc.Id());
            }
        }

        private void OnCrittersCleared()
        {
            m_SelectedCritter = null;
            ActorWorld.Free(m_World, ref m_SelectedCritterInstance);
            Services.Events.Dispatch(ExperimentEvents.ExperimentCrittersCleared);
        }

        #endregion // Critter Callbacks

        #region Property Callbacks

        private void OnPropertyChanged(WaterPropertyId inId, float inValue)
        {
            m_World.Water[inId] = inValue;
            ActorStateTransitionRange range = m_CritterTransitions[inId];
            WaterPropertyDesc property = Assets.Property(inId);
            
            bool bHasMin = !float.IsInfinity(range.AliveMin);
            bool bHasMax = !float.IsInfinity(range.AliveMax);
            float min = bHasMin ? range.AliveMin : property.MinValue();
            float max = bHasMax ? range.AliveMax : property.MaxValue();

            if (inValue <= min)
            {
                inValue = min;
                m_DialMap[(int) inId].SetValue(inValue);

                if (bHasMin)
                {
                    ActorInstance.SetActorState(m_SelectedCritterInstance, ActorStateId.Stressed, m_World);
                }

                if (!m_RevealedLeftMask[inId])
                {
                    m_RevealedLeftMask[inId] = true;
                    WaterPropertyDial.DisplayRanges(m_DialMap[(int) inId], true, m_RevealedRightMask[inId]);
                    Services.Audio.PostEvent("experiment.stress.reveal");
                    CheckForAllRangesFound();
                }
            }
            else if (inValue >= max)
            {
                inValue = max;
                m_DialMap[(int) inId].SetValue(inValue);

                if (bHasMax)
                {
                    ActorInstance.SetActorState(m_SelectedCritterInstance, ActorStateId.Stressed, m_World);
                }

                if (!m_RevealedRightMask[inId])
                {
                    m_RevealedRightMask[inId] = true;
                    WaterPropertyDial.DisplayRanges(m_DialMap[(int) inId], m_RevealedLeftMask[inId], true);
                    Services.Audio.PostEvent("experiment.stress.reveal");
                    CheckForAllRangesFound();
                }
            }
            else
            {
                ActorInstance.SetActorState(m_SelectedCritterInstance, ActorStateId.Alive, m_World);
            }
        }

        private void OnPropertyReleased(WaterPropertyId inId)
        {
            if (m_SelectedCritter == null)
                return;
            
            float value = m_World.Water[inId];
            ActorStateTransitionRange range = m_CritterTransitions[inId];
            if (value < range.AliveMin)
            {
                m_World.Water[inId] = range.AliveMin;
                m_DialMap[(int) inId].SetValue(range.AliveMin);
            }
            else if (value > range.AliveMax)
            {
                m_World.Water[inId] = range.AliveMax;
                m_DialMap[(int) inId].SetValue(range.AliveMax);
            }
            ActorInstance.SetActorState(m_SelectedCritterInstance, ActorStateId.Alive, m_World);
        }

        #endregion // Property Callbacks

        #region Dials

        private void RebuildPropertyDials()
        {
            if ((m_ParentTank.CurrentState & TankState.Selected) == 0)
            {
                m_DialsDirty = true;
                return;
            }

            Array.Clear(m_DialMap, 0, m_DialMap.Length);

            m_DialsUsed = 0;
            m_VisiblePropertiesMask = default;

            InventoryData invData = Save.Inventory;

            WaterPropertyDial dial;
            foreach(var property in Services.Assets.WaterProp.Sorted())
            {
                if (!property.HasFlags(WaterPropertyFlags.IsProperty))
                    continue;

                dial = m_Dials[m_DialsUsed++];
                dial.Property = property;
                dial.Icon.sprite = property.Icon();
                dial.Label.SetText(property.LabelId());
                dial.OnChanged = m_DialChangedDelegate ?? (m_DialChangedDelegate = OnPropertyChanged);
                dial.OnReleased = m_DialReleasedDelegate ?? (m_DialReleasedDelegate = OnPropertyReleased);

                var palette = property.Palette();

                dial.AliveRegion.color = palette.Background;
                dial.MinStressed.color = dial.MaxStressed.color = palette.Shadow;
                dial.MinDeath.color = dial.MaxDeath.color = ((Color) palette.Shadow * 0.8f).WithAlpha(1);
                dial.Value.Graphic.color = palette.Content;

                m_DialMap[(int) property.Index()] = dial;
                m_VisiblePropertiesMask[property.Index()] = true;

                dial.gameObject.SetActive(true);
            }

            for(int i = m_DialsUsed; i < m_Dials.Length; i++)
            {
                m_Dials[i].gameObject.SetActive(false);
            }

            m_DialsDirty = false;
        }

        private void ResetWaterPropertiesForCritter(BestiaryData inSaveData)
        {
            m_RevealedLeftMask = default;
            m_RevealedRightMask = default;
            m_RequiredReveals = default;

            m_World.Water = BestiaryUtils.FindHealthyWaterValues(m_CritterTransitions, Services.Assets.WaterProp.DefaultValues());
            WaterPropertyDial dial;
            BFState state;
            WaterPropertyId propId;
            for(int i = 0; i < m_DialsUsed; i++)
            {
                dial = m_Dials[i];
                propId = dial.Property.Index();
                dial.SetValue(m_World.Water[propId]);

                state = BestiaryUtils.FindStateRangeRule(m_SelectedCritter, propId);
                if (state != null)
                {
                    WaterPropertyDial.ConfigureStress(dial, state.Range);
                    if (inSaveData.HasFact(state.Id))
                    {
                        m_RevealedLeftMask[propId] = m_RevealedRightMask[propId] = true;
                        WaterPropertyDial.DisplayRanges(dial, true, true);
                    }
                    else
                    {
                        m_RequiredReveals[propId] = true;
                        WaterPropertyDial.DisplayRanges(dial, false, false);
                    }
                }
                else
                {
                    WaterPropertyDial.ConfigureStress(dial, ActorStateTransitionRange.Default);
                    WaterPropertyDial.DisplayRanges(dial, false, false);
                }
            }
        }
        
        #endregion // Dials

        #if UNITY_EDITOR

        void IBakedComponent.Bake()
        {
            m_Dials = GetComponentsInChildren<WaterPropertyDial>(true);
        }

        #endif // UNITY_EDITOR
    }
}