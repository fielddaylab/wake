using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace ProtoAqua
{
    public class WorldInput : MonoBehaviour, IInputLayer
    {
        #region Inspector

        [SerializeField] protected int m_Priority = 10;
        [SerializeField, AutoEnum] protected InputLayerFlags m_LayerFlags = InputLayerFlags.PlayerControls;

        #endregion // Inspector

        [NonSerialized] protected bool? m_Override;
        [NonSerialized] protected int m_LastSystemPriority;
        [NonSerialized] protected InputLayerFlags m_LastSystemFlags;
        [NonSerialized] protected bool m_InputEnabled;

        #region Unity Events

        protected virtual void OnEnable()
        {
            if (Services.Input)
            {
                Services.Input.RegisterInput(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (Services.Input)
            {
                Services.Input.DeregisterInput(this);
            }
        }

        protected virtual void OnInputEnabled()
        {

        }

        protected virtual void OnInputDisabled()
        {

        }

        #endregion // Unity Events

        #region IInputLayer

        public int Priority { get { return m_Priority; } }
        public InputLayerFlags Flags { get { return InputLayerFlags.PlayerControls; } }
        public bool? Override
        {
            get { return m_Override; }
            set { m_Override = value; UpdateInputEnabled(); }
        }

        public void UpdateSystemPriority(int inSystemPriority)
        {
            m_LastSystemPriority = inSystemPriority;
            UpdateInputEnabled();
        }

        public void UpdateSystemFlags(InputLayerFlags inputLayerFlags)
        {
            m_LastSystemFlags = inputLayerFlags;
            UpdateInputEnabled();
        }

        protected virtual void UpdateInputEnabled()
        {
            bool bDesired = GetDesiredInputState();
            if (m_InputEnabled != bDesired)
            {
                m_InputEnabled = bDesired;
                if (bDesired)
                    OnInputEnabled();
                else
                    OnInputDisabled();
            }
        }

        protected virtual bool GetDesiredInputState()
        {
            if (m_Override.HasValue)
            {
                return m_Override.Value;
            }
            else
            {
                return m_Priority >= m_LastSystemPriority && (m_LayerFlags == 0 || (m_LastSystemFlags & m_LayerFlags) != 0);
            }
        }

        #endregion // IInputLayer
    }
}