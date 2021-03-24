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

            public StringHash32 TargetId;
            public ScriptActorDef TargetDef;
            public StringHash32 PortraitId;

            public bool AutoContinue;
            public float SkipHoldTimer;
            public bool SkipHeld;

            public bool IsCutsceneSkip;

            public void ResetFull()
            {
                SpeakerName = string.Empty;
                Speed = 1;
                VisibleText = null;
                TypeSFX = null;
                IsCutsceneSkip = false;
                TargetId = StringHash32.Null;
                PortraitId = StringHash32.Null;
                TargetDef = null;

                ResetTemp();
            }

            public void ResetTemp()
            {
                SkipHoldTimer = 0;
                SkipHeld = false;
                AutoContinue = false;
                IsCutsceneSkip = false;
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
        [SerializeField] private Graphic m_SpeakerLabelBackground = null;
        [SerializeField] private Image m_SpeakerPortrait = null;

        [Header("Text")]

        [SerializeField, Required] private LayoutGroup m_TextLayout = null;
        [SerializeField, Required] private TMP_Text m_TextDisplay = null;
        [SerializeField] private Graphic m_TextBackground = null;

        [Header("Button")]

        [SerializeField] private RectTransform m_ButtonContainer = null;
        [SerializeField] private Button m_Button = null;

        [Header("Options")]

        [SerializeField] private RectTransform m_OptionContainer = null;
        [SerializeField] private CanvasGroup m_OptionGroup = null;
        [SerializeField] private LayoutGroup m_OptionLayout = null;
        [SerializeField] private ContentSizeFitter m_OptionSizer = null;
        [SerializeField] private DialogOptionButton[] m_OptionButtons = null;

        #endregion // Inspector

        [NonSerialized] private ColorPalette4 m_DefaultNamePalette;
        [NonSerialized] private ColorPalette4 m_DefaultTextPalette;

        [NonSerialized] private TypingState m_CurrentState;
        [NonSerialized] private Routine m_BoxAnim;
        [NonSerialized] private Routine m_FadeAnim;
        [NonSerialized] private TagStringEventHandler m_EventHandler;

        [NonSerialized] private BaseInputLayer m_Input;
        [NonSerialized] private Routine m_RebuildRoutine;

        [NonSerialized] private TagString m_TempTagString = new TagString();

        public StringHash32 StyleId() { return m_StyleId; }

        #region BasePanel

        protected override void Awake()
        {
            base.Awake();
            
            if (m_SpeakerLabel)
                m_DefaultNamePalette.Content = m_SpeakerLabel.color;

            if (m_SpeakerLabelBackground)
                m_DefaultNamePalette.Background = m_SpeakerLabelBackground.color;

            m_DefaultTextPalette.Content = m_TextDisplay.color;

            if (m_TextBackground)
                m_DefaultTextPalette.Background = m_TextBackground.color;
        }

        protected override void Start()
        {
            base.Start();

            m_Input = BaseInputLayer.Find(this);
            m_CurrentState.ResetFull();
            ResetSpeaker();
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            m_CurrentState.ResetFull();

            ResetSpeaker();

            m_TextDisplay.SetText(string.Empty);
            
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
                m_EventHandler.Register(ScriptEvents.Dialog.Speaker, (e, o) => SetSpeakerName(e.StringArgument));
                m_EventHandler.Register(ScriptEvents.Dialog.Speed, (e, o) => {
                    m_CurrentState.Speed = e.IsClosing ? 1 : e.Argument0.AsFloat();
                    return Routine.WaitSeconds(0.15f);
                });
                m_EventHandler.Register(ScriptEvents.Dialog.Target, (e, o) => SetTarget(e.Argument0.AsStringHash(), e.Argument1.AsStringHash(), false));
                m_EventHandler.Register(ScriptEvents.Dialog.Portrait, (e, o) => SetPortrait(e.Argument0.AsStringHash(), false));
                m_EventHandler.Register(ScriptEvents.Global.Wait, (e, o) => Routine.WaitSeconds(e.Argument0.AsFloat() * GetSkipMultiplier()));
            }

            return m_EventHandler;
        }

        #endregion // Event Handlers

        #region Speaker

        private void ResetSpeaker()
        {
            SetTarget(StringHash32.Null, StringHash32.Null, true);
        }

        private bool SetSpeakerName(StringSlice inSpeaker)
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

        private bool SetTarget(StringHash32 inTargetId, StringHash32 inPortraitId, bool inbForce)
        {
            if (!inbForce && m_CurrentState.TargetId == inTargetId)
                return SetPortrait(inPortraitId, false);

            m_CurrentState.TargetId = inTargetId;
            ScriptActorDef actorDef = m_CurrentState.TargetDef = Services.Assets.Characters.Get(inTargetId);

            m_CurrentState.TypeSFX = actorDef.DefaultTypeSfx();

            if (actorDef.HasFlags(ScriptActorTypeFlags.IsPlayer))
            {
                SetSpeakerName(Services.Data.CurrentCharacterName());
            }
            else
            {
                if (actorDef.NameId().IsEmpty)
                    SetSpeakerName(StringSlice.Empty);
                else
                    SetSpeakerName(Services.Loc.Localize(actorDef.NameId()));
            }

            ColorPalette4? nameOverride = actorDef.NamePaletteOverride();
            ColorPalette4? textOverride = actorDef.TextPaletteOverride();

            AssignNamePalette(nameOverride ?? m_DefaultNamePalette);
            AssignTextPalette(textOverride ?? m_DefaultTextPalette);

            SetPortrait(inPortraitId, true);
            return true;
        }

        private bool SetPortrait(StringHash32 inPortraitId, bool inbForce)
        {
            if (!inbForce && m_CurrentState.PortraitId == inPortraitId)
                return false;
            
            Sprite portraitSprite = m_CurrentState.TargetDef.Portrait(inPortraitId);
            m_CurrentState.PortraitId = inPortraitId;
            if (portraitSprite)
            {
                if (m_SpeakerPortrait)
                {
                    m_SpeakerPortrait.sprite = portraitSprite;
                    m_SpeakerPortrait.gameObject.SetActive(true);
                }
            }
            else
            {
                if (m_SpeakerPortrait)
                {
                    m_SpeakerPortrait.gameObject.SetActive(false);
                    m_SpeakerPortrait.sprite = null;
                }
            }

            return true;
        }

        private void AssignNamePalette(in ColorPalette4 inPalette)
        {
            if (m_SpeakerLabel)
                m_SpeakerLabel.color = inPalette.Content;
            if (m_SpeakerLabelBackground)
                m_SpeakerLabelBackground.color = inPalette.Background;
        }

        private void AssignTextPalette(in ColorPalette4 inPalette)
        {
            m_TextDisplay.color = inPalette.Content;
            if (m_TextBackground)
                m_TextBackground.color = inPalette.Background;
        }

        #endregion // Speaker

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

        /// <summary>
        /// Skips all typing.
        /// </summary>
        public void Skip()
        {
            m_CurrentState.IsCutsceneSkip = true;
        }

        #endregion // Scripting

        #region Typing

        /// <summary>
        /// Types out a certain number of characters.
        /// </summary>
        public IEnumerator TypeLine(TagTextData inText)
        {
            if (m_CurrentState.IsCutsceneSkip)
                yield break;

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

                    if (m_CurrentState.IsCutsceneSkip)
                        yield break;
                }

                m_TextDisplay.maxVisibleCharacters++;

                if (m_CurrentState.IsCutsceneSkip)
                    yield break;

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

        internal IEnumerator ShowOptions(ScriptNode inNode, LeafChoice inChoice, ILeafContentResolver inResolver, object inContext)
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
            if (!m_ButtonContainer)
                yield break;
            
            m_ButtonContainer.gameObject.SetActive(true);
            yield return Routine.Race(
                m_Button == null ? null : m_Button.onClick.WaitForInvoke(),
                Routine.WaitCondition(() => m_CurrentState.IsCutsceneSkip || m_Input.Device.MousePressed(0) || m_Input.Device.KeyPressed(KeyCode.Space))
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