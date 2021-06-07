using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using System;
using AquaAudio;
using Aqua.Scripting;
using BeauUtil;

namespace Aqua.Option 
{
    public class OptionsMenu : SharedPanel 
    {
        public enum PageId
        {
            Audio,
            Game,
            Quality,
            Accessibility
        }

        public class Panel : MonoBehaviour
        {
            public OptionsData Data { get; private set; }

            public virtual void Load(OptionsData inOptions) { Data = inOptions; }
        }

        #region Inspector

        [SerializeField] private Button m_CloseButton = null;

        [Header("Tabs")]
        [SerializeField] private ToggleGroup m_TabList = null;

        [Header("Pages")]
        [SerializeField] private AudioPanel m_AudioPanel = null;
        [SerializeField] private GamePanel m_GamePanel = null;
        [SerializeField] private QualityPanel m_QualityPanel = null;
        [SerializeField] private AccessibilityPanel m_AccessibilityPanel = null;

        #endregion //Inspector

        [NonSerialized] private BaseInputLayer m_InputLayer = null;
        [NonSerialized] private OptionsTabToggle[] m_Tabs = null;
        [NonSerialized] private Panel m_CurrentPanel;
        
        static private PageId s_LastPage;

        protected override void Awake() 
        {
            base.Awake();

            m_CloseButton.onClick.AddListener(() => Hide());

            m_InputLayer = BaseInputLayer.Find(this);
            Services.Events.Register(GameEvents.SceneWillUnload, InstantHide);

            m_Tabs = m_TabList.GetComponentsInChildren<OptionsTabToggle>();
            foreach(var tab in m_Tabs)
            {
                PageId page = tab.Page;
                tab.Toggle.onValueChanged.AddListener((b) => OnToggleChanged(page, b));
            }
        }

        protected override void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
            base.OnDestroy();
        }

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            if (m_InputLayer.PushPriority())
            {
                Services.Pause.Pause();
            }
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);

            SetPage(s_LastPage, true);
        }

        protected override void OnHide(bool inbInstant)
        {
            if (Services.Valid)
            {
                Services.Data?.SaveOptionsSettings();
            }

            if (m_InputLayer.PopPriority())
            {
                Services.Pause?.Resume();
            }

            m_CurrentPanel = null;
            
            base.OnHide(inbInstant);
        }

        private void OnToggleChanged(PageId inPage, bool inbValue)
        {
            if (inbValue)
                SetPage(inPage, false);
        }

        private void SetPage(PageId inPage, bool inbForce)
        {
            if (!inbForce && s_LastPage == inPage)
                return;

            s_LastPage = inPage;
            if (m_CurrentPanel)
                m_CurrentPanel.gameObject.SetActive(false);

            switch(inPage)
            {
                case PageId.Accessibility:
                    {
                        m_CurrentPanel = m_AccessibilityPanel;
                        break;
                    }

                case PageId.Audio:
                    {
                        m_CurrentPanel = m_AudioPanel;
                        break;
                    }

                case PageId.Game:
                    {
                        m_CurrentPanel = m_GamePanel;
                        break;
                    }

                case PageId.Quality:
                    {
                        m_CurrentPanel = m_QualityPanel;
                        break;
                    }
            }

            m_CurrentPanel.gameObject.SetActive(true);
            m_CurrentPanel.Load(Services.Data.Options);

            OptionsTabToggle toggle;
            m_Tabs.TryGetValue(s_LastPage, out toggle);
            toggle.Toggle.SetIsOnWithoutNotify(true);
        }
    }
}