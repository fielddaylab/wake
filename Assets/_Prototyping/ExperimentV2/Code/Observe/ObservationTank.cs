using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using Aqua.Animation;
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
        [SerializeField, Required] private AmbientRenderer m_CameraBlinking = null;

        #endregion // Inspector

        [NonSerialized] private ActorWorld m_World;
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
            m_CameraBlinking.enabled = false;

            m_RunButton.onClick.AddListener(OnRunClick);
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
            m_World = m_ActorBehavior.World();

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
            m_ActorBehavior.ClearAll();
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

        #endregion // Environment Callbacks

        private void OnRunClick()
        {
            m_IsRunning = true;

            m_AddCrittersPanel.Hide();
            m_SelectEnvPanel.Hide();
            
            m_CameraBlinking.enabled = true;
            m_BottomPanelGroup.blocksRaycasts = false;
            Routine.Start(this, m_BottomPanelGroup.Hide(0.1f, false));
        }
    }
}