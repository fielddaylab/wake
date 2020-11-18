using System;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    [RequireComponent(typeof(Toggle))]
    public class GenericObjectToggle : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Toggle m_Toggle = null;
        [Space]
        [SerializeField] private GameObject m_OnObject = null;
        [SerializeField] private bool m_Invert = false;

        #endregion // Inspector

        [NonSerialized] private BasePanel m_Menu;

        private void Awake()
        {
            m_OnObject.CacheComponent(ref m_Menu);

            m_Toggle.onValueChanged.AddListener(OnToggleChanged);
            OnToggleChanged(m_Toggle.isOn);

            if (m_Menu)
            {
                m_Menu.OnShowEvent.AddListener((t) => m_Toggle.SetIsOnWithoutNotify(true));
                m_Menu.OnHideEvent.AddListener((t) => m_Toggle.SetIsOnWithoutNotify(false));
            }
        }

        private void OnToggleChanged(bool inbOn)
        {
            if (m_Invert)
                inbOn = !inbOn;

            if (m_Menu)
            {
                if (inbOn)
                    m_Menu.Show();
                else
                    m_Menu.Hide();
            }
            else
            {
                m_OnObject.SetActive(inbOn);
            }
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            m_Toggle = GetComponent<Toggle>();
        }

        #endif // UNITY_EDITOR
    }
}