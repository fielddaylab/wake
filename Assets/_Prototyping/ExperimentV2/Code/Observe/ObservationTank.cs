using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class ObservationTank : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private SelectableTank m_ParentTank = null;
        
        [SerializeField, Required] private CanvasGroup m_BottomPanelGroup = null;
        [SerializeField, Required] private BestiaryAddPanel m_AddCrittersPanel = null;
        [SerializeField, Required] private BestiaryAddPanel m_SelectEnvPanel = null;
        [SerializeField, Required(ComponentLookupDirection.Children)] private EnvIconDisplay m_EnvIcon = null;
        [SerializeField, Required] private Button m_RunButton = null;
        [SerializeField, Required] private ObservationBehaviorSystem m_ActorBehavior = null;

        #endregion // Inspector

        private readonly HashSet<BestiaryDesc> m_ActiveCritters = new HashSet<BestiaryDesc>();
        [NonSerialized] private BestiaryDesc m_SelectedEnvironment;
        [NonSerialized] private bool m_IsRunning;

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

            m_RunButton.onClick.AddListener(OnRunClick);
        }

        private void LateUpdate()
        {
            if (!m_IsRunning || Services.Pause.IsPaused())
                return;

            m_ActorBehavior.TickBehaviors(Time.deltaTime);
        }

        private void Activate()
        {
            EnvIconDisplay.Populate(m_EnvIcon, null);
            m_BottomPanelGroup.alpha = 1;
            m_BottomPanelGroup.blocksRaycasts = true;
            m_BottomPanelGroup.gameObject.SetActive(true);
        }

        private void Deactivate()
        {
            m_SelectEnvPanel.Hide();
            m_SelectEnvPanel.ClearSelection();
            m_AddCrittersPanel.Hide();
            m_AddCrittersPanel.ClearSelection();
            m_ActorBehavior.ClearAll();
            m_IsRunning = false;
        }

        private void OnCritterAdded(BestiaryDesc inDesc)
        {
            Assert.False(m_ActiveCritters.Contains(inDesc));
            m_ActiveCritters.Add(inDesc);
            m_RunButton.interactable = m_SelectedEnvironment != null;

            m_ActorBehavior.Alloc(inDesc.Id());
        }

        private void OnCritterRemoved(BestiaryDesc inDesc)
        {
            Assert.True(m_ActiveCritters.Contains(inDesc));
            m_ActiveCritters.Remove(inDesc);
            m_RunButton.interactable = m_ActiveCritters.Count > 0 && m_SelectedEnvironment != null;

            m_ActorBehavior.Free(inDesc.Id());
        }

        private void OnCrittersCleared()
        {
            m_ActiveCritters.Clear();
            m_RunButton.interactable = false;
            m_ActorBehavior.ClearAll();
        }

        private void OnEnvironmentAdded(BestiaryDesc inDesc)
        {
            m_SelectedEnvironment = inDesc;
            m_RunButton.interactable = m_ActiveCritters.Count > 0;
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

        private void OnRunClick()
        {
            m_IsRunning = true;

            m_AddCrittersPanel.Hide();
            m_SelectEnvPanel.Hide();
            
            m_BottomPanelGroup.blocksRaycasts = false;
            Routine.Start(this, m_BottomPanelGroup.Hide(0.1f, false));
        }
    }
}