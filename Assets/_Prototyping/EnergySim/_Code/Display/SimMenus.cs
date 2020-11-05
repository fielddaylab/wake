using System;
using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using System.Collections;
using BeauRoutine.Extensions;

namespace ProtoAqua.Energy
{
    public class SimMenus : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private CanvasGroup m_Fader = null;

        [Header("Info Panel")]
        [SerializeField] private SimIntroPanel m_InfoPanel = null;
        [SerializeField] private Button m_InfoButton = null;
        
        [Header("Help Panel")]
        [SerializeField] private SimHelpPanel m_HelpPanel = null;
        [SerializeField] private Button m_HelpButton = null;
        
        [Header("Complete Panel")]
        [SerializeField] private SimCompletePanel m_CompletePanel = null;
        
        [Header("Edit Panel")]
        [SerializeField] private SimEditPanel m_EditPanel = null;
        [SerializeField] private Button m_EditButton = null;
        [SerializeField] private RectTransform m_EditSeparator = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Open = false;
        [NonSerialized] private Routine m_FaderAnim;

        [NonSerialized] private ScenarioPackage m_ScenarioPackage;
        [NonSerialized] private ISimDatabase m_Database;

        public bool IsOpen() { return m_Open; }

        public SimEditPanel EditPanel { get { return m_EditPanel; } }

        #region Unity Events

        private void Awake()
        {
            m_InfoButton.onClick.AddListener(HandleInfoButtonClicked);
            m_HelpButton.onClick.AddListener(HandleHelpButtonClicked);
            m_EditButton.onClick.AddListener(HandleEditButtonClicked);

            RegisterPanelUpdateHandler(m_InfoPanel);
            RegisterPanelUpdateHandler(m_HelpPanel);
            RegisterPanelUpdateHandler(m_CompletePanel);
            RegisterPanelUpdateHandler(m_EditPanel);

            bool bDevelopment = false;
            #if DEVELOPMENT
            bDevelopment = true;
            #endif // DEVELOPMENT

            m_EditButton.gameObject.SetActive(bDevelopment);
            m_EditSeparator.gameObject.SetActive(bDevelopment);
        }

        private void RegisterPanelUpdateHandler(BasePanel inPanel)
        {
            inPanel.OnShowEvent.AddListener(HandlePanelUpdate);
            inPanel.OnHideEvent.AddListener(HandlePanelUpdate);
        }

        #endregion // Unity Events

        public void Initialize(ScenarioPackage inScenario, ISimDatabase inDatabase)
        {
            m_ScenarioPackage = inScenario;
            m_Database = inDatabase;
        }

        public void ShowIntro()
        {
            m_InfoPanel.Display(m_ScenarioPackage.Header, true);
        }

        public void ShowComplete()
        {
            m_CompletePanel.Display(m_ScenarioPackage.Header);
        }

        #region Handlers

        private void HandleInfoButtonClicked()
        {
            m_InfoPanel.Display(m_ScenarioPackage.Header, false);
        }

        private void HandleHelpButtonClicked()
        {
            m_HelpPanel.Display(m_ScenarioPackage.Header);
        }

        private void HandleEditButtonClicked()
        {
            m_EditPanel.Display(m_ScenarioPackage, m_Database);
        }

        private void HandlePanelUpdate(BasePanel.TransitionType inTransition)
        {
            bool bOpen = m_InfoPanel.IsShowing() || m_HelpPanel.IsShowing() || m_EditPanel.IsShowing() || m_CompletePanel.IsShowing();
            if (bOpen != m_Open)
            {
                m_Open = bOpen;
                if (inTransition == BasePanel.TransitionType.Instant)
                {
                    m_FaderAnim.Stop();
                    m_Fader.alpha = m_Open ? 1 : 0;
                    m_Fader.gameObject.SetActive(m_Open);
                }
                else
                {
                    if (m_Open)
                        m_FaderAnim.Replace(this, OpenAnim());
                    else
                        m_FaderAnim.Replace(this, CloseAnim());
                }
            }
        }

        #endregion // Handlers

        #region Routines

        private IEnumerator OpenAnim()
        {
            if (!m_Fader.gameObject.activeSelf)
            {
                m_Fader.alpha = 0;
                m_Fader.gameObject.SetActive(true);
            }

            m_Fader.blocksRaycasts = true;

            yield return m_Fader.FadeTo(1f, 0.1f);
        }

        private IEnumerator CloseAnim()
        {
            if (m_Fader.gameObject.activeSelf)
            {
                m_Fader.blocksRaycasts = false;
                yield return m_Fader.FadeTo(0f, 0.1f);
                m_Fader.gameObject.SetActive(false);
            }
        }

        #endregion // Routines
    }
}