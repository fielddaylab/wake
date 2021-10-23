using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using Aqua;
using System;
using System.Collections;
using BeauRoutine.Extensions;

namespace Aqua.Portable
{
    public class PortableTabToggle : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Toggle m_Toggle = null;
        [SerializeField] private PortableAppId m_Id = default;
        [SerializeField, Required] private PortableMenuApp m_App = null;
        [SerializeField, Required] private TMP_Text m_Label = null;

        #endregion // Inspector

        public PortableAppId Id() { return m_Id; }

        public Toggle Toggle { get { return m_Toggle; } }
        public PortableMenuApp App { get { return m_App; } }

        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener(OnToggleValue);

            if (m_App != null)
            {
                m_App.OnShowEvent.AddListener(OnOpened);
                m_App.OnHideEvent.AddListener(OnClosed);
            }
        }

        private void OnDisable()
        {
            m_Toggle.isOn = false;
        }

        private void OnToggleValue(bool inbValue)
        {
            if (!m_App || !isActiveAndEnabled)
                return;

            if (inbValue)
            {
                m_App.Show();
            }
            else
            {
                m_App.Hide();
            }
        }

        private void OnOpened(BasePanel.TransitionType inTransition)
        {
            m_Label.font = Assets.Font(FontWeight.SemiBold);
            m_Toggle.SetIsOnWithoutNotify(true);
        }

        private void OnClosed(BasePanel.TransitionType inTransition)
        {
            m_Label.font = Assets.Font(FontWeight.Regular);
            m_Toggle.SetIsOnWithoutNotify(false);
        }
    }
}