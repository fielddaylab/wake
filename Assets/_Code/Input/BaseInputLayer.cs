using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using Aqua;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aqua
{
    public abstract class BaseInputLayer : MonoBehaviour, IInputLayer
    {
        #region Inspector

        [Header("Settings")]
        [SerializeField] protected int m_Priority = 0;
        [SerializeField, AutoEnum] protected InputLayerFlags m_Flags = InputLayerFlags.GameUI;
        [SerializeField] private bool m_AutoPush = false;
        
        [Header("Override")]
        [SerializeField] private bool m_Override = false;
        [SerializeField, ShowIfField("m_Override")] private bool m_OverrideState = true;

        [Header("Events")]
        [SerializeField] private UnityEvent m_OnInputEnabled = new UnityEvent();
        [SerializeField] private UnityEvent m_OnInputDisabled = new UnityEvent();

        #endregion // Inspector

        [NonSerialized] private int m_LastKnownSystemPriority = 0;
        [NonSerialized] private InputLayerFlags m_LastKnownSystemFlags = InputLayerFlags.Default;
        [NonSerialized] private bool m_LastKnownState;
        [NonSerialized] private DeviceInput m_DeviceInput;

        [NonSerialized] private bool m_PriorityPushed;

        #region Unity Events

        protected virtual void Awake()
        {
        }

        protected virtual void OnEnable()
        {
            Services.Input.RegisterInput(this);
            if (m_AutoPush)
                Services.Input.PushPriority(this);
        }

        protected virtual void OnDisable()
        {
            if (Services.Input != null)
            {
                Services.Input.DeregisterInput(this);
                if (m_AutoPush)
                    Services.Input.PopPriority(this);
            }
            UpdateEnabled(false);
        }

        #if UNITY_EDITOR

        protected virtual void Reset()
        {
        }

        protected virtual void OnValidate()
        {
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events

        #region IInputLayer

        public int Priority
        {
            get { return m_Priority; }
        }

        public InputLayerFlags Flags
        {
            get { return m_Flags; }
        }

        public bool? Override
        {
            get { return m_Override ? new bool?(m_OverrideState) : null; }
            set
            {
                m_Override = value.HasValue;
                m_OverrideState = value.GetValueOrDefault();
                UpdateEnabled(false);
            }
        }

        public bool IsInputEnabled
        {
            get { return m_LastKnownState; }
        }

        public DeviceInput Device
        {
            get { return m_DeviceInput ?? (m_DeviceInput = new DeviceInput(this)); }
        }

        public void UpdateSystemPriority(int inSystemPriority)
        {
            if (m_LastKnownSystemPriority != inSystemPriority)
            {
                m_LastKnownSystemPriority = inSystemPriority;
                UpdateEnabled(false);
            }
        }

        public void UpdateSystemFlags(InputLayerFlags inFlags)
        {
            if (m_LastKnownSystemFlags != inFlags)
            {
                m_LastKnownSystemFlags = inFlags;
                UpdateEnabled(false);
            }
        }

        public UnityEvent OnInputEnabled { get { return m_OnInputEnabled; } }
        public UnityEvent OnInputDisabled { get { return m_OnInputDisabled; } }

        #endregion // IInputLayer

        #region Pushing Priorities

        public bool PushPriority()
        {
            if (m_PriorityPushed)
                return false;

            Services.Input.PushPriority(this);
            m_PriorityPushed = true;
            return true;
        }

        public bool PopPriority()
        {
            if (!m_PriorityPushed)
                return false;

            Services.Input?.PopPriority(this);
            m_PriorityPushed = false;
            return true;
        }

        #endregion // Pushing Priorities

        protected abstract void SyncEnabled(bool inbState);

        protected void UpdateEnabled(bool inbForce)
        {
            bool bDesiredState = GetDesiredState();
            bool bChanged = m_LastKnownState != bDesiredState;

            if (!inbForce && !bChanged)
                return;

            Debug.LogFormat("[BaseInputLayer] Set Input Enabled state on '{0}' to {1}", gameObject.FullPath(true), bDesiredState);

            m_LastKnownState = bDesiredState;
            SyncEnabled(m_LastKnownState);

            if (bDesiredState)
                m_OnInputEnabled.Invoke();
            else
                m_OnInputDisabled.Invoke();
        }

        private bool GetDesiredState()
        {
            if (!isActiveAndEnabled)
                return false;
            
            if (m_Override)
                return m_OverrideState;
            else
                return m_Priority >= m_LastKnownSystemPriority && m_LastKnownSystemFlags != 0 && (m_Flags == 0 || (m_LastKnownSystemFlags & m_Flags) != 0);
        }

        static public BaseInputLayer Find(Component inComponent)
        {
            return inComponent.GetComponentInParent<BaseInputLayer>();
        }
    }
}