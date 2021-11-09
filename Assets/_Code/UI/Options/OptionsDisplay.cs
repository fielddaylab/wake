using System;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField, Required] private LayoutGroup m_Layout = null;

        [Header("Pages")]
        [SerializeField, Required] private AudioPanel m_AudioPanel = null;
        [SerializeField, Required] private GamePanel m_GamePanel = null;
        [SerializeField, Required] private QualityPanel m_QualityPanel = null;
        [SerializeField, Required] private AccessibilityPanel m_AccessibilityPanel = null;

        #endregion //Inspector

        private void Awake() {
            Async.InvokeAsync(() => m_Layout.ForceRebuild());
        }

        private void OnEnable() {
            if (m_UserNameLabel) {
                m_UserNameLabel.SetText(Save.Id);
            }

            LoadOptions(Save.Options);
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