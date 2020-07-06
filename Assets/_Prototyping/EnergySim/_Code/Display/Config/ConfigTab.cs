using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class ConfigTab : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Button m_Button = null;
        [SerializeField] private Graphic m_SelectedGraphic = null;
        [SerializeField] private GameObject m_Content = null;

        #endregion // Inspector

        public Button Button { get { return m_Button; } }

        public void SetSelected(bool inbSelected)
        {
            m_SelectedGraphic.gameObject.SetActive(inbSelected);
            m_Content.gameObject.SetActive(inbSelected);
        }
    }
}