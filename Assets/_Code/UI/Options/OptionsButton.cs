using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using System;
using System.Collections;
using BeauRoutine.Extensions;

namespace Aqua.Option
{
    public class OptionsButton : BasePanel
    {
        #region Inspector

        [SerializeField, Required] private Toggle m_Toggle = null;

        #endregion // Inspector

        [NonSerialized] private OptionsMenu m_Menu;

        public Toggle Toggle { get { return m_Toggle; } }

        protected override void Awake()
        {
            base.Awake();

            m_Menu = Services.UI.FindPanel<OptionsMenu>();

            m_Toggle.onValueChanged.AddListener(OnToggleValue);

            Services.Events.Register(GameEvents.OptionsClosed, OnOptionsClose);
        }

        private void OnDestroy()
        {

            Services.Events?.DeregisterAll(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

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