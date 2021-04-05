using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using System;

namespace Aqua
{
    public sealed class DialogHistoryButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Toggle m_Toggle = null;

        #endregion // Inspector

        [NonSerialized] private DialogHistoryPanel m_Menu;

        private void Awake()
        {
            m_Menu = Services.UI.FindPanel<DialogHistoryPanel>();
            m_Menu.OnHideEvent.AddListener(OnMenuClose);
            m_Toggle.onValueChanged.AddListener(OnToggleValue);
        }

        private void OnDestroy()
        {
            m_Menu?.OnHideEvent.RemoveListener(OnMenuClose);
        }

        private void OnDisable()
        {
            m_Toggle.isOn = false;
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