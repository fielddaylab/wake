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
using BeauUtil.Debugger;
using Leaf.Runtime;

namespace Aqua
{
    public class DialogPanel : BasePanel
    {
        public delegate void UpdatePortraitDelegate(StringHash32 characterId, StringHash32 poseId);

        private const float SkipMultiplier = 1f / 1024;
        private const float DefaultMultiplier = 0.55f;

        public static UpdatePortraitDelegate UpdatePortrait;

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
            public ScriptCharacterDef TargetDef;
            public StringHash32 PortraitId;

            public bool Silent;
            public bool AutoContinue;
            public bool SkipPressed;
            public bool Sticky;

            public bool IsCutsceneSkip;

            public void ResetFull()
            {
                SpeakerName = string.Empty;
                Speed = 1;
                VisibleText = null;
                TypeSFX = null;
                IsCutsceneSkip = false;
                Sticky = false;
                TargetId = StringHash32.Null;
                PortraitId = StringHash32.Null;
                TargetDef = null;
                Silent = false;

                ResetTemp();
            }

            public void ResetTemp()
            {
                SkipPressed = false;
                AutoContinue = false;
                IsCutsceneSkip = false;
            }
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private string m_StyleId = null;
        
        [Header("Behavior")]

        [SerializeField] private SerializedHash32 m_DefaultTypeSFX = "text_type";
        [SerializeField] private LineEndBehavior m_EndBehavior = LineEndBehavior.WaitForInput;
        [SerializeField] private float m_NonAutoWaitTimer = 2;
        [SerializeField] private float m_StickyWaitTimer = 2;
        [SerializeField] private float m_StickyVerticalOffset = 0;
        [SerializeField] private bool m_UseShortName = false;

        [Header("Speaker")]
        
        [SerializeField] private RectTransform m_SpeakerContainer = null;
        [SerializeField] private TMP_Text m_SpeakerLabel = null;
        [SerializeField] private Graphic m_SpeakerLabelBackground = null;
        [SerializeField] private RectTransform m_SpeakerPortraitGroup = null;
        [SerializeField] private Image m_SpeakerPortrait = null;
        [SerializeField] private Graphic m_SpeakerPortraitBackground = null;

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
        [SerializeField] private float m_OptionsInset = 0;

        #endregion // Inspector

        [NonSerialized] private RectTransform m_SelfTransform;
        [NonSerialized] private ColorPalette4 m_DefaultNamePalette;
        [NonSerialized] private ColorPalette4 m_DefaultTextPalette;

        [NonSerialized] private TypingState m_CurrentState;
        [NonSerialized] private Routine m_BoxAnim;
        [NonSerialized] private Routine m_FadeAnim;
        [NonSerialized] private Routine m_StickyMoveAnim;
        [NonSerialized] private bool m_VisibleStickyState;
        [NonSerialized] private TagStringEventHandler m_EventHandler;

        [NonSerialized] private BaseInputLayer m_Input;
        [NonSerialized] private Routine m_RebuildRoutine;
        [NonSerialized] private float m_OriginalX;
        [NonSerialized] private float m_SelfOriginalY;
        [NonSerialized] private ScriptThreadHandle m_CurrentThread;

        [NonSerialized] private TagString m_TempTagString;

        public StringHash32 StyleId() { return m_StyleId; }

        #region BasePanel

        protected override void Awake()
        {
            base.Awake();

            this.CacheComponent(ref m_SelfTransform);
            
            if (m_SpeakerLabel)
                m_DefaultNamePalette.Content = m_SpeakerLabel.color;

            if (m_SpeakerLabelBackground)
                m_DefaultNamePalette.Background = m_SpeakerLabelBackground.color;

            m_DefaultTextPalette.Content = m_TextDisplay.color;

            m_OriginalX = Root.anchoredPosition.x;
            m_SelfOriginalY = m_SelfTransform.anchoredPosition.y;

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
            if (!Services.Valid)
                return;
            
            m_CurrentState.ResetFull();
            m_CurrentThread = default;

            ResetSpeaker();

            m_TextDisplay.SetText(string.Empty);

            Root.SetAnchorPos(m_OriginalX, Axis.X);
            UpdateStickyState();
            m_StickyMoveAnim.Stop();
            
            if (m_ButtonContainer)
                m_ButtonContainer.gameObject.SetActive(false);

            if (m_OptionContainer)
                m_OptionContainer.gameObject.SetActive(false);

            if (m_SpeakerPortraitGroup)
                m_SpeakerPortraitGroup.gameObject.SetActive(false);
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
                m_EventHandler.Register(ScriptEvents.Dialog.DoNotClose, () => {
                    m_CurrentState.Sticky = true;
                    UpdateStickyState();
                });
                m_EventHandler.Register(ScriptEvents.Dialog.SetTypeSFX, (e, o) => m_CurrentState.TypeSFX = e.Argument0.AsStringHash());
                m_EventHandler.Register(ScriptEvents.Dialog.SetVoiceType, SetVoiceType);
                m_EventHandler.Register(ScriptEvents.Dialog.Speaker, (e, o) => SetSpeakerName(e.StringArgument));
                m_EventHandler.Register(ScriptEvents.Dialog.Speed, (e, o) => {
                    m_CurrentState.Speed = e.IsClosing ? 1 : e.Argument0.AsFloat();
                    return Routine.WaitSeconds(0.15f);
                });
                m_EventHandler.Register(LeafUtils.Events.Character, (e, o) => SetTarget(e.Argument0.AsStringHash(), e.Argument1.AsStringHash(), false));
                m_EventHandler.Register(LeafUtils.Events.Pose, (e, o) => SetPortrait(e.Argument0.AsStringHash(), false));
                m_EventHandler.Register(LeafUtils.Events.Wait, (e, o) => Wait(e.GetFloat()));
            }

            return m_EventHandler;
        }

        private void SetVoiceType(TagEventData inEventData, object inContext)
        {
            StringHash32 voiceType = inEventData.Argument0.AsStringHash();
            if (voiceType == "silent")
            {
                m_CurrentState.Silent = true;
            }
            else
            {
                m_CurrentState.Silent = false;
            }
        }

        #endregion // Event Handlers

        #region Speaker

        private void ResetSpeaker()
        {
            SetTarget(StringHash32.Null, StringHash32.Null, true);
        }

        private bool SetSpeakerName(StringSlice inSpeaker)
        {
            if (IsShowing() && m_CurrentState.SpeakerName == inSpeaker)
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

            if (!Services.Valid)
                return false;

            m_CurrentState.TargetId = inTargetId;
            ScriptCharacterDef actorDef = m_CurrentState.TargetDef = Assets.Character(inTargetId);

            m_CurrentState.TypeSFX = actorDef.DefaultTypeSfx();

            if (actorDef.HasFlags(ScriptActorTypeFlags.IsPlayer))
            {
                SetSpeakerName(Save.Name);
            }
            else
            {
                TextId nameId = m_UseShortName ? actorDef.ShortNameId() : actorDef.NameId();
                if (nameId.IsEmpty)
                    SetSpeakerName(StringSlice.Empty);
                else
                    SetSpeakerName(Loc.Find(nameId));
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
            
            if (m_CurrentThread.IsRunning()) {
                m_CurrentThread.TapCharacter(m_CurrentState.TargetId);
                UpdatePortrait?.Invoke(m_CurrentState.TargetId, m_CurrentState.PortraitId);
            }

            if (portraitSprite)
            {
                if (m_SpeakerPortraitGroup)
                {
                    m_SpeakerPortrait.sprite = portraitSprite;
                    m_SpeakerPortraitGroup.gameObject.SetActive(true);
                }
            }
            else
            {
                if (m_SpeakerPortraitGroup)
                {
                    m_SpeakerPortraitGroup.gameObject.SetActive(false);
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
            if (m_SpeakerPortraitBackground)
                m_SpeakerPortraitBackground.color = inPalette.Background;
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
        /// Sets the current thread.
        /// </summary>
        public void AssignThread(ScriptThreadHandle inHandle)
        {
            if (Ref.Replace(ref m_CurrentThread, inHandle))
            {
                m_CurrentState.ResetFull();
                UpdateStickyState();
            }
        }

        /// <summary>
        /// Clears the current thread.
        /// </summary>
        public void ClearThread(ScriptThreadHandle inHandle)
        {
            Ref.CompareExchange(ref m_CurrentThread, inHandle, default);
        }

        /// <summary>
        /// Preps for a line of dialog.
        /// </summary>
        public TagStringEventHandler PrepLine(TagString inLine, TagStringEventHandler inParentHandler, bool inbForceHandle)
        {
            m_CurrentState.ResetTemp();

            bool bHasText = inLine.RichText.Length > 0;
            if (inbForceHandle || bHasText)
            {
                if (bHasText)
                {
                    m_TextDisplay.SetText(inLine.RichText);
                    m_CurrentState.VisibleText = inLine.VisibleText;
                    m_TextDisplay.maxVisibleCharacters = 0;

                    UpdateSkipHeld();

                    RebuildLayout();
                }

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
        public IEnumerator CompleteLine(LeafThreadState inThread)
        {
            if (IsShowing() && !string.IsNullOrEmpty(m_CurrentState.VisibleText))
            {
                if (m_CurrentState.Sticky)
                {
                    if (LeafRuntime.PredictEnd(inThread))
                        return Routine.Yield(Routine.Command.Pause);
                    return Routine.WaitSeconds(m_StickyWaitTimer);
                }

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

                if (Accessibility.TTSEnabled)
                {
                    float pitch = 1;
                    if (m_CurrentState.TargetDef != null)
                        pitch = m_CurrentState.TargetDef.TTSPitch();

                    Services.TTS.Text(m_CurrentState.VisibleText, pitch);
                }
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

                    if (m_CurrentState.IsCutsceneSkip || m_CurrentState.VisibleText == null)
                        yield break;
                }

                m_TextDisplay.maxVisibleCharacters++;

                if (m_CurrentState.IsCutsceneSkip || m_CurrentState.VisibleText == null)
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
            if (m_EndBehavior != LineEndBehavior.WaitForInput || m_CurrentState.Sticky)
                return;
            
            if (m_Input.Device.MousePressed(0))
            {
                m_CurrentState.SkipPressed = true;
            }
        }

        #endregion // Typing

        #region Options

        internal IEnumerator ShowOptions(ScriptNode inNode, LeafChoice inChoice, ILeafPlugin inPlugin, object inContext)
        {
            if (!m_OptionContainer)
                yield break;

            m_OptionContainer.gameObject.SetActive(true);
            int optionsToShow = inChoice.AvailableCount;
            if (optionsToShow > m_OptionButtons.Length)
            {
                optionsToShow = m_OptionButtons.Length;
                Log.Warn("[DialogPanel] Too many options - {0}, but only {1} buttons present", inChoice.AvailableCount, optionsToShow);
            }

            int buttonIdx = 0;
            string line;
            foreach(var option in inChoice.AvailableOptions())
            {
                DialogOptionButton button = m_OptionButtons[buttonIdx++];
                button.gameObject.SetActive(true);

                LeafUtils.TryLookupLine(inPlugin, option.LineCode, inNode, out line);
                Services.Script.ParseToTag(ref m_TempTagString, line, inContext);
                button.Populate(option.TargetId, m_TempTagString.RichText, inChoice, (option.Flags & LeafChoice.OptionFlags.IsSelector) != 0);
            }

            m_TempTagString?.Clear();

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

            float width = ((RectTransform) m_OptionSizer.transform).sizeDelta.x / 2 + m_OptionsInset;

            yield return Root.AnchorPosTo(m_OriginalX - width, 0.2f, Axis.X).Ease(Curve.Smooth);

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

            yield return Root.AnchorPosTo(m_OriginalX, 0.2f, Axis.X).Ease(Curve.Smooth);
        }

        #endregion // Options

        private float GetSkipMultiplier()
        {
            return m_CurrentState.SkipPressed ? SkipMultiplier : DefaultMultiplier;
        }

        private void PlayTypingSound()
        {
            if (m_CurrentState.Silent)
                return;
            
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

        private IEnumerator Wait(float inDuration)
        {
            while(inDuration > 0 && !m_CurrentState.SkipPressed)
            {
                inDuration -= Routine.DeltaTime;
                yield return null;
            }
        }

        private IEnumerator ScrollToVerticalOffset(float inOffset)
        {
            yield return m_SelfTransform.AnchorPosTo(m_SelfOriginalY + inOffset, 0.2f, Axis.Y).Ease(Curve.Smooth);
        }

        private void SetVerticalOffset(float inOffset)
        {
            m_StickyMoveAnim.Stop();
            m_SelfTransform.SetAnchorPos(m_SelfOriginalY + inOffset, Axis.Y);
        }

        private void UpdateStickyState()
        {
            if (m_VisibleStickyState == m_CurrentState.Sticky)
                return;

            m_VisibleStickyState = m_CurrentState.Sticky;
            if (m_RootTransform.gameObject.activeSelf && (IsShowing() || IsTransitioning()))
            {
                m_StickyMoveAnim.Replace(this, ScrollToVerticalOffset(m_VisibleStickyState ? m_StickyVerticalOffset : 0));
            }
            else
            {
                SetVerticalOffset(m_VisibleStickyState ? m_StickyVerticalOffset : 0);
            }
        }

        #endregion // Coroutines

        #region BasePanel

        protected override IEnumerator TransitionToShow()
        {
            if (!m_RootTransform.gameObject.activeSelf)
            {
                UpdateStickyState();
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
            yield return null;
            
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
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform) m_TextLayout.transform);
        }
    
        #endregion // BasePanel
    }
}