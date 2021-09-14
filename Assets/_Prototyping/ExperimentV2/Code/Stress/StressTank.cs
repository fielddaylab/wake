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

namespace ProtoAqua.ExperimentV2
{
    public class StressTank : MonoBehaviour, ISceneOptimizable
    {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private SelectableTank m_ParentTank = null;
        
        [SerializeField, Required] private BestiaryAddPanel m_AddCrittersPanel = null;
        [SerializeField, Required] private CanvasGroup m_WaterPropertyGroup = null;

        [SerializeField] private ActorAllocator m_Allocator = null;


        [Header("Summary Popup")]
        [SerializeField] private GameObject m_SummaryParent = null;
        [SerializeField] private LocText m_CritterNameText = null;
        [SerializeField] private Image m_CritterImage = null;
        [SerializeField] private Transform m_StateFactParent = null;
        [SerializeField] private CanvasGroup m_BottomBarCanvasGroup = null;
        [NonSerialized] private StateFactDisplay[] m_StateFactArray = null;
        [NonSerialized] private WaterPropertyId[] m_StateFactWaterProperties = null;
        [SerializeField] private GameObject[] m_EnvIndicatorArray = null; 
        [NonSerialized] private ExperimentResult m_ExpResult = null;
        [SerializeField, Required] private BestiaryAddPanel m_SelectEnvPanel = null;
        [SerializeField, Required(ComponentLookupDirection.Children)] private EnvIconDisplay m_EnvIcon = null;

        #endregion // Inspector

        [SerializeField, HideInInspector] private WaterPropertyDial[] m_Dials;

        [NonSerialized] private BestiaryDesc m_SelectedCritter;
        [NonSerialized] private ActorStateTransitionSet m_CritterTransitions;

        [NonSerialized] private ActorInstance m_SelectedCritterInstance;
        [NonSerialized] private ActorWorld m_World;

        [NonSerialized] private Routine m_DialFadeAnim;

        [NonSerialized] private int m_DialsUsed = 0;
        [NonSerialized] private WaterPropertyDial.ValueChangedDelegate m_DialChangedDelegate;
        [NonSerialized] private WaterPropertyDial.ReleasedDelegate m_DialReleasedDelegate;
        [NonSerialized] private WaterPropertyDial[] m_DialMap = new WaterPropertyDial[(int) WaterPropertyId.TRACKED_COUNT];
        [NonSerialized] private bool m_DialsDirty = true;

        [NonSerialized] private WaterPropertyMask m_RevealedLeftMask;
        [NonSerialized] private WaterPropertyMask m_RevealedRightMask;
        [NonSerialized] private WaterPropertyMask m_RequiredReveals;
        [NonSerialized] private WaterPropertyMask m_VisiblePropertiesMask;

        [NonSerialized] private BestiaryDesc m_SelectedEnvironment;

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

            Services.Events.Register(GameEvents.WaterPropertiesUpdated, RebuildPropertyDials, this);

            //Populate StateFact Array
            m_StateFactArray = new StateFactDisplay[m_StateFactParent.childCount];
            for(int i = 0; i< m_StateFactParent.childCount; i++)
            {
                m_StateFactArray[i] = m_StateFactParent.GetChild(i).GetComponent<StateFactDisplay>();
            }

            m_StateFactWaterProperties = new WaterPropertyId[m_StateFactParent.childCount];

            m_SummaryParent.SetActive(false);
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
                m_World = new ActorWorld(m_Allocator, m_ParentTank.Bounds, null, null, 1, this);
            }

            m_WaterPropertyGroup.alpha = 0;
            m_WaterPropertyGroup.gameObject.SetActive(false);

            if (m_DialsDirty)
            {
                RebuildPropertyDials();
            }
        }

        private void Deactivate()
        {
            HideSummaryPopup();

            m_AddCrittersPanel.Hide();

            m_RevealedLeftMask = default;
            m_RevealedRightMask = default;
        }

        #endregion // Tank

        #region Environment Callbacks

        private void OnEnvironmentAdded(BestiaryDesc inDesc)
        {
            m_SelectedEnvironment = inDesc;
            EnvIconDisplay.Populate(m_EnvIcon, inDesc);
            SetEnvironmentMarkers();
        }

        private void OnEnvironmentRemoved(BestiaryDesc inDesc)
        {
            ResetEnvironmentMarkers();

            if (Ref.CompareExchange(ref m_SelectedEnvironment, inDesc, null))
            {
                EnvIconDisplay.Populate(m_EnvIcon, null);
            }
        }

        private void OnEnvironmentCleared()
        {
            ResetEnvironmentMarkers();

            m_SelectedEnvironment = null;
            EnvIconDisplay.Populate(m_EnvIcon, null);
        }

        #endregion // Environment Callbacks

        private void SetEnvironmentMarkers()
        {
            for (int i = 0; i < m_ExpResult.Facts.Length; i++)
            {
                WaterPropertyId currFactType = m_StateFactWaterProperties[i];
                GameObject currEnvIndicator = m_EnvIndicatorArray[i];

                float value = m_SelectedEnvironment.GetEnvironment()[currFactType];

                float ratio = Assets.Property(currFactType).RemapValue(value);
                Debug.Log("Value, Ratio:" + value + ", " + ratio);

                currEnvIndicator.GetComponent<RectTransform>().anchoredPosition = new Vector2(ratio,0);
            }
        }

        private void ResetEnvironmentMarkers()
        {
            foreach (GameObject envMarker in m_EnvIndicatorArray)
                envMarker.GetComponent<RectTransform>().anchoredPosition = new Vector2(-5, 0);
        }

        private void CheckForAllRangesFound()
        {
            if (m_RequiredReveals.Mask == 0)
                return;

            if (m_RevealedLeftMask != m_RevealedRightMask || m_RevealedLeftMask != m_RequiredReveals)
                return;

            BFState state;
            BestiaryData saveData = Services.Data.Profile.Bestiary;

            List<ExperimentFactResult> experimentFacts = new List<ExperimentFactResult>();

            foreach (WaterPropertyId id in m_RequiredReveals)
            {
                state = BestiaryUtils.FindStateRangeRule(m_SelectedCritter, id);
                Assert.NotNull(state, "No BFState {0} fact found for critter {1}", id, m_SelectedCritter.Id());

                experimentFacts.Add(ExperimentUtil.NewFact(state.Id));

                saveData.RegisterFact(state.Id);
            }

            m_ExpResult = new ExperimentResult();
            m_ExpResult.Facts = experimentFacts.ToArray();

            DisplaySummaryPopup();

            Services.Events.Dispatch(ExperimentEvents.ExperimentEnded, m_ParentTank.Type);
            using (var table = TempVarTable.Alloc())
            {
                table.Set("tankType", m_ParentTank.Type.ToString());
                table.Set("tankId", m_ParentTank.Id);
                Services.Script.TriggerResponse(ExperimentTriggers.ExperimentFinished, table);
            }

            m_AddCrittersPanel.ClearSelection();
        }

        private void DisplaySummaryPopup()
        {
            foreach (StateFactDisplay stateFact in m_StateFactArray)
                stateFact.gameObject.SetActive(false);

            Routine.Start(this, m_BottomBarCanvasGroup.Hide(0.1f));
            m_SummaryParent.SetActive(true);

            m_CritterNameText.SetText(m_SelectedCritter.CommonName());
            Canvas.ForceUpdateCanvases();
            m_CritterNameText.transform.parent.GetComponent<HorizontalLayoutGroup>().enabled = false;
            m_CritterNameText.transform.parent.GetComponent<HorizontalLayoutGroup>().enabled = true;

            m_CritterImage.sprite = m_SelectedCritter.Icon();

            for (int i = 0; i < m_ExpResult.Facts.Length; i++)
            {
                StateFactDisplay currStateFact = m_StateFactArray[i];
                ExperimentFactResult currExpResult = m_ExpResult.Facts[i];

                currStateFact.gameObject.SetActive(true);

                m_StateFactWaterProperties[i] = Assets.Fact<BFState>(currExpResult.Id).Property;

                bool isNewFact = false;
                if (currExpResult.Type == ExperimentFactResultType.NewFact)
                    isNewFact = true;

                //Set new fact icon to active/not active
                currStateFact.transform.GetChild(4).gameObject.SetActive(isNewFact);

                currStateFact.Populate(Assets.Fact<BFState>(currExpResult.Id));
            }
        }

        public void HideSummaryPopup()
        {
            m_SummaryParent.SetActive(false);
            Routine.Start(this, m_BottomBarCanvasGroup.Show(0.1f));
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

                m_DialFadeAnim.Replace(this, m_WaterPropertyGroup.Show(0.1f, true));
                m_CritterTransitions = m_SelectedCritter.GetActorStateTransitions();
                ResetWaterPropertiesForCritter(Services.Data.Profile.Bestiary);

                m_SelectedCritterInstance = ActorWorld.Alloc(m_World, inDesc.Id());

                Services.Events.Dispatch(ExperimentEvents.ExperimentBegin, m_ParentTank.Type);

                using (var table = TempVarTable.Alloc())
                {
                    table.Set("tankType", m_ParentTank.Type.ToString());
                    table.Set("tankId", m_ParentTank.Id);
                    Services.Script.TriggerResponse(ExperimentTriggers.ExperimentStarted, table);
                }

                m_AddCrittersPanel.Hide();
            }
        }

        private void OnCritterRemoved(BestiaryDesc inDesc)
        {
            if (Ref.CompareExchange(ref m_SelectedCritter, inDesc, null))
            {
                ActorWorld.Free(m_World, ref m_SelectedCritterInstance);
                m_DialFadeAnim.Replace(this, m_WaterPropertyGroup.Hide(0.1f, true));
            }
        }

        private void OnCrittersCleared()
        {
            m_SelectedCritter = null;
            ActorWorld.Free(m_World, ref m_SelectedCritterInstance);
            m_DialFadeAnim.Replace(this, m_WaterPropertyGroup.Hide(0.1f, true));
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
            if (!isActiveAndEnabled)
            {
                m_DialsDirty = true;
                return;
            }

            Assert.True(m_Dials.Length == (int) WaterPropertyId.TRACKED_COUNT, "{0} dials to handle {1} properties", m_Dials.Length, (int) WaterPropertyId.TRACKED_COUNT);
            Array.Clear(m_DialMap, 0, m_DialMap.Length);

            m_DialsUsed = 0;
            m_VisiblePropertiesMask = default;

            WaterPropertyDial dial;
            foreach(var property in Services.Assets.WaterProp.Sorted())
            {
                if (!Services.Data.Profile.Inventory.IsPropertyUnlocked(property.Index()))
                    continue;

                dial = m_Dials[m_DialsUsed++];
                dial.Property = property;
                dial.Label.SetText(property.LabelId());
                dial.OnChanged = m_DialChangedDelegate ?? (m_DialChangedDelegate = OnPropertyChanged);
                dial.OnReleased = m_DialReleasedDelegate ?? (m_DialReleasedDelegate = OnPropertyReleased);

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

        void ISceneOptimizable.Optimize()
        {
            m_Allocator = FindObjectOfType<ActorAllocator>();
            m_Dials = GetComponentsInChildren<WaterPropertyDial>(true);
        }

        #endif // UNITY_EDITOR
    }
}