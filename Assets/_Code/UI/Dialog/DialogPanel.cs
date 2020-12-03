using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using TMPro;
using System.Collections;
using System;
using BeauUtil.Tags;
using Aqua.Scripting;
using BeauUtil;
using Leaf;

namespace Aqua
{
    public class DialogPanel : BasePanel
    {
        #region Types

        private enum LineEndBehavior
        {
            WaitForInput,
            WaitFixedDuration
        }

        private struct TypingState
        {
            public StringSlice SpeakerName;
            public float Speed;
            public string VisibleText;
            public StringHash32 TypeSFX;

            public bool AutoContinue;
            public float SkipHoldTimer;
            public bool SkipHeld;

            public void ResetFull()
            {
                SpeakerName = string.Empty;
                Speed = 1;
                VisibleText = null;
                TypeSFX = null;

                ResetTemp();
            }

            public void ResetTemp()
            {
                SkipHoldTimer = 0;
                SkipHeld = false;
                AutoContinue = false;
            }
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private string m_StyleId = null;
        
        [Header("Behavior")]

        [SerializeField] private SerializedHash32 m_DefaultTypeSFX = "text_type";
        [SerializeField] private float m_SpeedUpThreshold = 0.25f;
        [SerializeField] private LineEndBehavior m_EndBehavior = LineEndBehavior.WaitForInput;
        [SerializeField] private float m_NonAutoWaitTimer = 2;

        [Header("Speaker")]
        
        [SerializeField] private RectTransform m_SpeakerContainer = null;
        [SerializeField] private TMP_Text m_SpeakerLabel = null;
        [SerializeField] private Graphic m_SpeakerLabelBG = null;

        [Header("Text")]

        [SerializeField] private LayoutGroup m_TextLayout = null;
        [SerializeField] private CanvasGroup m_TextContainer = null;
        [SerializeField] private TMP_Text m_TextDisplay = null;

        [Header("Button")]

        [SerializeField] private RectTransform m_ButtonContainer = null;
        [SerializeField] private Button m_Button = null;
        [SerializeField] private CanvasGroup m_ButtonGroup = null;

        [Header("Options")]

        [SerializeField] private RectTransform m_OptionContainer = null;
        [SerializeField] private CanvasGroup m_OptionGroup = null;
        [SerializeField] private LayoutGroup m_OptionLayout = null;
        [SerializeField] private ContentSizeFitter m_OptionSizer = null;
        [SerializeField] private DialogOptionButton[] m_OptionButtons = null;

        #endregion // Inspector

        [NonSerialized] private TypingState m_CurrentState;
        [NonSerialized] private Routine m_BoxAnim;
        [NonSerialized] private Routine m_FadeAnim;
        [NonSerialized] private TagStringEventHandler m_EventHandler;

        [NonSerialized] private BaseInputLayer m_Input;
        [NonSerialized] private Routine m_RebuildRoutine;

        [NonSerialized] private TagString m_TempTagString = new TagString();

        public StringHash32 StyleId() { return m_StyleId; }

        #region BasePanel

        protected override void Start()
        {
            base.Start();

            m_Input = BaseInputLayer.Find(this);
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            m_CurrentState.ResetFull();

            if (m_SpeakerLabel)
                m_SpeakerLabel.SetText(string.Empty);
            
            m_TextDisplay.SetText(string.Empty);
            if (m_SpeakerContainer)
                m_SpeakerContainer.gameObject.SetActive(false);
            
            if (m_ButtonContainer)
                m_ButtonContainer.gameObject.SetActive(false);

            if (m_OptionContainer)
                m_OptionContainer.gameObject.SetActive(false);
        }

        #endregion // BasePanel

        #region Event Handlers

        private TagStringEventHandler GetHandler()
        {
            if (m_EventHandler == null)
            {
                m_EventHandler = new TagStringEventHandler();

                m_EventHandler.Register(ScriptEvents.Dialog.Auto, () => m_CurrentState.AutoContinue = true);
                m_EventHandler.Register(ScriptEvents.Dialog.Clear, () => m_TextDisplay.SetText(string.Empty));
                m_EventHandler.Register(ScriptEvents.Dialog.InputContinue, () => WaitForInput());
                m_EventHandler.Register(ScriptEvents.Dialog.SetTypeSFX, (e, o) => m_CurrentState.TypeSFX = e.Argument0.AsStringHash());
                m_EventHandler.Register(ScriptEvents.Dialog.Speaker, (e, o) => SetSpeaker(e.StringArgument));
                m_EventHandler.Register(ScriptEvents.Dialog.Speed, (e, o) => {
                    m_CurrentState.Speed = e.IsClosing ? 1 : e.Argument0.AsFloat();
                    return Routine.WaitSeconds(0.15f);
                });
                m_EventHandler.Register(ScriptEvents.Dialog.Target, (e, o) => SetTarget(e.Argument0.AsStringHash()));
                m_EventHandler.Register(ScriptEvents.Global.Wait, (e, o) => Routine.WaitSeconds(e.Argument0.AsFloat() * GetSkipMultiplier()));
            }

            return m_EventHandler;
        }

        
        private void SetTarget(StringHash32 inTarget)
        {
            if (inTarget.IsEmpty)
            {
                m_CurrentState.TypeSFX = null;
                SetSpeaker(null);
                return;
            }

            ScriptActorDefinition actorDef = Services.Script.Tweaks.ActorDef(inTarget);

            m_CurrentState.TypeSFX = actorDef.DefaultTypeSfx();
            if (actorDef.HasFlags(ScriptActorTypeFlags.IsPlayer))
            {
                SetSpeaker(Services.Data.CurrentCharacterName());
            }
            else
            {
                SetSpeaker(Services.Loc.Localize(actorDef.NameId()));
            }
        }

        private bool SetSpeaker(StringSlice inSpeaker)
        {
            if (m_CurrentState.SpeakerName == inSpeaker)
                return false;

            m_CurrentState.SpeakerName = inSpeaker;

            if (m_SpeakerContainer)
            {
                if (inSpeaker.IsEmpty)
                {
                    m_SpeakerContainer.gameObject.SetActive(false);
                    m_SpeakerLabel.SetText(string.Empty);
                }
                else
                {
                    m_SpeakerLabel.SetText(inSpeaker.ToString());
                    m_SpeakerContainer.gameObject.SetActive(true);
                }
            }

            return true;
        }

        #endregion // Event Handlers

        #region Scripting

        /// <summary>
        /// Preps for a line of dialog.
        /// </summary>
        public TagStringEventHandler PrepLine(TagString inLine, TagStringEventHandler inParentHandler)
        {
            m_CurrentState.ResetTemp();

            if (inLine.RichText.Length > 0)
            {
                m_TextDisplay.SetText(inLine.RichText);
                m_CurrentState.VisibleText = inLine.VisibleText;
                m_TextDisplay.maxVisibleCharacters = 0;

                UpdateSkipHeld();

                RebuildLayout();

                TagStringEventHandler handler = GetHandler();
                handler.Base = inParentHandler;
                return handler;
            }

            return inParentHandler;
        }

        /// <summary>
        /// Updates the dialog mid-line.
        /// </summary>
        public void UpdateInput()
        {
            UpdateSkipHeld();
        }

        /// <summary>
        /// Finishes the current line.
        /// </summary>
        public IEnumerator CompleteLine()
        {
            if (IsShowing() && !string.IsNullOrEmpty(m_CurrentState.VisibleText))
            {
                switch(m_EndBehavior)
                {
                    case LineEndBehavior.WaitForInput:
                        {
                            if (!m_CurrentState.AutoContinue)
                                return WaitForInput();
                            break;
                        }

                    case LineEndBehavior.WaitFixedDuration:
                        {
                            if (!m_CurrentState.AutoContinue)
                                return Routine.WaitSeconds(m_NonAutoWaitTimer);
                            break;
                        }
                }
            }

            return null;
        }

        /// <summary>
        /// Completes the current sequence.
        /// </summary>
        public void CompleteSequence()
        {
            if (IsShowing())
            {
                Hide();
            }
        }

        #endregion // Scripting

        #region Typing

        /// <summary>
        /// Types out a certain number of characters.
        /// </summary>
        public IEnumerator TypeLine(TagTextData inText)
        {
            if (m_TextDisplay.maxVisibleCharacters == 0)
            {
                if (IsShowing())
                    m_BoxAnim.Replace(this, Pulse());
                Show();

                RebuildLayout();
                while(m_RebuildRoutine)
                    yield return null;
            }

            float timeThisFrame = Routine.DeltaTime;
            float delay = -timeThisFrame;
            bool bPlayType = false;

            int charsToShow = (int) inText.VisibleCharacterCount;
            while(charsToShow > 0)
            {
                while(delay > 0)
                {
                    if (bPlayType)
                    {
                        bPlayType = false;
                        PlayTypingSound();
                    }
                    yield return null;
                    timeThisFrame = Routine.DeltaTime;
                    delay -= timeThisFrame;
                    UpdateInput();
                }

                m_TextDisplay.maxVisibleCharacters++;

                char shownChar = m_CurrentState.VisibleText[m_TextDisplay.maxVisibleCharacters - 1];
                switch(shownChar)
                {
                    case ' ':
                        delay += GetDelay(0.05f);
                        break;

                    case '.':
                    case '!':
                    case '?':
                        delay += GetDelay(0.2f);
                        break;
                    
                    case ',':
                        delay += GetDelay(0.08f);
                        break;

                    default:
                        if (char.IsLetterOrDigit(shownChar))
                        {
                            bPlayType = true;
                        }
                        delay += GetDelay(0.03f);
                        break;
                }

                --charsToShow;
            }

            if (bPlayType)
            {
                PlayTypingSound();
            }
        }

        private void UpdateSkipHeld()
        {
            if (m_EndBehavior != LineEndBehavior.WaitForInput)
                return;
            
            if (m_Input.Device.MouseDown(0))
            {
                m_CurrentState.SkipHoldTimer += Routine.DeltaTime;
                m_CurrentState.SkipHeld = m_CurrentState.SkipHoldTimer >= m_SpeedUpThreshold;
            }
            else
            {
                m_CurrentState.SkipHoldTimer = 0;
                m_CurrentState.SkipHeld = false;
            }
        }

        #endregion // Typing

        #region Options

        public IEnumerator ShowOptions(ScriptNode inNode, LeafChoice inChoice, ILeafContentResolver inResolver, object inContext)
        {
            if (!m_OptionContainer)
                yield break;

            m_OptionContainer.gameObject.SetActive(true);
            int optionsToShow = inChoice.AvailableCount;
            if (optionsToShow > m_OptionButtons.Length)
            {
                optionsToShow = m_OptionButtons.Length;
                Debug.LogWarningFormat("[DialogPanel] Too many options - {0}, but only {1} buttons present", inChoice.AvailableCount, optionsToShow);
            }

            int buttonIdx = 0;
            string line;
            foreach(var option in inChoice.AvailableOptions())
            {
                DialogOptionButton button = m_OptionButtons[buttonIdx++];
                button.gameObject.SetActive(true);

                inResolver.TryGetLine(option.LineCode, inNode, out line);
                Services.Script.ParseToTag(ref m_TempTagString, line, inContext);
                button.Populate(option.TargetId, m_TempTagString.RichText, inChoice);
            }

            m_TempTagString.Clear();

            for(int i = optionsToShow; i < m_OptionButtons.Length; ++i)
            {
                m_OptionButtons[i].gameObject.SetActive(false);
            }

            m_OptionLayout.enabled = true;
            m_OptionSizer.enabled = true;
            m_OptionLayout.ForceRebuild();
            yield return null;
            
            m_OptionSizer.enabled = false;
            m_OptionLayout.enabled = false;

            for(int i = 0; i < optionsToShow; ++i)
            {
                m_OptionButtons[i].Prep();
            }

            m_OptionGroup.blocksRaycasts = false;
            yield return Routine.ForParallel(
                0, optionsToShow,
                (i) => m_OptionButtons[i].AnimateOn(i * 0.02f)
            );
            m_OptionGroup.blocksRaycasts = true;

            while(!inChoice.HasChosen())
                yield return null;

            m_OptionGroup.blocksRaycasts = false;

            yield return Routine.ForParallel(
                0, optionsToShow,
                (i) => m_OptionButtons[i].AnimateOff(i * 0.02f)
            );

            m_OptionContainer.gameObject.SetActive(false);
        }

        #endregion // Options

        private float GetSkipMultiplier()
        {
            return m_CurrentState.SkipHeld ? 0.25f : 1;
        }

        private void PlayTypingSound()
        {
            StringHash32 typeSfx = m_CurrentState.TypeSFX.IsEmpty ? m_DefaultTypeSFX.Hash() : m_CurrentState.TypeSFX;
            Services.Audio.PostEvent(typeSfx);
        }

        private float GetDelay(float inBase)
        {
            if (m_CurrentState.Speed == 0)
                return 0;

            return inBase / m_CurrentState.Speed * GetSkipMultiplier();
        }

        #region Coroutines

        private IEnumerator Pulse()
        {
            yield return Routine.Inline(m_RootTransform.ScaleTo(1.03f, 0.04f).RevertOnCancel().Yoyo());
        }

        private IEnumerator WaitForInput()
        {
            m_ButtonContainer.gameObject.SetActive(true);
            yield return Routine.Race(
                m_Button == null ? null : m_Button.onClick.WaitForInvoke(),
                Routine.WaitCondition(() => m_Input.Device.MousePressed(0) || m_Input.Device.KeyPressed(KeyCode.Space))
            );
            m_ButtonContainer.gameObject.SetActive(false);
        }

        #endregion // Coroutines

        #region BasePanel

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

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            RebuildLayout();
        }

        private void RebuildLayout()
        {
            m_RebuildRoutine.Replace(this, RebuildDelayed());
        }

        private IEnumerator RebuildDelayed()
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) m_TextLayout.transform);
        }
    
        #endregion // BasePanel
    }
}