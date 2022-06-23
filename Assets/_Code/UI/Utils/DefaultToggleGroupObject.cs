using System;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    [RequireComponent(typeof(ToggleGroup))]
    public class DefaultToggleGroupObject : MonoBehaviour, IUpdaterUI
    {
        #region Inspector

        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [Space]
        [SerializeField] private GameObject m_DefaultObject = null;
        [SerializeField] private bool m_Invert = false;

        #endregion // Inspector

        [NonSerialized] private bool m_Default = false;

        private void Start()
        {
            OnToggleChanged(!m_ToggleGroup.AnyTogglesOn());
        }

        private void OnEnable() {
            Services.UI.RegisterUpdate(this);
        }

        private void OnDisable() {
            Services.UI?.DeregisterUpdate(this);
        }

        public void OnUIUpdate()
        {
            bool bDefault = !m_ToggleGroup.AnyTogglesOn();
            if (bDefault != m_Default)
            {
                OnToggleChanged(bDefault);
            }
        }

        private void OnToggleChanged(bool inbOn)
        {
            m_Default = inbOn;

            if (m_Invert)
                inbOn = !inbOn;
            m_DefaultObject.SetActive(inbOn);
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            m_ToggleGroup = GetComponent<ToggleGroup>();
        }

        #endif // UNITY_EDITOR
    }
}