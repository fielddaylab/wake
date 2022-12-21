using System;
using System.Collections;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    public class PopupPanel : BasePanel {
        static public readonly StringHash32 Option_Okay = "Okay";
        static public readonly StringHash32 Option_Submit = "Submit";
        static public readonly StringHash32 Option_Yes = "Yes";
        static public readonly StringHash32 Option_No = "";
        static public readonly StringHash32 Option_Cancel = "";

        static public readonly NamedOption[] DefaultOkay = new NamedOption[] { new NamedOption(Option_Okay, "ui.popup.okay") };
        static public readonly NamedOption[] DefaultYesNo = new NamedOption[] { new NamedOption(Option_Yes, "ui.popup.yes"), new NamedOption(Option_No, "ui.popup.no") };
        static public readonly NamedOption[] DefaultAddToBestiary = new NamedOption[] { new NamedOption(Option_Okay, "ui.popup.addToBestiaryButton") };
        static public readonly NamedOption[] DefaultDismiss = new NamedOption[] { new NamedOption(Option_Okay, "ui.popup.dismiss") };

        #region Inspector

        [Header("Canvas")]
        [SerializeField] private InputRaycasterLayer m_RaycastBlocker = null;

        [Header("Contents")]
        [SerializeField] private PopupLayout m_Layout = null;
        [SerializeField] private float m_AutoCloseDelay = 0.01f;

        #endregion // Inspector

        [NonSerialized] private Routine m_DisplayRoutine;
        [NonSerialized] private Routine m_BoxAnim;

        #region Initialization

        protected override void Awake() {
            base.Awake();
            m_Layout.Initialize();
        }

        #endregion // Initialization

        #region Display

        public Future<StringHash32> Display(string inHeader, string inText, StreamedImageSet inImage = default, PopupFlags inPopupFlags = default) {
            return Present(inHeader, inText, inImage, inPopupFlags, DefaultOkay);
        }

        public Future<StringHash32> DisplayWithClose(string inHeader, string inText, StreamedImageSet inImage = default, PopupFlags inPopupFlags = default) {
            return Present(inHeader, inText, inImage, inPopupFlags | PopupFlags.ShowCloseButton, null);
        }

        public Future<StringHash32> AskYesNo(string inHeader, string inText, StreamedImageSet inImage = default, PopupFlags inPopupFlags = default) {
            return Present(inHeader, inText, inImage, inPopupFlags, DefaultYesNo);
        }

        public Future<StringHash32> Present(string inHeader, string inText, StreamedImageSet inImage, PopupFlags inPopupFlags, params NamedOption[] inOptions) {
            Future<StringHash32> future = new Future<StringHash32>();
            m_DisplayRoutine.Replace(this, PresentMessageRoutine(future, PopupLayout.TempContent(inHeader, inText, inImage, default, inOptions), inPopupFlags));
            return future;
        }

        public Future<StringHash32> Present(PopupContent inContent, PopupFlags inPopupFlags) {
            Future<StringHash32> future = new Future<StringHash32>();
            m_DisplayRoutine.Replace(this, PresentMessageRoutine(future, inContent, inPopupFlags));
            return future;
        }

        public Future<StringHash32> PresentFact(string inHeader, string inText, StreamedImageSet inImage, BFBase inFact, BFDiscoveredFlags inFlags, PopupFlags inPopupFlags = default, params NamedOption[] inOptions) {
            Future<StringHash32> future = new Future<StringHash32>();
            if (inOptions == null || inOptions.Length == 0) {
                inOptions = DefaultAddToBestiary;
            }
            m_DisplayRoutine.Replace(this, PresentMessageRoutine(future, PopupLayout.TempContent(inHeader, inText, inImage, TempFacts(inFact, inFlags), inOptions), inPopupFlags)).Tick();
            return future;
        }

        public Future<StringHash32> PresentFacts(string inHeader, string inText, StreamedImageSet inImage, PopupFacts inFacts, PopupFlags inPopupFlags = default, params NamedOption[] inOptions) {
            Future<StringHash32> future = new Future<StringHash32>();
            if (inOptions == null || inOptions.Length == 0) {
                inOptions = DefaultAddToBestiary;
            }
            m_DisplayRoutine.Replace(this, PresentMessageRoutine(future, PopupLayout.TempContent(inHeader, inText, inImage, inFacts, inOptions), inPopupFlags)).Tick();
            return future;
        }

        public Future<StringHash32> PresentFactDetails(BFDetails inDetails, BFBase inFact, BFDiscoveredFlags inFlags, PopupFlags inPopupFlags, params NamedOption[] inOptions) {
            Future<StringHash32> future = new Future<StringHash32>();
            m_DisplayRoutine.Replace(this, PresentMessageRoutine(future, PopupLayout.TempContent(inDetails.Header, inDetails.Description, inDetails.Image, TempFacts(inFact, inFlags), inOptions), inPopupFlags)).Tick();
            return future;
        }

        private PopupFacts TempFacts(BFBase fact, BFDiscoveredFlags flags) {
            return m_Layout.TempFacts(fact, flags);
        }

        private void ShowOrBounce() {
            if (IsShowing()) {
                m_BoxAnim.Replace(this, BounceAnim());
                m_Layout.PlayAnim();
            } else {
                Show();
            }
        }

        private IEnumerator PresentMessageRoutine(Future<StringHash32> ioFuture, PopupContent inContent, PopupFlags inPopupFlags) {
            using (ioFuture) {
                m_Layout.Configure(ref inContent, inPopupFlags);
                
                Services.Events.Queue(GameEvents.PopupOpened);

                ShowOrBounce();

                PopupLayout.AttemptTTS(ref inContent);

                if (inContent.Execute != null) {
                    SetInputState(false);
                    m_RootGroup.blocksRaycasts = false;
                    yield return inContent.Execute(this, m_Layout);
                    m_RootGroup.blocksRaycasts = true;
                }

                SetInputState(true);
                yield return m_Layout.WaitForInput(inContent, ioFuture);

                Services.TTS.Cancel();
                SetInputState(false);

                yield return null;

                if (m_AutoCloseDelay > 0)
                    yield return m_AutoCloseDelay;

                Hide();
            }
        }

        public bool IsDisplaying() {
            return m_DisplayRoutine;
        }

        #endregion // Display

        #region Animation

        private IEnumerator BounceAnim() {
            m_RootGroup.alpha = 0.5f;
            m_RootTransform.SetScale(0.5f);
            yield return Routine.Combine(
                m_RootTransform.ScaleTo(1, 0.2f).ForceOnCancel().Ease(Curve.BackOut),
                m_RootGroup.FadeTo(1, 0.2f)
            );
        }

        protected override IEnumerator TransitionToShow() {
            float durationMultiplier = 1;
            if (m_RootGroup.alpha > 0 && m_RootGroup.alpha < 1)
                durationMultiplier = 0.5f;

            m_RootGroup.alpha = durationMultiplier < 1 ? 0.5f : 0;
            m_RootTransform.SetScale(durationMultiplier < 1 ? 0.75f : 0.5f);
            m_RootTransform.gameObject.SetActive(true);
            
            m_Layout.PlayAnim(0.15f);

            yield return Routine.Combine(
                m_RootGroup.FadeTo(1, 0.2f * durationMultiplier),
                m_RootTransform.ScaleTo(1f, 0.2f * durationMultiplier).Ease(Curve.BackOut)
            );
        }

        protected override IEnumerator TransitionToHide() {
            yield return Routine.Combine(
                m_RootGroup.FadeTo(0, 0.15f),
                m_RootTransform.ScaleTo(0.5f, 0.15f).Ease(Curve.CubeIn)
            );

            m_RootTransform.gameObject.SetActive(false);
        }

        #endregion // Animation

        #region BasePanel

        protected override void OnShow(bool inbInstant) {
            SetInputState(false);
            m_RaycastBlocker.Override = null;

            if (!WasShowing()) {
                Services.Input?.PushPriority(m_RaycastBlocker);
                Services.Input?.PushFlags(m_RaycastBlocker);
            }
        }

        protected override void OnHide(bool inbInstant) {
            SetInputState(false);

            m_BoxAnim.Stop();
            m_DisplayRoutine.Stop();

            m_RaycastBlocker.Override = false;

            if (WasShowing()) {
                Services.Events?.Queue(GameEvents.PopupClosed);
            }

            if (WasShowing()) {
                Services.Input?.PopFlags(m_RaycastBlocker);
                Services.Input?.PopPriority(m_RaycastBlocker);
            }
        }

        protected override void OnHideComplete(bool inbInstant) {
            m_RootGroup.alpha = 0;

            base.OnHideComplete(inbInstant);
        }

        #endregion // BasePanel
    }
}