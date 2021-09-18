using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using TMPro;
using System.Collections;
using System;
using BeauUtil.Tags;
using BeauUtil;
using BeauUtil.Debugger;

namespace Aqua
{
    public class PopupPanel : BasePanel
    {
        static public readonly StringHash32 Option_Okay = "Okay";
        static public readonly StringHash32 Option_Submit = "Submit";
        static public readonly StringHash32 Option_Yes = "Yes";
        static public readonly StringHash32 Option_No = "";
        static public readonly StringHash32 Option_Cancel = "";

        static private readonly NamedOption[] DefaultOkay = new NamedOption[] { new NamedOption(Option_Okay, "Okay") };
        static private readonly NamedOption[] DefaultYesNo = new NamedOption[] { new NamedOption(Option_Yes, "Yes"), new NamedOption(Option_No, "No") };
        static private readonly NamedOption[] DefaultSubmitCancel = new NamedOption[] { new NamedOption(Option_Submit, "Submit"), new NamedOption(Option_Cancel, "Cancel") };

        [Serializable]
        private struct ButtonConfig
        {
            public Transform Root;
            public TMP_Text Text;
            public Button Button;

            [NonSerialized] public StringHash32 OptionId;
        }

        #region Inspector

        [Header("Canvas")]
        [SerializeField] private InputRaycasterLayer m_RaycastBlocker = null;

        [Header("Contents")]
        [SerializeField] private LayoutGroup m_Layout = null;
        [SerializeField] private LocText m_HeaderText = null;
        [SerializeField] private Image m_ImageDisplay = null;
        [SerializeField] private LocText m_ContentsText = null;
        [SerializeField] private FactPools m_FactPools = null;
        [SerializeField] private Transform m_FactPoolTransform = null;
        [SerializeField] private ButtonConfig[] m_Buttons = null;
        [SerializeField] private float m_AutoCloseDelay = 0.01f;

        #endregion // Inspector

        [NonSerialized] private Routine m_DisplayRoutine;
        [NonSerialized] private Routine m_BoxAnim;

        [NonSerialized] private StringHash32 m_SelectedOption;
        [NonSerialized] private bool m_OptionWasSelected;
        [NonSerialized] private int m_OptionCount;

        #region Initialization

        protected override void Awake()
        {
            base.Awake();

            for(int i = 0; i < m_Buttons.Length; ++i)
            {
                int cachedIdx = i;
                m_Buttons[i].Button.onClick.AddListener(() => OnButtonClicked(cachedIdx));
            }
        }

        #endregion // Initialization

        #region Display

        public Future<StringHash32> Display(string inHeader, string inText, Sprite inImage = null)
        {
            return Present(inHeader, inText, inImage, DefaultOkay);
        }

        public Future<StringHash32> AskYesNo(string inHeader, string inText, Sprite inImage = null)
        {
            return Present(inHeader, inText, inImage, DefaultYesNo);
        }

        public Future<StringHash32> Present(string inHeader, string inText, Sprite inImage, params NamedOption[] inOptions)
        {
            Future<StringHash32> future = new Future<StringHash32>();
            m_DisplayRoutine.Replace(this, PresentMessageRoutine(future, inHeader, inText, inImage, inOptions));
            return future;
        }

        public Future<StringHash32> PresentFact(string inHeader, string inText, BFBase inFact, BFDiscoveredFlags inFlags)
        {
            Future<StringHash32> future = new Future<StringHash32>();
            m_DisplayRoutine.Replace(this, PresentFactRoutine(future, inHeader, inText, new BFBase[] { inFact }, new BFDiscoveredFlags[] { inFlags }, DefaultOkay)).TryManuallyUpdate(0);
            return future;
        }

        public Future<StringHash32> PresentFacts(string inHeader, string inText, ListSlice<BFBase> inFacts, ListSlice<BFDiscoveredFlags> inFlags)
        {
            Future<StringHash32> future = new Future<StringHash32>();
            m_DisplayRoutine.Replace(this, PresentFactRoutine(future, inHeader, inText, inFacts, inFlags, DefaultOkay)).TryManuallyUpdate(0);
            return future;
        }

        private void Configure(string inHeader, string inText, Sprite inImage, NamedOption[] inOptions)
        {
            if (!string.IsNullOrEmpty(inHeader))
            {
                m_HeaderText.SetText(inHeader);
                m_HeaderText.gameObject.SetActive(true);
            }
            else
            {
                m_HeaderText.gameObject.SetActive(false);
                m_HeaderText.SetText(string.Empty);
            }

            if (!string.IsNullOrEmpty(inText))
            {
                m_ContentsText.SetText(inText);
                m_ContentsText.gameObject.SetActive(true);
            }
            else
            {
                m_ContentsText.gameObject.SetActive(false);
                m_ContentsText.SetText(string.Empty);
            }

            if (inImage != null)
            {
                m_ImageDisplay.sprite = inImage;
                m_ImageDisplay.gameObject.SetActive(true);
            }
            else
            {
                m_ImageDisplay.gameObject.SetActive(false);
                m_ImageDisplay.sprite = null;
            }

            m_OptionCount = inOptions.Length;
            for(int i = 0; i < m_Buttons.Length; ++i)
            {
                ref ButtonConfig config = ref m_Buttons[i];

                if (i < m_OptionCount)
                {
                    NamedOption option = inOptions[i];
                    config.Text.SetText(option.Text);
                    config.OptionId = option.Id;
                    config.Root.gameObject.SetActive(true);
                }
                else
                {
                    config.OptionId = null;
                    config.Root.gameObject.SetActive(false);
                }
            }
        }

        private void ConfigureFacts(ListSlice<BFBase> inFacts, ListSlice<BFDiscoveredFlags> inFlags)
        {
            m_FactPools.FreeAll();

            if (inFacts.IsEmpty)
                return;

            Assert.True(inFacts.Length == inFlags.Length);

            for(int i = 0; i < inFacts.Length; i++)
            {
                m_FactPools.Alloc(inFacts[i], null, inFlags[i], m_FactPoolTransform);
            }
        }

        private IEnumerator PresentMessageRoutine(Future<StringHash32> ioFuture, string inHeader, string inText, Sprite inImage, NamedOption[] inOptions)
        {
            using(ioFuture)
            {
                Configure(inHeader, inText, inImage, inOptions);
                ConfigureFacts(default, default);

                if (IsShowing())
                {
                    m_BoxAnim.Replace(this, BounceAnim());
                }
                else
                {
                    Show();
                }

                SetInputState(true);
                Services.TTS.Text(inText);

                m_SelectedOption = StringHash32.Null;
                m_OptionWasSelected = false;
                while(!m_OptionWasSelected)
                {
                    if (inOptions.Length <= 1 && m_RaycastBlocker.Device.KeyPressed(KeyCode.Space))
                    {
                        m_OptionWasSelected = true;
                        if (inOptions.Length == 1)
                            m_SelectedOption = inOptions[0].Id;
                        break;
                    }
                    
                    yield return null;
                }

                Services.TTS.Cancel();
                SetInputState(false);

                ioFuture.Complete(m_SelectedOption);
                m_SelectedOption = StringHash32.Null;

                yield return null;

                if (m_AutoCloseDelay > 0)
                    yield return m_AutoCloseDelay;

                Hide();
            }
        }

        private IEnumerator PresentFactRoutine(Future<StringHash32> ioFuture, string inHeader, string inText, ListSlice<BFBase> inFact, ListSlice<BFDiscoveredFlags> inFlags, NamedOption[] inOptions)
        {
            using(ioFuture)
            {
                Configure(inHeader, inText, null, inOptions);
                ConfigureFacts(inFact, inFlags);

                if (IsShowing())
                {
                    m_BoxAnim.Replace(this, BounceAnim());
                }
                else
                {
                    Show();
                }

                SetInputState(true);
                Services.TTS.Text(inText);

                m_SelectedOption = StringHash32.Null;
                m_OptionWasSelected = false;
                while(!m_OptionWasSelected)
                {
                    if (inOptions.Length <= 1 && m_RaycastBlocker.Device.KeyPressed(KeyCode.Space))
                    {
                        m_OptionWasSelected = true;
                        if (inOptions.Length == 1)
                            m_SelectedOption = inOptions[0].Id;
                        break;
                    }
                    
                    yield return null;
                }

                Services.TTS.Cancel();
                SetInputState(false);

                ioFuture.Complete(m_SelectedOption);
                m_SelectedOption = StringHash32.Null;

                yield return null;

                if (m_AutoCloseDelay > 0)
                    yield return m_AutoCloseDelay;

                Hide();
            }
        }

        #endregion // Display

        #region Animation

        private IEnumerator BounceAnim()
        {
            yield return Routine.Inline(m_RootTransform.ScaleTo(1.03f, 0.04f).RevertOnCancel().Yoyo());
        }

        protected override IEnumerator TransitionToShow()
        {
            if (!m_RootTransform.gameObject.activeSelf)
            {
                m_RootGroup.alpha = 0;
                m_RootTransform.SetScale(0.5f);
                m_RootTransform.gameObject.SetActive(true);
            }

            m_Layout.ForceRebuild();

            yield return Routine.Combine(
                m_RootGroup.FadeTo(1, 0.2f),
                m_RootTransform.ScaleTo(1f, 0.2f).Ease(Curve.BackOut)
            );
        }

        protected override IEnumerator TransitionToHide()
        {
            yield return Routine.Combine(
                m_RootGroup.FadeTo(0, 0.15f),
                m_RootTransform.ScaleTo(0.5f, 0.15f).Ease(Curve.CubeIn)
            );

            m_RootTransform.gameObject.SetActive(false);
        }
    
        #endregion // Animation

        #region Callbacks

        private void OnButtonClicked(int inIndex)
        {
            m_SelectedOption = m_Buttons[inIndex].OptionId;
            m_OptionWasSelected = true;
        }

        #endregion // Callbacks

        #region BasePanel

        protected override void OnShow(bool inbInstant)
        {
            SetInputState(false);
            m_RaycastBlocker.Override = null;

            if (!WasShowing())
            {
                Services.Input?.PushPriority(m_RaycastBlocker);
            }
        }

        protected override void OnHide(bool inbInstant)
        {
            SetInputState(false);

            m_BoxAnim.Stop();
            m_DisplayRoutine.Stop();

            m_RaycastBlocker.Override = false;

            if (WasShowing())
            {
                Services.Input?.PopPriority(m_RaycastBlocker);
            }
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            m_FactPools.FreeAll();

            base.OnHideComplete(inbInstant);
        }

        #endregion // BasePanel
    }

    public struct PopupInputResult
    {
        public readonly string Input;
        public readonly StringHash32 Option;

        public PopupInputResult(string inInput, StringHash32 inOption)
        {
            Input = inInput;
            Option = inOption;
        }
    }
}