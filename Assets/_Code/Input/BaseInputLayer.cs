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
using Aqua.Debugging;

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

        [NonSerialized] private int m_LastKnownSystemPriority = InputService.DefaultPriority;
        [NonSerialized] private InputLayerFlags m_LastKnownSystemFlags = InputLayerFlags.Default;
        [NonSerialized] private bool m_LastKnownState;
        [NonSerialized] private DeviceInput m_DeviceInput;

        [NonSerialized] private bool m_PriorityPushed;
        [NonSerialized] private int? m_PriorityOverride;

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
            get { return m_PriorityOverride.HasValue ? m_PriorityOverride.Value : m_Priority; }
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

        public int? PriorityOVerride
        {
            get { return m_PriorityOverride; }
            set
            {
                m_PriorityOverride = value;
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

        public void UpdateSystem(int inSystemPriority, InputLayerFlags inFlags, bool inbForceUpdate)
        {
            if (inbForceUpdate || m_LastKnownSystemPriority != inSystemPriority || m_LastKnownSystemFlags != inFlags)
            {
                m_LastKnownSystemPriority = inSystemPriority;
                m_LastKnownSystemFlags = inFlags;
                UpdateEnabled(inbForceUpdate);
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

            if (DebugService.IsLogging(LogMask.Input))
            {
                DebugService.Log(LogMask.Input, "[BaseInputLayer] Set Input Enabled state on '{0}' to {1}", gameObject.FullPath(true), bDesiredState);
            }

            m_LastKnownState = bDesiredState;
            SyncEnabled(m_LastKnownState);

            if (!Services.Valid)
                return;

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
                return Priority >= m_LastKnownSystemPriority && m_LastKnownSystemFlags != 0 && (m_Flags == 0 || (m_LastKnownSystemFlags & m_Flags) != 0);
        }

        [ContextMenu("Reset Priority")]
        protected virtual void ResetPriority()
        {
            Canvas c = GetComponentInParent<Canvas>();
            if (c != null)
                m_Priority = CalculateDefaultPriority(c);
        }

        static public BaseInputLayer Find(Component inComponent)
        {
            return inComponent.GetComponentInParent<BaseInputLayer>();
        }

        /// <summary>
        /// Calculates the priority of a given canvas.
        /// </summary>
        static public int CalculateDefaultPriority(Canvas inCanvas)
        {
            int offset = CalculatePriorityOffset(inCanvas.sortingLayerID, (int) inCanvas.planeDistance, inCanvas.sortingOrder);
            return 100000 * (2 - (int) inCanvas.renderMode) + offset;
        }

        static protected int CalculatePriorityOffset(int inSortingLayerId, int inDistanceToCamera, int inSortingOrder)
        {
            int layerIndex = GameSortingLayers.IndexOf(inSortingLayerId);
            return 500 * layerIndex + 500 - inDistanceToCamera + inSortingOrder;
        }
    }
}