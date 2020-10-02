using System;
using UnityEngine;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using BeauRoutine.Extensions;
using UnityEngine.UI;

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

        #endregion // Inspector

        [NonSerialized] private AudioHandle m_Hum;
        [NonSerialized] private ExperimentSetupSubscreen m_CurrentSubscreen;
        [NonSerialized] private Routine m_SwitchSubscreenRoutine;

        [NonSerialized] private TankSelectionData m_SelectionData;

        protected override void Awake()
        {
            m_CloseButton.onClick.AddListener(() => Hide());
            Services.Events.Register(ExperimentEvents.SetupPanelOn, () => Show(), this);

            m_BootScreen.OnSelectContinue = () => SetSubscreen(m_TankScreen);
            m_TankScreen.OnSelectContinue = () => SetSubscreen(m_EcoScreen);
            m_EcoScreen.OnSelectBack = () => SetSubscreen(m_TankScreen, true);
            m_EcoScreen.OnSelectContinue = () => SetSubscreen(m_HypothesisScreen);
            m_HypothesisScreen.OnSelectBack = () => SetSubscreen(m_EcoScreen, true);
            m_HypothesisScreen.OnSelectContinue = () => OnHypothesisSubmit();

            m_SelectionData = new TankSelectionData();
            m_TankScreen.SetData(m_SelectionData);
            m_EcoScreen.SetData(m_SelectionData);
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
            SetSubscreen(m_BootScreen);
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

            SetSubscreen(m_BootScreen, false);
        }

        #endregion // BasePanel

        #region Callbacks

        private void OnHypothesisSubmit()
        {
            SetInputState(false);
            Routine.Start(this, LoadExperimentRoutine());
        }

        private IEnumerator LoadExperimentRoutine()
        {
            using(var tempFader = Services.UI.ScreenFaders.AllocFader())
            {
                Services.UI.ShowLetterbox();
                yield return tempFader.Object.Show(Color.black, 0.5f);
                InstantHide();
                Services.Events.Dispatch(ExperimentEvents.SetupInitialSubmit, m_SelectionData);
                yield return 0.2f;
                Services.UI.HideLetterbox();
                yield return tempFader.Object.Hide(0.5f, false);
            }
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
                        yield return 0.3f;
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