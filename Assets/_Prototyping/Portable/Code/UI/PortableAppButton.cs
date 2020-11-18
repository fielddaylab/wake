using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;

namespace ProtoAqua.Portable
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
                m_App.Show();
            else
                m_App.Hide();
        }
    }
}