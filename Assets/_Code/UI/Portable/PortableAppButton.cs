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
    public class PortableAppButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Toggle m_Toggle = null;
        [SerializeField] private SerializedHash32 m_Id = null;
        [SerializeField, Required] private PortableMenuApp m_App = null;

        #endregion // Inspector

        public StringHash32 Id() { return m_Id; }

        public Toggle Toggle { get { return m_Toggle; } }
        public PortableMenuApp App { get { return m_App; } }

        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener(OnToggleValue);

            if (m_App != null)
            {
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
                //User clicked app button
                //Debug.Log("App button with ID " + m_Id.ToString() + " clicked");
                Services.Events.Dispatch(GameEvents.PortableAppOpened, gameObject.name);
                m_App.Show();
            }
            else
            {
                Services.Events.Dispatch(GameEvents.PortableAppClosed, gameObject.name);
                m_App.Hide();
            }
        }

        private void OnClosed(BasePanel.TransitionType inTransition)
        {
            m_Toggle.SetIsOnWithoutNotify(false);
        }
    }
}