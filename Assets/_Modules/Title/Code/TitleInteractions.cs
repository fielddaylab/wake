using System;
using System.Collections;
using Aqua.Animation;
using Aqua.Cameras;
using AquaAudio;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Aqua.Title
{
    public class TitleInteractions : MonoBehaviour
    {
        static private readonly TextId NewHeader = "ui.title.new";
        static private readonly TextId ContinueHeader = "ui.title.continue";

        private enum Page
        {
            Title,
            Credits,
            New,
            Continue
        }

        #region Inspector

        [SerializeField] private CanvasGroup m_FullGroup = null;

        [Header("Initial Page")]
        [SerializeField] private CanvasGroup m_InitialGroup = null;
        [SerializeField] private Button m_NewGameButton = null;
        [SerializeField] private Button m_ContinueGameButton = null;
        [SerializeField] private Button m_CreditsButton = null;
        [SerializeField] private BasePanel m_SettingsPanel = null;

        [Header("Profile Page")]
        [SerializeField] private CanvasGroup m_ProfileGroup = null;
        [SerializeField] private LocText m_ProfileHeader = null;
        [SerializeField] private TMP_InputField m_ProfileName = null;
        [SerializeField] private Graphic m_EditableNameBG = null;
        [SerializeField] private Button m_StartButton = null;
        [SerializeField] private Button m_BackButton = null;
        [SerializeField] private GameObject m_LoadingSpinner = null;

        [Header("Credits Page")]
        [SerializeField] private BasePanel m_CreditsPanel = null;

        #endregion // Inspector

        [NonSerialized] private TitleConfig m_TitleConfig = null;
        [NonSerialized] private Page m_CurrentPage = Page.Title;

        private void Awake()
        {
            m_NewGameButton.onClick.AddListener(OnNewClick);
            m_ContinueGameButton.onClick.AddListener(OnContinueClick);
            m_BackButton.onClick.AddListener(OnBackClick);
            m_StartButton.onClick.AddListener(OnStartClicked);
            m_CreditsButton.onClick.AddListener(OnCreditsClick);
            m_CreditsPanel.OnHideEvent.AddListener(OnCreditsClose);

            m_ProfileName.onValueChanged.AddListener(OnProfileNameUpdated);
            m_ProfileName.text = Services.Data.LastProfileName();
            m_ProfileName.onSelect.AddListener(OnProfileNameSelected);

            m_SettingsPanel.OnShowEvent.AddListener(OnSettingsOpen);
            m_SettingsPanel.OnHideEvent.AddListener(OnSettingsClosed);

            m_InitialGroup.gameObject.SetActive(true);
            m_ProfileGroup.gameObject.SetActive(false);

            m_LoadingSpinner.SetActive(false);
        }

        public void LoadConfig(TitleConfig config) {
            m_TitleConfig = config;
        }

        #region Handlers

        private void OnCreditsClick()
        {
            LoadPage(Page.Credits);
        }

        private void OnCreditsClose(BasePanel.TransitionType _)
        {
            if (Services.Valid && !Script.IsLoading) {
                LoadPage(Page.Title);
            }
        }

        private void OnNewClick()
        {
            LoadPage(Page.New);
        }

        private void OnContinueClick()
        {
            LoadPage(Page.Continue);
        }

        private void OnBackClick()
        {
            LoadPage(Page.Title);
        }

        private void OnProfileNameUpdated(string inText)
        {
            UpdateInteractable();
        }

        private void OnProfileNameSelected(string inText) {
            Debug.Log("[Keyboard] Selected!");
            bool deviceIsIpad = UnityEngine.iOS.Device.generation.ToString().Contains("iPad");
            if (deviceIsIpad) {
                TouchScreenKeyboard.Open(m_ProfileName.text, TouchScreenKeyboardType.Default, false, false, false);
            }
        }

        private void OnStartClicked()
        {
            Assert.True(m_CurrentPage != Page.Title);

            Services.Input.PauseAll();
            
            switch(m_CurrentPage)
            {
                case Page.New:
                    Routine.Start(this, NewGame());
                    break;

                case Page.Continue:
                    Routine.Start(this, ContinueGame());
                    break;
            }
        }

        private IEnumerator NewGame() {
            m_LoadingSpinner.SetActive(true);

            string profileName = m_ProfileName.text;

            Future<bool> newProfile = Services.Data.NewProfile(profileName);
            yield return newProfile;

            m_LoadingSpinner.SetActive(false);

            if (!newProfile.IsComplete()) {
                Services.Input.ResumeAll();
                Services.UI.Popup.DisplayWithClose(
                    Loc.Find("ui.title.saveError.header"),
                    Loc.Format("ui.title.saveError.description", DataService.ErrorMessage(newProfile)));
            }
            else
            {
                Services.Input.ResumeAll();
                Services.Data.StartPlaying("RS-1C");
            }
        }

        private IEnumerator ContinueGame()
        {
            m_LoadingSpinner.SetActive(true);

            string profileName = m_ProfileName.text;

            Future<bool> load = Services.Data.LoadProfile(profileName);
            yield return load;

            m_LoadingSpinner.SetActive(false);

            if (!load.IsComplete()) {
                Services.Input.ResumeAll();

                switch(DataService.ReturnStatus(load)) {
                    case DataService.ErrorStatus.Error_Request: {
                        Services.UI.Popup.DisplayWithClose(
                            Loc.Find("ui.title.fileMissing.header"),
                            Loc.Format("ui.title.fileMissing.description", profileName));
                        break;
                    }

                    case DataService.ErrorStatus.DeserializeError: {
                        Services.UI.Popup.DisplayWithClose(
                            Loc.Find("ui.title.fileCorrupt.header"),
                            Loc.Format("ui.title.fileCorrupt.description", profileName));
                        break;
                    }

                    case DataService.ErrorStatus.OutOfDateError: {
                        Services.UI.Popup.DisplayWithClose(
                            Loc.Find("ui.title.earlyAccessFile.header"),
                            Loc.Format("ui.title.earlyAccessFile.description", profileName));
                        break;
                    }

                    default: {
                        Services.UI.Popup.DisplayWithClose(
                            Loc.Find("ui.title.loadError.header"),
                            Loc.Format("ui.title.loadError.description", DataService.ErrorMessage(load)));
                        break;
                    }
                }
            } else {
                Services.Input.ResumeAll();
                Services.Data.StartPlaying();
            }
        }

        private void OnSettingsOpen(BasePanel.TransitionType _) {
            m_InitialGroup.blocksRaycasts = false;
            m_ProfileGroup.blocksRaycasts = false;
        }

        private void OnSettingsClosed(BasePanel.TransitionType _) {
            m_InitialGroup.blocksRaycasts = m_InitialGroup.isActiveAndEnabled;
            m_ProfileGroup.blocksRaycasts = m_ProfileGroup.isActiveAndEnabled;
        }

        #endregion // Handlers

        private void LoadPage(Page inPage)
        {
            Page prevPage = m_CurrentPage;
            m_CurrentPage = inPage;

            switch(inPage)
            {
                case Page.Title:
                    if (prevPage == Page.Credits) {
                        m_CreditsPanel.Hide();
                        Routine.Start(this, m_FullGroup.Show(0.2f, true)).DelayBy(0.6f).ExecuteWhileDisabled();
                        Services.Camera.MoveToPose(m_TitleConfig.FullPose, 1f, Curve.Smooth, CameraPoseProperties.All);
                    } else {
                        Routine.Start(this, m_ProfileGroup.Hide(0.2f, false));
                        Routine.Start(this, m_InitialGroup.Show(0.2f, true));
                    }
                    break;

                case Page.Credits:
                    Routine.Start(this, m_FullGroup.Hide(0.2f, false)).ExecuteWhileDisabled();
                    m_CreditsPanel.Show(0.6f);
                    Services.Camera.MoveToPose(m_TitleConfig.CreditsPose, 1f, Curve.Smooth, CameraPoseProperties.All);
                    break;

                case Page.New:
                    {
                        m_ProfileHeader.SetText(NewHeader);
                        m_ProfileName.interactable = false;
                        m_ProfileName.SetTextWithoutNotify("---");
                        m_EditableNameBG.enabled = false;
                        m_ProfileName.textComponent.color = Color.gray;
                        Routine.Start(this, m_InitialGroup.Hide(0.2f, false));
                        Routine.Start(this, m_ProfileGroup.Show(0.2f, true));
                        m_ProfileGroup.interactable = false;
                        UpdateInteractable();
                        Services.Input.PauseAll();
                        m_LoadingSpinner.SetActive(true);
                        OGD.Player.NewId(OnNewNameSuccess, OnNewNameFail);
                        break;
                    }

                case Page.Continue:
                    {
                        m_ProfileHeader.SetText(ContinueHeader);
                        m_ProfileName.interactable = true;
                        m_ProfileName.SetTextWithoutNotify(Services.Data.LastProfileName());
                        m_EditableNameBG.enabled = true;
                        m_ProfileName.textComponent.color = Color.black;
                        Routine.Start(this, m_InitialGroup.Hide(0.2f, false));
                        Routine.Start(this, m_ProfileGroup.Show(0.2f, true));
                        UpdateInteractable();
                        break;
                    }
            }
        }

        private void OnNewNameSuccess(string inName)
        {
            m_ProfileName.text = inName;
            m_ProfileGroup.interactable = true;
            Services.Input.ResumeAll();
            m_LoadingSpinner.SetActive(false);
        }

        private void OnNewNameFail(OGD.Core.Error error)
        {
            Log.Error("[TitleInteractions] Generating new player id failed: {0}", error.Msg);
            Services.Input.ResumeAll();
            m_LoadingSpinner.SetActive(false);
            Services.UI.Popup.DisplayWithClose(
                Loc.Find("ui.title.idGenerationError.header"),
                Loc.Format("ui.title.idGenerationError.description", error.Msg))
                .OnComplete((e) => LoadPage(Page.Title));
        }

        private void UpdateInteractable()
        {
            m_StartButton.interactable = m_ProfileName.text.Length > 0;
        }
    }
}