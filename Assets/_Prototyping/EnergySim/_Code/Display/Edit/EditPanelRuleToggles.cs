using System;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProtoCP;
using BeauPools;

namespace ProtoAqua.Energy
{
    public class EditPanelRuleToggles : MonoBehaviour, IPoolAllocHandler
    {
        [SerializeField] private CPControl m_Control = null;
        [SerializeField] private Graphic m_IsChanged = null;
        
        [Header("Visibility")]
        [SerializeField] private Toggle m_VisibleToggle = null;
        [SerializeField] private Graphic m_VisibleOff = null;
        [SerializeField] private Graphic m_VisibleOn = null;

        [Header("Locked")]
        [SerializeField] private Toggle m_LockedToggle = null;
        [SerializeField] private Graphic m_LockedOff = null;
        [SerializeField] private Graphic m_LockedOn = null;

        [NonSerialized] private ScenarioPackage m_Scenario;
        [NonSerialized] private SerializedRule m_Rule;
        [NonSerialized] private Action m_OnRuleStateChanged;
        [NonSerialized] private ActorType m_ActorType;
        [NonSerialized] private float m_BaseValue;
        [NonSerialized] private string m_ActorRuleId;

        private void Awake()
        {
            m_VisibleToggle.onValueChanged.AddListener(SetVisible);
            m_LockedToggle.onValueChanged.AddListener(SetLocked);
        }

        public void Initialize(ScenarioPackage inScenario, Action inOnRuleStateChange, ActorType inType)
        {
            m_Scenario = inScenario;
            m_Rule = inScenario.GetRule(m_Control.Id());
            m_OnRuleStateChanged = inOnRuleStateChange;
            m_ActorType = inType;
            m_ActorRuleId = m_Control.Id().Substring(5);
            m_ActorType.OriginalType().TryGetProperty(m_ActorRuleId, out m_BaseValue);
        }

        private void OnSync()
        {
            m_Rule = m_Scenario.GetRule(m_Control.Id());
            UpdateVisualState();
        }

        private void OnUpdate()
        {
            UpdateVisualState();
        }

        private void SetVisible(bool inbOn)
        {
            if (inbOn)
            {
                m_Rule.Flags &= ~SerializedRuleFlags.Hidden;
            }
            else
            {
                m_Rule.Flags |= SerializedRuleFlags.Hidden;
            }

            m_OnRuleStateChanged?.Invoke();
            UpdateVisualState();
        }

        private void SetLocked(bool inbOn)
        {
            if (inbOn)
            {
                m_Rule.Flags |= SerializedRuleFlags.Locked;
            }
            else
            {
                m_Rule.Flags &= ~SerializedRuleFlags.Locked;
            }

            m_OnRuleStateChanged?.Invoke();
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            bool bLockedOn = (m_Rule.Flags & SerializedRuleFlags.Locked) != 0;
            m_LockedOn.enabled = bLockedOn;
            m_LockedOff.enabled = !bLockedOn;
            m_LockedToggle.SetIsOnWithoutNotify(bLockedOn);

            bool bVisible = (m_Rule.Flags & SerializedRuleFlags.Hidden) == 0;
            m_VisibleOn.enabled = bVisible;
            m_VisibleOff.enabled = !bVisible;
            m_VisibleToggle.SetIsOnWithoutNotify(bVisible);

            float currentValue;
            m_ActorType.TryGetProperty(m_ActorRuleId, out currentValue);
            m_IsChanged.enabled = currentValue != m_BaseValue;
        }

        #region IPool Handlers

        void IPoolAllocHandler.OnAlloc()
        {
            m_Control.OnSync += OnSync;
            m_Control.OnUpdate += OnUpdate;
        }

        void IPoolAllocHandler.OnFree()
        {
            m_Control.OnSync -= OnSync;
            m_Control.OnUpdate -= OnUpdate;
            
            m_Scenario = null;
            m_Rule = null;
            m_OnRuleStateChanged = null;
            m_ActorType = null;
        }

        #endregion // IPool Handlers
    }
}