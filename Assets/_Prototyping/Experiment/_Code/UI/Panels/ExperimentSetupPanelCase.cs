using System;
using UnityEngine;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauRoutine.Extensions;
using UnityEngine.UI;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupPanelCase : BasePanel
    {
        #region Inspector

        [Header("Animation Settings")]
        [SerializeField] private float m_OffscreenY = -660;
        [SerializeField] private TweenSettings m_ToOnAnim = default(TweenSettings);
        [SerializeField] private TweenSettings m_ToOffAnim = default(TweenSettings);

        [Header("Shared Interface")]
        [SerializeField] private CanvasGroup m_SharedGroup = null;
        [SerializeField] private Button m_CloseButton = null;

        [Header("Panels")]
        [SerializeField] private ExperimentSetupSubscreenBoot m_BootScreen = null;
        [SerializeField] private ExperimentSetupSubscreenTank m_TankScreen = null;
        [SerializeField] private ExperimentSetupSubscreenEco m_EcoScreen = null;
        [SerializeField] private ExperimentSetupSubscreenHypothesis m_HypothesisScreen = null;
        [SerializeField] private ExperimentSetupSubscreenActors m_ActorsScreen = null;
        [SerializeField] private ExperimentSetupSubscreenBegin m_BeginExperimentScreen = null;
        [SerializeField] private ExperimentSetupSubscreenInProgress m_InProgressScreen = null;
        [SerializeField] private ExperimentSetupSubscreenSummary m_SummaryScreen = null;

        #endregion // Inspector

        [NonSerialized] private AudioHandle m_Hum;
        [NonSerialized] private ExperimentSetupSubscreen m_CurrentSubscreen;
        [NonSerialized] private Routine m_SwitchSubscreenRoutine;

        [NonSerialized] private ExperimentSetupData m_SelectionData;
        [NonSerialized] private bool m_ExperimentSetup = false;
        [NonSerialized] private bool m_ExperimentRunning = false;

        protected override void Awake()
        {
            m_CloseButton.onClick.AddListener(() => CancelExperiment());

            Services.Events.Register(ExperimentEvents.SetupPanelOn, () => Show(), this)
                .Register(ExperimentEvents.SetupInitialSubmit, OnExperimentSubmitInitial, this)
                .Register(ExperimentEvents.ExperimentTeardown, OnExperimentTeardown, this)
                .Register(ExperimentEvents.ExperimentBegin, OnExperimentBegin, this);

            m_BootScreen.OnSelectContinue = () => SetSubscreen(m_TankScreen);
            m_TankScreen.OnSelectContinue = () => SetSubscreen(m_EcoScreen);
            m_EcoScreen.OnSelectBack = () => SetSubscreen(m_TankScreen, true);
            m_EcoScreen.OnSelectContinue = () => SetSubscreen(m_HypothesisScreen);
            m_HypothesisScreen.OnSelectBack = () => SetSubscreen(m_EcoScreen, true);
            m_HypothesisScreen.OnSelectContinue = () => OnHypothesisSubmit();
            m_ActorsScreen.OnSelectContinue = () => SetSubscreen(m_BeginExperimentScreen);
            m_BeginExperimentScreen.OnSelectBack = () => SetSubscreen(m_ActorsScreen, true);
            m_BeginExperimentScreen.OnSelectStart = () => StartExperiment();
            m_InProgressScreen.OnSelectEnd = () => TryEndExperiment();

            m_SelectionData = new ExperimentSetupData();
            m_TankScreen.SetData(m_SelectionData);
            m_EcoScreen.SetData(m_SelectionData);
            m_ActorsScreen.SetData(m_SelectionData);
            m_BeginExperimentScreen.SetData(m_SelectionData);
            m_InProgressScreen.SetData(m_SelectionData);
            m_SummaryScreen.SetData(m_SelectionData);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        #region BasePanel

        protected override void OnShow(bool inbInstant)
        {
            m_Hum = Services.Audio.PostEvent("tablet_hum").SetVolume(0).SetVolume(1, 0.5f);
            Services.Data.SetVariable(ExperimentVars.SetupPanelOn, true);
        }

        protected override void OnHide(bool inbInstant)
        {
            if (WasShowing() && Services.Valid)
            {
                m_Hum.Stop(0.5f);
                Services.Events.Dispatch(ExperimentEvents.SetupPanelOff);

                if (!Services.State.IsLoadingScene())
                {
                    Services.Data.SetVariable(ExperimentVars.SetupPanelOn, false);
                    Services.Data.SetVariable(ExperimentVars.SetupPanelScreen, null);
                }
            }
        }

        protected override void InstantTransitionToHide()
        {
            m_RootTransform.gameObject.SetActive(false);
            m_RootTransform.SetAnchorPos(-m_OffscreenY, Axis.Y);
            SetSubscreen(null);
            m_SharedGroup.alpha = 0;
        }

        protected override IEnumerator TransitionToHide()
        {
            SetSubscreen(null);
            yield return 0.1f;
            m_SharedGroup.alpha = 0;
            yield return m_RootTransform.AnchorPosTo(m_OffscreenY, m_ToOffAnim, Axis.Y);
            m_RootTransform.gameObject.SetActive(false);
        }

        protected override void InstantTransitionToShow()
        {
            m_RootTransform.SetAnchorPos(0, Axis.Y);
            m_RootTransform.gameObject.SetActive(true);
            SetSubscreen(GetBootScreen());
            m_SharedGroup.alpha = 1;
        }

        protected override IEnumerator TransitionToShow()
        {
            if (!m_RootTransform.gameObject.activeSelf)
            {
                m_RootTransform.SetAnchorPos(m_OffscreenY, Axis.Y);
                m_RootTransform.gameObject.SetActive(true);
            }

            SetSubscreen(null, false);
            m_SharedGroup.alpha = 0;

            yield return m_RootTransform.AnchorPosTo(0, m_ToOnAnim, Axis.Y);
            
            yield return 0.1f;
            m_SharedGroup.alpha = 1;

            SetSubscreen(GetBootScreen(), false);
        }

        private ExperimentSetupSubscreen GetBootScreen()
        {
            if (m_ExperimentRunning)
                return m_InProgressScreen;
            return m_BootScreen;
        }

        #endregion // BasePanel

        #region Callbacks

        private void OnHypothesisSubmit()
        {
            var kevinResponse = Services.Script.TriggerResponse(ExperimentTriggers.TrySubmitHypothesis);
            if (!kevinResponse.IsRunning())
            {
                SetInputState(false);
                Routine.Start(this, LoadExperimentRoutine());
            }
        }

        private IEnumerator LoadExperimentRoutine()
        {
            using(var tempFader = Services.UI.ScreenFaders.AllocFader())
            {
                Services.UI.ShowLetterbox();
                yield return tempFader.Object.Show(Color.black, 0.5f);
                Services.Events.Dispatch(ExperimentEvents.SetupInitialSubmit, m_SelectionData);
                m_ExperimentSetup = true;
                yield return 0.2f;
                Services.UI.HideLetterbox();
                SetSubscreen(m_ActorsScreen);
                yield return tempFader.Object.Hide(0.5f, false);
                SetInputState(true);
            }
        }

        private void CancelExperiment()
        {
            bool bExit;
            if (m_CurrentSubscreen != null && m_CurrentSubscreen.ShouldCancelOnExit().HasValue)
                bExit = m_CurrentSubscreen.ShouldCancelOnExit().Value;
            else
                bExit = m_ExperimentSetup && !m_ExperimentRunning;
            
            if (bExit)
            {
                SetInputState(false);
                Routine.Start(this, ExitExperimentRoutine());
            }
            else
            {
                if (m_CurrentSubscreen == m_SummaryScreen)
                {
                    Services.Script.TriggerResponse(ExperimentTriggers.ExperimentFinished);
                }
                Hide();
            }
        }

        private void TryEndExperiment()
        {
            var kevinResponse = Services.Script.TriggerResponse(ExperimentTriggers.TryEndExperiment);
            if (!kevinResponse.IsRunning())
            {
                Services.UI.Popup.AskYesNo("End Experiment?", "Do you want to end the experiment?")
                    .OnComplete((a) => {
                        if (a == PopupPanel.Option_Yes)
                        {
                            SetInputState(false);
                            Routine.Start(this, ExitExperimentRoutine());
                        }
                    });
            }
        }

        private IEnumerator ExitExperimentRoutine()
        {
            using(var tempFader = Services.UI.ScreenFaders.AllocFader())
            {
                bool bWasRunning = m_ExperimentRunning;
                Services.UI.ShowLetterbox();
                yield return tempFader.Object.Show(Color.black, 0.5f);
                if (bWasRunning)
                {
                    ExperimentResultData result = new ExperimentResultData();
                    result.Setup = m_SelectionData.Clone();
                    Services.Events.Dispatch(ExperimentEvents.ExperimentRequestSummary, result);
                    m_SummaryScreen.Populate(result);
                }
                Services.Events.Dispatch(ExperimentEvents.ExperimentTeardown);
                yield return 0.2f;
                Services.UI.HideLetterbox();
                if (!bWasRunning)
                {
                    InstantHide();
                }
                else
                {
                    SetInputState(true);
                    SetSubscreen(m_SummaryScreen);
                }
                yield return tempFader.Object.Hide(0.5f, false);
            }
        }

        private void StartExperiment()
        {
            var kevinResponse = Services.Script.TriggerResponse(ExperimentTriggers.TrySubmitExperiment);
            if (!kevinResponse.IsRunning())
            {
                SetInputState(false);
                Routine.Start(this, StartExperimentRoutine());
            }
        }

        private IEnumerator StartExperimentRoutine()
        {
            while(ExperimentServices.Actors.AnyActorsAreAnimating())
                yield return null;
            
            Hide();
            yield return 0.25f;
            Services.Events.Dispatch(ExperimentEvents.ExperimentBegin);
        }

        private void OnExperimentSubmitInitial()
        {
            m_ExperimentSetup = true;
        }

        private void OnExperimentBegin()
        {
            m_ExperimentRunning = true;
        }

        private void OnExperimentTeardown()
        {
            m_ExperimentSetup = false;
            m_ExperimentRunning = false;

            m_SelectionData.Reset();

            m_ActorsScreen.Refresh();
            m_BootScreen.Refresh();
            m_TankScreen.Refresh();
            m_EcoScreen.Refresh();
            m_HypothesisScreen.Refresh();
            m_BeginExperimentScreen.Refresh();
            m_InProgressScreen.Refresh();
        }

        #endregion // Callbacks

        #region Subscreens

        private void SetSubscreen(ExperimentSetupSubscreen inSubscreen, bool inbBack = false)
        {
            m_SwitchSubscreenRoutine.Replace(this, SwitchSubscreenRoutine(inSubscreen, inbBack)).TryManuallyUpdate(0);
        }

        private IEnumerator SwitchSubscreenRoutine(ExperimentSetupSubscreen inSubscreen, bool inbBack)
        {
            if (m_CurrentSubscreen != inSubscreen)
            {
                ExperimentSetupSubscreen oldSub = m_CurrentSubscreen;
                m_CurrentSubscreen = inSubscreen;
                if (oldSub)
                {
                    oldSub.Hide();
                    if (m_CurrentSubscreen)
                    {
                        yield return 0.2f;
                    }
                }

                if (m_CurrentSubscreen)
                {
                    m_CurrentSubscreen.Show();
                    Services.Audio.PostEvent(inbBack ? "tablet_ui_back" : "tablet_ui_advance");
                }
            }
        }

        #endregion // Subscreens
    }
}