using System;
using System.Collections;
using Aqua.Animation;
using AquaAudio;
using BeauPools;
using BeauRoutine;
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
            New,
            Continue
        }

        #region Inspector

        [Header("Initial Page")]
        [SerializeField] private CanvasGroup m_InitialGroup = null;
        [SerializeField] private Button m_NewGameButton = null;
        [SerializeField] private Button m_ContinueGameButton = null;

        [Header("Profile Page")]
        [SerializeField] private CanvasGroup m_ProfileGroup = null;
        [SerializeField] private LocText m_ProfileHeader = null;
        [SerializeField] private TMP_InputField m_ProfileName = null;
        [SerializeField] private GameObject m_ProfileNameLocked = null;
        [SerializeField] private Button m_StartButton = null;
        [SerializeField] private Button m_BackButton = null;
        [SerializeField] private GameObject m_NameLoadingSpinner = null;
        [SerializeField] private GameObject m_GameStartingSpinner = null;

        #endregion // Inspector

        [NonSerialized] private Page m_CurrentPage = Page.Title;

        private void Awake()
        {
            m_NewGameButton.onClick.AddListener(OnNewClick);
            m_ContinueGameButton.onClick.AddListener(OnContinueClick);
            m_BackButton.onClick.AddListener(OnBackClick);
            m_StartButton.onClick.AddListener(OnStartClicked);

            m_ProfileName.onValueChanged.AddListener(OnProfileNameUpdated);
            m_ProfileName.text = Services.Data.LastProfileName();

            m_InitialGroup.gameObject.SetActive(true);
            m_ProfileGroup.gameObject.SetActive(false);

            m_NameLoadingSpinner.SetActive(false);
            m_GameStartingSpinner.SetActive(false);
        }

        #region Handlers

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
            m_GameStartingSpinner.SetActive(true);

            string profileName = m_ProfileName.text;

            Future<bool> newProfile = Services.Data.NewProfile(profileName);
            yield return newProfile;

            m_GameStartingSpinner.SetActive(false);

            if (!newProfile.IsComplete()) {
                Services.Input.ResumeAll();
                Services.UI.Popup.DisplayWithClose(
                    Loc.Find("ui.title.saveError.header"),
                    Loc.Format("ui.title.saveError.description", DataService.ErrorMessage(newProfile)));
            }
            else
            {
                Services.Input.ResumeAll();
                Services.Data.StartPlaying("RS-1C-Tutorial");
            }
        }

        private IEnumerator ContinueGame()
        {
            m_GameStartingSpinner.SetActive(true);

            string profileName = m_ProfileName.text;

            Future<bool> load = Services.Data.LoadProfile(profileName);
            yield return load;

            m_GameStartingSpinner.SetActive(false);

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

        #endregion // Handlers

        private void LoadPage(Page inPage)
        {
            m_CurrentPage = inPage;

            switch(inPage)
            {
                case Page.Title:
                    Routine.Start(this, m_ProfileGroup.Hide(0.2f, false));
                    Routine.Start(this, m_InitialGroup.Show(0.2f, true));
                    break;

                case Page.New:
                    {
                        m_ProfileHeader.SetText(NewHeader);
                        m_ProfileName.interactable = false;
                        m_ProfileNameLocked.SetActive(true);
                        m_ProfileName.SetTextWithoutNotify("---");
                        Routine.Start(this, m_InitialGroup.Hide(0.2f, false));
                        Routine.Start(this, m_ProfileGroup.Show(0.2f, true));
                        m_ProfileGroup.interactable = false;
                        UpdateInteractable();
                        Services.Input.PauseAll();
                        m_NameLoadingSpinner.SetActive(true);
                        OGD.Player.NewId(OnNewNameSuccess, OnNewNameFail);
                        break;
                    }

                case Page.Continue:
                    {
                        m_ProfileHeader.SetText(ContinueHeader);
                        m_ProfileName.interactable = true;
                        m_ProfileNameLocked.SetActive(false);
                        m_ProfileName.SetTextWithoutNotify(Services.Data.LastProfileName());
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
            m_NameLoadingSpinner.SetActive(false);
        }

        private void OnNewNameFail(OGD.Core.Error error)
        {
            Log.Error("[TitleInteractions] Generating new player id failed: {0}", error.Msg);
            Services.Input.ResumeAll();
            m_NameLoadingSpinner.SetActive(false);
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