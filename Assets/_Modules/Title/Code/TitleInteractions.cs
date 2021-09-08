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
        [SerializeField] private Button m_StartButton = null;
        [SerializeField] private Button m_BackButton = null;

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

        private IEnumerator NewGame()
        {
            string profileName = m_ProfileName.text;

            Future<bool> exists = Services.Data.HasProfile(profileName);
            yield return exists;

            if (exists.Get())
            {
                Services.Input.ResumeAll();
                Future<StringHash32> overwrite = Services.UI.Popup.AskYesNo(
                    Loc.Find("ui.title.fileOverwrite.header"),
                    Loc.Format("ui.title.fileOverwrite.description", profileName));
                yield return overwrite;
                if (overwrite.Get() != PopupPanel.Option_Yes)
                    yield break;
                Services.Input.PauseAll();
            }

            Future<bool> newProfile = Services.Data.NewProfile(profileName);
            yield return newProfile;
            // TODO: Handle failure
            Services.Input.ResumeAll();
            Services.Data.StartPlaying("Ship");
        }

        private IEnumerator ContinueGame()
        {
            string profileName = m_ProfileName.text;

            Future<bool> exists = Services.Data.HasProfile(profileName);
            yield return exists;

            if (!exists.Get())
            {
                Services.Input.ResumeAll();
                Future<StringHash32> overwrite = Services.UI.Popup.AskYesNo(
                    Loc.Find("ui.title.fileMissing.header"),
                    Loc.Format("ui.title.fileMissing.description", profileName));
                yield return overwrite;
                if (overwrite.Get() != PopupPanel.Option_Yes)
                    yield break;

                Services.Input.PauseAll();
                Future<bool> newProfile = Services.Data.NewProfile(profileName);
                yield return newProfile;
                // TODO: Handle failure
                Services.Input.ResumeAll();
                Services.Data.StartPlaying("Ship");
            }
            else
            {
                Future<bool> load = Services.Data.LoadProfile(profileName);
                yield return load;
                // TODO: Handle failure

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
                        Routine.Start(this, m_InitialGroup.Hide(0.2f, false));
                        Routine.Start(this, m_ProfileGroup.Show(0.2f, true));
                        UpdateInteractable();
                        break;
                    }

                case Page.Continue:
                    {
                        m_ProfileHeader.SetText(ContinueHeader);
                        Routine.Start(this, m_InitialGroup.Hide(0.2f, false));
                        Routine.Start(this, m_ProfileGroup.Show(0.2f, true));
                        UpdateInteractable();
                        break;
                    }
            }
        }

        private void UpdateInteractable()
        {
            m_StartButton.interactable = m_ProfileName.text.Length > 0;
        }
    }
}