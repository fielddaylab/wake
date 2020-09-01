using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua
{
    [RequireComponent(typeof(BaseRaycaster))]
    public class InputRaycasterLayer : MonoBehaviour, IInputLayer
    {
        #region Inspector

        [SerializeField] private BaseRaycaster m_Raycaster = null;
        [SerializeField] private int m_Priority = 0;
        [SerializeField, AutoEnum] private InputLayerFlags m_Flags = InputLayerFlags.GameUI;
        [Space]
        [SerializeField] private bool m_Override = false;
        [SerializeField, ShowIfField("m_Override")] private bool m_OverrideState = true;
        [Space]
        [SerializeField] private bool m_AutoPush = false;

        #endregion // Inspector

        [NonSerialized] private int m_LastKnownSystemPriority = 0;
        [NonSerialized] private InputLayerFlags m_LastKnownSystemFlags = InputLayerFlags.All;

        public void ClearOverride()
        {
            if (m_Override)
            {
                m_Override = false;
                UpdateRaycasterEnabled();
            }
        }

        public void OverrideState(bool inbOverride)
        {
            if (!m_Override || m_OverrideState != inbOverride)
            {
                m_Override = true;
                m_OverrideState = inbOverride;
                UpdateRaycasterEnabled();
            }
        }

        #region Unity Events

        private void Awake()
        {
            this.CacheComponent(ref m_Raycaster);
        }

        private void OnEnable()
        {
            Services.Input.RegisterInput(this);
            if (m_AutoPush)
                Services.Input.PushPriority(this);
        }

        private void OnDisable()
        {
            if (Services.Input != null)
            {
                Services.Input.DeregisterInput(this);
                if (m_AutoPush)
                    Services.Input.PopPriority();
            }
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            this.CacheComponent(ref m_Raycaster);
            Canvas c = GetComponent<Canvas>();
            if (c != null)
            {
                switch(c.renderMode)
                {
                    case RenderMode.ScreenSpaceCamera:
                        m_Priority = 1000 - (int) c.planeDistance;
                        break;

                    case RenderMode.ScreenSpaceOverlay:
                        m_Priority = 1000 + c.renderOrder;
                        break;
                }
            }
        }

        private void OnValidate()
        {
            this.CacheComponent(ref m_Raycaster);
            if (Application.isPlaying)
                UpdateRaycasterEnabled();
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
            }
        }

        public void UpdateSystemPriority(int inSystemPriority)
        {
            if (m_LastKnownSystemPriority != inSystemPriority)
            {
                m_LastKnownSystemPriority = inSystemPriority;
                UpdateRaycasterEnabled();
            }
        }

        public void UpdateSystemFlags(InputLayerFlags inFlags)
        {
            if (m_LastKnownSystemFlags != inFlags)
            {
                m_LastKnownSystemFlags = inFlags;
                UpdateRaycasterEnabled();
            }
        }

        #endregion // IInputLayer

        private void UpdateRaycasterEnabled()
        {
            m_Raycaster.enabled = GetDesiredRaycasterState();
        }

        private bool GetDesiredRaycasterState()
        {
            if (m_Override)
                return m_OverrideState;

            return m_Priority >= m_LastKnownSystemPriority && (m_Flags == 0 || (m_LastKnownSystemFlags & m_Flags) != 0);
        }
    }
}