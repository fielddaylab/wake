using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Profile;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using ScriptableBake;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2 {
    public class StressTank : MonoBehaviour, IBaked {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private SelectableTank m_ParentTank = null;

        [Header("Setup")]
        [SerializeField, Required] private ExperimentHeaderUI m_HeaderUI = null;
        [SerializeField, Required] private ExperimentScreen m_OrganismScreen = null;
        [SerializeField, Required] private ExperimentScreen m_PropertiesScreen = null;

        #endregion // Inspector

        [SerializeField, HideInInspector] private WaterPropertyDial[] m_Dials;

        [NonSerialized] private ActorWorld m_World;

        [NonSerialized] private BestiaryDesc m_SelectedCritter;
        [NonSerialized] private ActorStateTransitionSet m_CritterTransitions;
        [NonSerialized] private ActorInstance m_SelectedCritterInstance;

        [NonSerialized] private int m_DialsUsed = 0;
        [NonSerialized] private WaterPropertyDial.ValueChangedDelegate m_DialChangedDelegate;
        [NonSerialized] private WaterPropertyDial.ReleasedDelegate m_DialReleasedDelegate;
        [NonSerialized] private WaterPropertyDial[] m_DialMap = new WaterPropertyDial[(int)WaterPropertyId.TRACKED_COUNT];
        [NonSerialized] private bool m_DialsDirty = true;

        [NonSerialized] private WaterPropertyMask m_RevealedLeftMask;
        [NonSerialized] private WaterPropertyMask m_RevealedRightMask;
        [NonSerialized] private WaterPropertyMask m_RequiredReveals;
        [NonSerialized] private WaterPropertyMask m_VisiblePropertiesMask;

        private void Awake() {
            m_ParentTank.ActivateMethod = Activate;
            m_ParentTank.DeactivateMethod = Deactivate;
            m_ParentTank.HasCritter = (s) => m_OrganismScreen.Panel.IsSelected(Assets.Bestiary(s));
            m_ParentTank.HasEnvironment = (s) => false;
            m_ParentTank.ActorBehavior.ActionAvailable = (a) => {
                switch(a) {
                    case ActorActionId.Spawning:
                    case ActorActionId.Idle:
                    case ActorActionId.Waiting: 
                        return true;
                    default: return false;
                }
            };

            m_OrganismScreen.Panel.OnAdded = OnCritterAdded;
            m_OrganismScreen.Panel.OnCleared = OnCrittersCleared;
            m_HeaderUI.BackButton.onClick.AddListener(OnBackClick);
        }

        #region Tank

        private void Activate() {
            m_World = m_ParentTank.ActorBehavior.World;
            ExperimentScreen.Transition(m_OrganismScreen, m_World);
        }

        private void Deactivate() {
            m_RevealedLeftMask = default;
            m_RevealedRightMask = default;

            m_ParentTank.CurrentState = 0;
        }

        #endregion // Tank

        private void CheckForAllRangesFound() {
            if (m_RequiredReveals.Mask == 0)
                return;

            if ((m_RevealedLeftMask & m_RevealedRightMask & m_RequiredReveals) != m_RequiredReveals)
                return;

            Routine.Start(this, TransitionToDone(GenerateResult())).Tick();
        }

        private ExperimentResult GenerateResult() {
            List<ExperimentFactResult> experimentFacts = new List<ExperimentFactResult>();

            BFState state;
            BestiaryData saveData = Save.Bestiary;
            foreach (WaterPropertyId id in m_RequiredReveals) {
                state = BestiaryUtils.FindStateRangeRule(m_SelectedCritter, id);
                Assert.NotNull(state, "No BFState {0} fact found for critter {1}", id, m_SelectedCritter.Id());

                experimentFacts.Add(ExperimentUtil.NewFact(state.Id));

                saveData.RegisterFact(state.Id);
            }

            ExperimentResult result = new ExperimentResult();
            result.Facts = experimentFacts.ToArray();

            return result;
        }

        private void OnBackClick() {
            ExperimentScreen.Transition(m_OrganismScreen, m_World, Routine.Call(() => m_OrganismScreen.Panel.ClearSelection()));
            Services.Events.Dispatch(ExperimentEvents.ExperimentEnded, m_ParentTank.Type);
        }

        private IEnumerator TransitionToDone(ExperimentResult inResult) {
            m_ParentTank.CurrentState &= ~TankState.Running;

            using(Script.DisableInput())
            using(Script.Letterbox()) {
                Services.Script.KillLowPriorityThreads();
                using (var fader = Services.UI.WorldFaders.AllocFader()) {
                    yield return fader.Object.Show(Color.black, 0.5f);
                    SelectableTank.Reset(m_ParentTank, true);
                    yield return 0.5f;
                    yield return fader.Object.Hide(0.5f, false);
                }
            }

            using(Script.Letterbox()) {
                yield return ExperimentUtil.DisplaySummaryPopup(inResult);

                if (inResult.Facts.Length > 0) {
                    yield return Services.Script.TriggerResponse(ExperimentTriggers.NewStressResults).Wait();
                }

                using (var table = TempVarTable.Alloc()) {
                    table.Set("tankType", m_ParentTank.Type.ToString());
                    table.Set("tankId", m_ParentTank.Id);
                    Services.Script.TriggerResponse(ExperimentTriggers.ExperimentFinished, table);
                }

                Services.Events.Dispatch(ExperimentEvents.ExperimentEnded, m_ParentTank.Type);
                ExperimentScreen.Transition(m_OrganismScreen, m_World);
            }
        }

        #region Critter Callbacks

        private void OnCritterAdded(BestiaryDesc inDesc) {
            if (Ref.Replace(ref m_SelectedCritter, inDesc)) {
                if (m_SelectedCritterInstance != null) {
                    ActorWorld.Free(m_World, ref m_SelectedCritterInstance);
                }

                if (m_DialsDirty) {
                    RebuildPropertyDials();
                }
                m_CritterTransitions = m_SelectedCritter.GetActorStateTransitions();
                ResetWaterPropertiesForCritter(Save.Bestiary);

                m_SelectedCritterInstance = ActorWorld.Alloc(m_World, inDesc.Id());

                Services.Events.Dispatch(ExperimentEvents.ExperimentBegin, m_ParentTank.Type);

                using (var table = TempVarTable.Alloc()) {
                    table.Set("tankType", m_ParentTank.Type.ToString());
                    table.Set("tankId", m_ParentTank.Id);
                    Services.Script.TriggerResponse(ExperimentTriggers.ExperimentStarted, table);
                }

                m_ParentTank.CurrentState |= TankState.Running;

                ExperimentScreen.Transition(m_PropertiesScreen, m_World, WaitForCritterToLand());
            }
        }

        private IEnumerator WaitForCritterToLand() {
            while (m_SelectedCritterInstance.CurrentAction == ActorActionId.Spawning) {
                yield return null;
            }
            yield return 0.1f;
        }

        private void OnCrittersCleared() {
            m_SelectedCritter = null;
            m_SelectedCritterInstance = null;
            m_ParentTank.ActorBehavior.ClearActors();
        }

        #endregion // Critter Callbacks

        #region Property Callbacks

        private void OnPropertyChanged(WaterPropertyId inId, float inValue) {
            m_World.Water[inId] = inValue;
            ActorStateTransitionRange range = m_CritterTransitions[inId];
            WaterPropertyDesc property = Assets.Property(inId);

            bool bHasMin = !float.IsInfinity(range.AliveMin);
            bool bHasMax = !float.IsInfinity(range.AliveMax);
            float min = bHasMin ? range.AliveMin : property.MinValue();
            float max = bHasMax ? range.AliveMax : property.MaxValue();

            if (inValue <= min) {
                inValue = min;
                m_DialMap[(int)inId].SetValue(inValue);

                if (bHasMin) {
                    ActorInstance.SetActorState(m_SelectedCritterInstance, ActorStateId.Stressed, m_World);
                }

                if (!m_RevealedLeftMask[inId]) {
                    m_RevealedLeftMask[inId] = true;
                    WaterPropertyDial.DisplayRanges(m_DialMap[(int)inId], true, m_RevealedRightMask[inId]);
                    Services.Audio.PostEvent("experiment.stress.reveal");
                    CheckForAllRangesFound();
                }
            } else if (inValue >= max) {
                inValue = max;
                m_DialMap[(int)inId].SetValue(inValue);

                if (bHasMax) {
                    ActorInstance.SetActorState(m_SelectedCritterInstance, ActorStateId.Stressed, m_World);
                }

                if (!m_RevealedRightMask[inId]) {
                    m_RevealedRightMask[inId] = true;
                    WaterPropertyDial.DisplayRanges(m_DialMap[(int)inId], m_RevealedLeftMask[inId], true);
                    Services.Audio.PostEvent("experiment.stress.reveal");
                    CheckForAllRangesFound();
                }
            } else {
                ActorInstance.SetActorState(m_SelectedCritterInstance, ActorStateId.Alive, m_World);
            }
        }

        private void OnPropertyReleased(WaterPropertyId inId) {
            if (m_SelectedCritter == null)
                return;

            float value = m_World.Water[inId];
            ActorStateTransitionRange range = m_CritterTransitions[inId];
            if (value < range.AliveMin) {
                m_World.Water[inId] = range.AliveMin;
                m_DialMap[(int)inId].SetValue(range.AliveMin);
            } else if (value > range.AliveMax) {
                m_World.Water[inId] = range.AliveMax;
                m_DialMap[(int)inId].SetValue(range.AliveMax);
            }
            ActorInstance.SetActorState(m_SelectedCritterInstance, ActorStateId.Alive, m_World);
        }

        #endregion // Property Callbacks

        #region Dials

        private void RebuildPropertyDials() {
            if ((m_ParentTank.CurrentState & TankState.Selected) == 0) {
                m_DialsDirty = true;
                return;
            }

            Array.Clear(m_DialMap, 0, m_DialMap.Length);

            m_DialsUsed = 0;
            m_VisiblePropertiesMask = default;

            InventoryData invData = Save.Inventory;

            WaterPropertyDial dial;
            foreach (var property in Services.Assets.WaterProp.Sorted()) {
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
                dial.MinDeath.color = dial.MaxDeath.color = ((Color)palette.Shadow * 0.8f).WithAlpha(1);
                dial.Value.Graphic.color = palette.Content;

                m_DialMap[(int)property.Index()] = dial;
                m_VisiblePropertiesMask[property.Index()] = true;

                dial.gameObject.SetActive(true);
            }

            for (int i = m_DialsUsed; i < m_Dials.Length; i++) {
                m_Dials[i].gameObject.SetActive(false);
            }

            m_DialsDirty = false;
        }

        private void ResetWaterPropertiesForCritter(BestiaryData inSaveData) {
            m_RevealedLeftMask = default;
            m_RevealedRightMask = default;
            m_RequiredReveals = default;

            m_World.Water = BestiaryUtils.FindHealthyWaterValues(m_CritterTransitions, Services.Assets.WaterProp.DefaultValues());
            WaterPropertyDial dial;
            BFState state;
            WaterPropertyId propId;
            for (int i = 0; i < m_DialsUsed; i++) {
                dial = m_Dials[i];
                propId = dial.Property.Index();
                dial.SetValue(m_World.Water[propId]);

                state = BestiaryUtils.FindStateRangeRule(m_SelectedCritter, propId);
                if (state != null) {
                    WaterPropertyDial.ConfigureStress(dial, state.Range);
                    if (inSaveData.HasFact(state.Id)) {
                        m_RevealedLeftMask[propId] = m_RevealedRightMask[propId] = true;
                        WaterPropertyDial.DisplayRanges(dial, true, true);
                    } else {
                        m_RequiredReveals[propId] = true;
                        WaterPropertyDial.DisplayRanges(dial, false, false);
                    }
                } else {
                    WaterPropertyDial.ConfigureStress(dial, ActorStateTransitionRange.Default);
                    WaterPropertyDial.DisplayRanges(dial, false, false);
                }
            }
        }

        #endregion // Dials

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            m_Dials = GetComponentsInChildren<WaterPropertyDial>(true);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}