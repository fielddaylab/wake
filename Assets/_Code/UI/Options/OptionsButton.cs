using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using System;

namespace Aqua.Option
{
    public class OptionsButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Toggle m_Toggle = null;

        #endregion // Inspector

        [NonSerialized] private OptionsMenu m_Menu;

        private void Awake()
        {
            m_Menu = Services.UI.FindPanel<OptionsMenu>();
            m_Toggle.onValueChanged.AddListener(OnToggleValue);
            m_Menu.OnHideEvent.AddListener((m) => OnOptionsClose());
        }

        #region Handlers

        private void OnOptionsClose()
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