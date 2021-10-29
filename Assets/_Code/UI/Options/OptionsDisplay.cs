using System;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;

namespace Aqua.Option
{
    public sealed class OptionsDisplay : MonoBehaviour {
        public class Panel : MonoBehaviour {
            public OptionsData Data { get; private set; }

            [NonSerialized] private bool m_Initialized;

            public void TryInit() {
                if (!m_Initialized) {
                    Init();
                    m_Initialized = true;
                }
            }

            protected virtual void Init() { }
            public virtual void Load(OptionsData inOptions) { Data = inOptions; }
        }

        #region Inspector

        [SerializeField] private TMP_Text m_UserNameLabel = null;

        [Header("Pages")]
        [SerializeField, Required] private AudioPanel m_AudioPanel = null;
        [SerializeField, Required] private GamePanel m_GamePanel = null;
        [SerializeField, Required] private QualityPanel m_QualityPanel = null;
        [SerializeField, Required] private AccessibilityPanel m_AccessibilityPanel = null;

        #endregion //Inspector

        private void OnEnable() {
            if (m_UserNameLabel) {
                m_UserNameLabel.SetText(Services.Data.Profile.Id);
            }

            LoadOptions(Services.Data.Options);
        }

        private void OnDisable() {
            if (Services.Valid) {
                Services.Data?.SaveOptionsSettings();
            }
        }

        private void LoadOptions(OptionsData inData) {
            m_AudioPanel.TryInit();
            m_GamePanel.TryInit();
            m_QualityPanel.TryInit();
            m_AccessibilityPanel.TryInit();

            m_AudioPanel.Load(inData);
            m_GamePanel.Load(inData);
            m_QualityPanel.Load(inData);
            m_AccessibilityPanel.Load(inData);
        }
    }
}