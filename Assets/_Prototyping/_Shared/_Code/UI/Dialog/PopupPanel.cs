using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using TMPro;
using System.Collections;
using System;
using BeauUtil.Tags;
using BeauUtil;

namespace ProtoAqua
{
    public class PopupPanel : BasePanel
    {
        static public readonly StringHash Option_Okay = "Okay";
        static public readonly StringHash Option_Yes = "Yes";
        static public readonly StringHash Option_No = "";

        static private readonly NamedOption[] DefaultOkay = new NamedOption[] { new NamedOption(Option_Okay, "Okay") };
        static private readonly NamedOption[] DefaultYesNo = new NamedOption[] { new NamedOption(Option_Yes, "Yes"), new NamedOption(Option_No, "No") };

        [Serializable]
        private struct ButtonConfig
        {
            public Transform Root;
            public TMP_Text Text;
            public Button Button;

            [NonSerialized] public StringHash OptionId;
        }

        #region Inspector

        [Header("Canvas")]
        [SerializeField] private InputRaycasterLayer m_RaycastBlocker = null;

        [Header("Contents")]
        [SerializeField] private TMP_Text m_HeaderText = null;
        [SerializeField] private TMP_Text m_ContentsText = null;
        [SerializeField] private ButtonConfig[] m_Buttons = null;
        [SerializeField] private float m_AutoCloseDelay = 0.01f;

        #endregion // Inspector

        [NonSerialized] private Routine m_DisplayRoutine;
        [NonSerialized] private Routine m_BoxAnim;

        [NonSerialized] private StringHash m_SelectedOption;
        [NonSerialized] private bool m_OptionWasSelected;

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

        public Future<StringHash> Display(string inHeader, string inText)
        {
            return Present(inHeader, inText, DefaultOkay);
        }

        public Future<StringHash> AskYesNo(string inHeader, string inText)
        {
            return Present(inHeader, inText, DefaultYesNo);
        }

        public Future<StringHash> Present(string inHeader, string inText, params NamedOption[] inOptions)
        {
            Future<StringHash> future = new Future<StringHash>();
            m_DisplayRoutine.Replace(this, PresentRoutine(future, inHeader, inText, inOptions));
            return future;
        }

        private void Configure(string inHeader, string inText, NamedOption[] inOptions)
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

            for(int i = 0; i < m_Buttons.Length; ++i)
            {
                ref ButtonConfig config = ref m_Buttons[i];

                if (i < inOptions.Length)
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

        private IEnumerator PresentRoutine(Future<StringHash> ioFuture, string inHeader, string inText, NamedOption[] inOptions)
        {
            using(ioFuture)
            {
                Configure(inHeader, inText, inOptions);

                if (IsShowing())
                {
                    m_BoxAnim.Replace(this, BounceAnim());
                }
                else
                {
                    Show();
                }

                SetInputState(true);

                m_SelectedOption = StringHash.Null;
                m_OptionWasSelected = false;
                while(!m_OptionWasSelected)
                {
                    if (inOptions.Length <= 1 && Input.GetKeyDown(KeyCode.Space))
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
                m_SelectedOption = StringHash.Null;

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
                Services.Input?.PopPriority();
            }
        }

        #endregion // BasePanel
    }
}