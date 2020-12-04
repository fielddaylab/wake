using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using TMPro;
using System.Collections;
using System;
using BeauUtil.Tags;
using BeauUtil;

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
        [SerializeField] private TMP_Text m_HeaderText = null;
        [SerializeField] private TMP_Text m_ContentsText = null;
        [SerializeField] private TMP_InputField m_Input = null;
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

            m_Input.onSubmit.AddListener((s) => OnTextSubmit());

            m_RaycastBlocker.OnInputDisabled.AddListener(OnInputDisabled);
        }

        private void OnDestroy()
        {
            m_RaycastBlocker.OnInputDisabled.RemoveListener(OnInputDisabled);
        }

        #endregion // Initialization

        #region Display

        public Future<StringHash32> Display(string inHeader, string inText)
        {
            return Present(inHeader, inText, DefaultOkay);
        }

        public Future<PopupInputResult> ForcedTextEntry(string inHeader, string inText)
        {
            return TextEntry(inHeader, inText, DefaultOkay);
        }

        public Future<StringHash32> AskYesNo(string inHeader, string inText)
        {
            return Present(inHeader, inText, DefaultYesNo);
        }

        public Future<PopupInputResult> CancelableTextEntry(string inHeader, string inText)
        {
            return TextEntry(inHeader, inText, DefaultSubmitCancel);
        }

        public Future<StringHash32> Present(string inHeader, string inText, params NamedOption[] inOptions)
        {
            Future<StringHash32> future = new Future<StringHash32>();
            m_DisplayRoutine.Replace(this, PresentMessageRoutine(future, inHeader, inText, inOptions));
            return future;
        }

        public Future<PopupInputResult> TextEntry(string inHeader, string inText, params NamedOption[] inOptions)
        {
            Future<PopupInputResult> future = new Future<PopupInputResult>();
            m_DisplayRoutine.Replace(this, PresentInputRoutine(future, inHeader, inText, inOptions));
            return future;
        }

        private void Configure(string inHeader, string inText, bool inbInput, NamedOption[] inOptions)
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

            m_Input.SetTextWithoutNotify(string.Empty);
            m_Input.gameObject.SetActive(inbInput);

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

        private IEnumerator PresentMessageRoutine(Future<StringHash32> ioFuture, string inHeader, string inText, NamedOption[] inOptions)
        {
            using(ioFuture)
            {
                Configure(inHeader, inText, false, inOptions);

                if (IsShowing())
                {
                    m_BoxAnim.Replace(this, BounceAnim());
                }
                else
                {
                    Show();
                }

                SetInputState(true);

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

                SetInputState(false);

                ioFuture.Complete(m_SelectedOption);
                m_SelectedOption = StringHash32.Null;

                yield return null;

                if (m_AutoCloseDelay > 0)
                    yield return m_AutoCloseDelay;

                Hide();
            }
        }

        private IEnumerator PresentInputRoutine(Future<PopupInputResult> ioFuture, string inHeader, string inText, NamedOption[] inOptions)
        {
            using(ioFuture)
            {
                Configure(inHeader, inText, true, inOptions);

                if (IsShowing())
                {
                    m_BoxAnim.Replace(this, BounceAnim());
                }
                else
                {
                    Show();
                }

                SetInputState(true);

                m_SelectedOption = StringHash32.Null;
                m_OptionWasSelected = false;
                while(!m_OptionWasSelected)
                {
                    for(int i = 0; i < m_OptionCount; ++i)
                    {
                        if (!inOptions[i].Id.IsEmpty)
                            m_Buttons[i].Button.interactable = !string.IsNullOrEmpty(m_Input.text);
                    }
                    yield return null;
                }

                SetInputState(false);

                ioFuture.Complete(new PopupInputResult(m_Input.text, m_SelectedOption));
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

        private void OnTextSubmit()
        {
            if (!string.IsNullOrEmpty(m_Input.text))
            {
                int nonEmptyIdx = -1;
                for(int i = 0; i < m_OptionCount; ++i)
                {
                    if (m_Buttons[i].OptionId.IsEmpty)
                        continue;

                    if (nonEmptyIdx >= 0)
                        return;

                    nonEmptyIdx = i;
                }

                if (nonEmptyIdx >= 0)
                {
                    OnButtonClicked(nonEmptyIdx);
                }
            }
        }

        private void OnInputDisabled()
        {
            m_Input.DeactivateInputField();
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