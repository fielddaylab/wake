using System;
using UnityEngine;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauRoutine.Extensions;
using System.Collections.Generic;
using UnityEngine.UI;
using Aqua;
using ProtoAqua;

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
        [SerializeField] private ExperimentSetupSubscreenWaterProp m_PropertyScreen = null;
        [SerializeField] private ExperimentSetupSubscreenCategory m_CategoryScreen = null;
        [SerializeField] private ExperimentSetupSubscreenSlider m_SliderScreen = null;

        #endregion // Inspector

        #region Local Data

        [NonSerialized] private AudioHandle m_Hum;
        [NonSerialized] private ExperimentSetupSubscreen m_CurrentSubscreen;
        [NonSerialized] private Routine m_SwitchSubscreenRoutine;

        [NonSerialized] private SubscreenDirectory m_SubDirectory;

        [NonSerialized] private ExperimentSetupData m_SelectionData;
        [NonSerialized] private bool m_ExperimentSetup = false;
        [NonSerialized] private bool m_ExperimentRunning = false;

        [NonSerialized] private TankType m_CurrentExp;

        [NonSerialized] private bool m_ExperimentFinished = false;

        #endregion // Local Data

        #region Core
        protected override void Awake()
        {
            m_CurrentExp = TankType.None;
            m_SubDirectory = new SubscreenDirectory(
                null, m_ActorsScreen, m_EcoScreen, m_BeginExperimentScreen, m_InProgressScreen,
                m_SummaryScreen, m_PropertyScreen, m_TankScreen, m_BootScreen, m_CategoryScreen, m_SliderScreen);

            m_CloseButton.onClick.AddListener(() => CancelExperiment());

            Services.Events.Register(ExperimentEvents.SetupPanelOn, () => Show(), this)
                .Register(ExperimentEvents.SetupInitialSubmit, OnExperimentSubmitInitial, this)
                .Register(ExperimentEvents.ExperimentTeardown, OnExperimentTeardown, this)
                .Register(ExperimentEvents.ExperimentBegin, OnExperimentBegin, this);

            SetBaseSubscreenChain();

            m_SelectionData = new ExperimentSetupData();
            SetData(m_SelectionData);
        }

        private void SetData(ExperimentSetupData selectData)
        {
            foreach (var screen in m_SubDirectory.AllSubscreens())
            {
                if (screen != null)
                {
                    screen.SetData(selectData);
                }
            }
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        #endregion // Core

        #region BasePanel

        protected override void OnShow(bool inbInstant)
        {
            m_Hum = Services.Audio.PostEvent("tablet_hum").SetVolume(0).SetVolume(1, 0.5f);
            Services.Data.SetVariable(ExperimentVars.SetupPanelOn, true);
        }

        protected override void OnHide(bool inbInstant)
        {
            if (WasShowing() && Services.Events)
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
            if (m_ExperimentRunning) {
                return m_InProgressScreen;
            }
            if(!m_ExperimentFinished) {
                if(m_CurrentExp == TankType.Measurement || m_CurrentExp == TankType.Stressor) return m_SummaryScreen;
            }
            return m_BootScreen;
        }

        #endregion // BasePanel

        #region Callbacks

        private void OnExperimentSubmit()
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
            using (var tempFader = Services.UI.ScreenFaders.AllocFader())
            {
                Services.UI.ShowLetterbox();
                yield return tempFader.Object.Show(Color.black, 0.5f);
                Services.Events.Dispatch(ExperimentEvents.SetupInitialSubmit, m_SelectionData);
                m_ExperimentSetup = true;
                yield return 0.2f;
                Services.UI.HideLetterbox();
                SetSubscreen(m_SubDirectory.GetNext(m_SubDirectory.GetEnum(m_CurrentSubscreen)));
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
                m_CurrentExp = TankType.None;
                SetInputState(false);
                Routine.Start(this, ExitExperimentRoutine());
            }
            else
            {
                if (m_CurrentSubscreen == m_SummaryScreen)
                {
                    Services.Script.TriggerResponse(ExperimentTriggers.ExperimentFinished);
                    m_ExperimentFinished = true;
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
                    .OnComplete((a) =>
                    {
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
            using (var tempFader = Services.UI.ScreenFaders.AllocFader())
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
                    Show();
                    SetSubscreen(m_SummaryScreen);
                }
                yield return tempFader.Object.Hide(0.5f, false);
            }

            Debug.Log("end experiment routine done");
        }

        private void StartExperiment()
        {
            m_ExperimentFinished = false;
            var kevinResponse = Services.Script.TriggerResponse(ExperimentTriggers.TrySubmitExperiment);
            if (!kevinResponse.IsRunning())
            {
                SetInputState(false);
                if (m_SelectionData.Tank == TankType.Foundational)
                {
                    Routine.Start(this, StartFoundationalTankExperimentRoutine());
                }

                if (m_SelectionData.Tank == TankType.Stressor || m_SelectionData.Tank == TankType.Measurement)
                {
                    Routine.Start(this, RunQuickExperimentRoutine());
                }

                // TODO: use ExperimentEvents?
                //Services.Events.Dispatch(GameEvents.BeginExperiment);
            }
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
            m_BootScreen.Refresh();

            var sequence = Services.Tweaks.Get<ExperimentSettings>().GetTank(m_SelectionData.Tank).Sequence;
            m_SelectionData.Reset();

            foreach (var sEnum in sequence)
            {
                if (!(sEnum.Equals(ExpSubscreen.None) || sEnum.Equals(ExpSubscreen.Boot)))
                {
                    m_SubDirectory.GetSubscreen(sEnum).Refresh();
                }
            }

            m_SubDirectory.Refresh();
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
                var oldsEnum = m_SubDirectory.GetEnum(oldSub);
                if(m_SubDirectory.InSequence(oldsEnum)) m_SubDirectory.SetVisited(m_SubDirectory.GetEnum(oldSub));
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
                    var currEnum = m_SubDirectory.GetEnum(m_CurrentSubscreen);
                    var isVisited = m_SubDirectory.InSequence(currEnum) ? m_SubDirectory.IsVisited(currEnum) : false;
                    m_CurrentSubscreen.Show();
                    Services.Audio.PostEvent(inbBack ? "tablet_ui_back" : "tablet_ui_advance");

                    if(inbBack || isVisited) {
                        Services.Events.Dispatch(ExperimentEvents.SubscreenBack, currEnum);
                    }
                }
            }
        }

        private void SetBaseSubscreenChain()
        {
            m_BootScreen.OnSelectContinue = () => SetSubscreen(m_TankScreen);
            m_BeginExperimentScreen.OnSelectStart = () => StartExperiment();
            m_TankScreen.OnSelectConstruct = () => OnExperimentSubmit();
            m_EcoScreen.OnSelectConstruct = () => OnExperimentSubmit();
            m_TankScreen.OnChange = () => SetSubSequence(m_SelectionData.Tank);
            m_InProgressScreen.OnSelectEnd = () => TryEndExperiment();
        }

        private void SetActions(ExpSubscreen[] sequence)
        {
            if (!m_SubDirectory.SeqEquals(sequence)) m_SubDirectory.SetSequence(sequence);
            foreach (var sub in sequence)
            {
                switch (sub)
                {
                    case ExpSubscreen.Tank:
                        if (m_SubDirectory.HasNext(sub)) m_TankScreen.OnSelectContinue = () => SetSubscreen(m_SubDirectory.GetNext(sub));
                        break;
                    case ExpSubscreen.Actor:
                        if (m_SubDirectory.HasNext(sub)) m_ActorsScreen.OnSelectContinue = () => SetSubscreen(m_SubDirectory.GetNext(sub));
                        break;
                    case ExpSubscreen.Begin:
                        if (m_SubDirectory.HasPrev(sub)) m_BeginExperimentScreen.OnSelectBack = () => SetSubscreen(m_SubDirectory.GetPrevious(sub), true);
                        break;
                    case ExpSubscreen.Ecosystem:
                        if (m_SubDirectory.HasPrevNext(sub))
                        {
                            m_EcoScreen.OnSelectContinue = () => SetSubscreen(m_SubDirectory.GetNext(sub));
                            m_EcoScreen.OnSelectBack = () => SetSubscreen(m_SubDirectory.GetPrevious(sub), true);
                        }
                        break;
                    case ExpSubscreen.Property:
                        if (m_SubDirectory.HasPrevNext(sub))
                        {
                            m_PropertyScreen.OnSelectContinue = () => SetSubscreen(m_SubDirectory.GetNext(sub));
                            m_PropertyScreen.OnSelectBack = () => SetSubscreen(m_SubDirectory.GetPrevious(sub), true);
                        }
                        break;
                    case ExpSubscreen.Category:
                        if(m_SubDirectory.HasPrev(sub)) m_CategoryScreen.OnSelectBack = () => SetSubscreen(m_SubDirectory.GetPrevious(sub), true);
                        m_CategoryScreen.OnSelectCritter = () => SetSubscreen(m_SubDirectory.GetNext(sub));
                        break;
                    case ExpSubscreen.Slider:
                        if(m_SubDirectory.HasNext(sub)) m_SliderScreen.OnSelectEnd = () => StartExperiment();
                        break;
                    default:
                        break;
                }
            }
        }

        public void SetSubSequence(TankType Tank)
        {
            Services.Events.Dispatch(ExperimentEvents.SetupTank, Tank);

            if (Tank != TankType.None)
            {
                m_CurrentExp = Tank;
                var sequence = Services.Tweaks.Get<ExperimentSettings>().GetTank(Tank).Sequence;
                if (m_SubDirectory.GetSequence() == null || !m_SubDirectory.SeqEquals(sequence))
                {
                    m_SubDirectory.SetSequence(sequence);
                }
                SetActions(sequence);
            }
        }

        #endregion // Subscreens

        #region Routines

        private IEnumerator StartFoundationalTankExperimentRoutine()
        {
            while (ExperimentServices.Actors.AnyActorsAreAnimating())
                yield return null;

            Hide();
            yield return 0.25f;
            Services.Events.Dispatch(ExperimentEvents.ExperimentBegin, m_CurrentExp);
        }

        private IEnumerator RunQuickExperimentRoutine() 
        {
            Services.UI.ShowLetterbox();
            yield return StartQuickExperimentRoutine();
            yield return ExitExperimentRoutine();
            Services.UI.HideLetterbox();
        }

        private IEnumerator StartQuickExperimentRoutine()
        {
            Hide();
            yield return 0.25f;
            Services.Events.Dispatch(ExperimentEvents.ExperimentBegin, m_CurrentExp);
            yield return 3f;
            Debug.Log("Start quick experiment routine done.");
        }

        #endregion //Routines
    }

        
}