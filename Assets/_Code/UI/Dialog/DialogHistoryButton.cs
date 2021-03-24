using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using System;
using System.Collections;
using BeauRoutine.Extensions;

namespace Aqua
{
    public class DialogHistoryButton : BasePanel
    {
        #region Inspector

        [SerializeField, Required] private Toggle m_Toggle = null;

        #endregion // Inspector

        [NonSerialized] private DialogHistoryPanel m_Menu;

        protected override void Awake()
        {
            base.Awake();

            m_Menu = Services.UI.DialogHistory;
            m_Menu.OnHideEvent.AddListener(OnMenuClose);
            m_Toggle.onValueChanged.AddListener(OnToggleValue);
        }

        private void OnDestroy()
        {
            m_Menu?.OnHideEvent.RemoveListener(OnMenuClose);
        }

        protected override void OnDisable()
        {
            m_Toggle.isOn = false;
            base.OnDisable();
        }

        #region Handlers

        private void OnMenuClose(SharedPanel.TransitionType inTransition)
        {
            m_Toggle.SetIsOnWithoutNotify(false);
        }

        private void OnToggleValue(bool inbValue)
        {
            if (!m_Menu || !isActiveAndEnabled)
                return;
            
            if (inbValue)
            {
                m_Menu.Show();
            }
            else
            {
                m_Menu.Hide();
            }
        }

        #endregion // Handlers
    }
}