using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using System.Collections;
using System;
using ProtoAudio;

namespace ProtoAqua
{
    public class DialogPanel : BasePanel
    {
        #region Inspector

        [Header("Speaker")]
        
        [SerializeField] private RectTransform m_SpeakerContainer = null;
        [SerializeField] private TMP_Text m_SpeakerLabel = null;
        [SerializeField] private Graphic m_SpeakerLabelBG = null;

        [Header("Text")]

        [SerializeField] private CanvasGroup m_TextContainer = null;
        [SerializeField] private TMP_Text m_TextDisplay = null;

        [Header("Button")]

        [SerializeField] private RectTransform m_ButtonContainer = null;
        [SerializeField] private Button m_Button = null;
        [SerializeField] private CanvasGroup m_ButtonGroup = null;

        [Header("DEBUG")]

        [SerializeField] private TextAsset m_DebugLines = null;
        [SerializeField] private CanvasGroup m_ScreenFader = null;

        #endregion // Inspector

        private struct TypingState
        {
            public string SpeakerName;
            public float Speed;
            public TagString Text;
            public string TypeSFX;

            public bool AutoContinue;
            public float SkipHoldTimer;
            public bool SkipHeld;

            public void ResetFull()
            {
                SpeakerName = string.Empty;
                Speed = 1;
                Text?.Clear();
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

        [NonSerialized] private TagStringParser m_Parser;
        [NonSerialized] private TypingState m_CurrentState;
        [NonSerialized] private Routine m_BoxAnim;
        [NonSerialized] private AudioHandle m_BGM;
        [NonSerialized] private Routine m_FadeAnim;

        [NonSerialized] private TagStringEventHandler m_EventHandler;

        protected override void Start()
        {
            base.Start();

            if (m_DebugLines != null)
            {
                Routine.Start(this, ProcessSequence(m_DebugLines.text));
            }
        }

        private void ResetState()
        {
            m_CurrentState.ResetFull();

            m_SpeakerLabel.SetText(string.Empty);
            m_TextDisplay.SetText(string.Empty);
            m_SpeakerContainer.gameObject.SetActive(false);
            m_ButtonContainer.gameObject.SetActive(false);
        }

        private TagStringParser GetParser()
        {
            if (m_Parser == null)
            {
                m_Parser = new TagStringParser();
                m_Parser.Delimiters = TagStringParser.CurlyBraceDelimiters;
                m_Parser.EventProcessor = DialogParser.ParserConfig;
                m_Parser.ReplaceProcessor = DialogParser.ParserConfig;
            }
            return m_Parser;
        }

        private TagStringEventHandler GetHandler()
        {
            if (m_EventHandler == null)
            {
                m_EventHandler = new TagStringEventHandler();

                m_EventHandler.Register(DialogParser.Event_Auto, () => m_CurrentState.AutoContinue = true);
                m_EventHandler.Register(DialogParser.Event_Clear, () => m_TextDisplay.SetText(string.Empty));
                m_EventHandler.Register(DialogParser.Event_Hide, () => Hide());
                m_EventHandler.Register(DialogParser.Event_InputContinue, () => WaitForInput());
                m_EventHandler.Register(DialogParser.Event_PitchBGM, (e, o) => {
                    float pitch, duration;
                    if (string.IsNullOrEmpty(e.StringArgument))
                    {
                        pitch = 1;
                        duration = 0.5f;
                    }
                    else
                    {
                        string[] split = e.StringArgument.Split(' ');
                        pitch = float.Parse(split[0]);
                        if (split.Length > 1)
                        {
                            duration = float.Parse(split[1]);
                        }
                        else
                        {
                            duration = 0.5f;
                        }
                    }

                    m_BGM.SetPitch(pitch, duration);
                });
                m_EventHandler.Register(DialogParser.Event_PlayBGM, (e, o) => {
                    bool bCrossFade = m_BGM.IsPlaying();
                    m_BGM.Stop(0.5f);

                    m_BGM = Services.Audio.PostEvent(e.StringArgument);
                    if (bCrossFade)
                    {
                        m_BGM.SetVolume(0).SetVolume(1, 0.5f);
                    }
                });
                m_EventHandler.Register(DialogParser.Event_PlaySound, (e, o) => Services.Audio.PostEvent(e.StringArgument));
                m_EventHandler.Register(DialogParser.Event_SetTypeSFX, (e, o) => m_CurrentState.TypeSFX = e.StringArgument);
                m_EventHandler.Register(DialogParser.Event_Show, () => Show());
                m_EventHandler.Register(DialogParser.Event_Speaker, (e, o) => SetSpeaker(e.StringArgument));
                m_EventHandler.Register(DialogParser.Event_Speed, (e, o) => {
                    m_CurrentState.Speed = e.IsClosing ? 1 : e.NumberArgument;
                    return Routine.WaitSeconds(0.15f);
                });
                m_EventHandler.Register(DialogParser.Event_StopBGM, (e, o) => {
                    m_BGM.Stop(e.NumberArgument);
                    m_BGM = AudioHandle.Null;
                });
                m_EventHandler.Register(DialogParser.Event_Target, (e, o) => SetTarget(e.StringArgument));
                m_EventHandler.Register(DialogParser.Event_Wait, (e, o) => Routine.WaitSeconds(e.NumberArgument * GetSkipMultiplier()));
            }

            return m_EventHandler;
        }

        private IEnumerator ProcessSequence(StringSlice inSequence)
        {
            ResetState();

            foreach(var line in inSequence.EnumeratedSplit(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                StringSlice trimmedLine = line.TrimStart();
                
                if (trimmedLine.IsEmpty || trimmedLine.StartsWith("#") || line.StartsWith("//"))
                    continue;

                yield return ProcessLine(trimmedLine);
            }

            yield return Hide();
        }

        private IEnumerator ProcessLine(StringSlice inSlice)
        {
            GetParser().Parse(inSlice, ref m_CurrentState.Text);

            Debug.LogFormat("[DialogPanel] Parse Results\n - Original Line: {0}\n - Rich Line: {1}\n - Visible Line: {2}\n - Node Count: {3}",
                inSlice, m_CurrentState.Text.RichText, m_CurrentState.Text.VisibleText, m_CurrentState.Text.Nodes.Length);

            m_CurrentState.ResetTemp();

            if (m_CurrentState.Text.RichText.Length > 0)
            {
                m_TextDisplay.SetText(m_CurrentState.Text.RichText);
                m_TextDisplay.maxVisibleCharacters = 0;
            }

            float timeThisFrame = Routine.DeltaTime;
            float delay = -timeThisFrame;
            bool bPlayType = false;

            bool bFirstText = true;

            UpdateSkipHeld();

            foreach(var node in m_CurrentState.Text.Nodes)
            {
                switch(node.Type)
                {
                    case TagString.NodeType.Text:
                        {
                            if (bFirstText)
                            {
                                if (!IsShowing())
                                    Show();
                                else
                                    m_BoxAnim.Replace(this, Pulse());
                                bFirstText = false;
                            }
                            int charsToShow = (int) node.Text.VisibleCharacterCount;
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
                                    UpdateSkipHeld();
                                }

                                m_TextDisplay.maxVisibleCharacters++;

                                char shownChar = m_CurrentState.Text.VisibleText[m_TextDisplay.maxVisibleCharacters - 1];
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
                            break;
                        }

                    case TagString.NodeType.Event:
                        {
                            IEnumerator handle;
                            if (GetHandler().TryEvaluate(node.Event, this, out handle) && handle != null)
                            {
                                if (bPlayType)
                                {
                                    bPlayType = false;
                                    PlayTypingSound();
                                }
                                yield return handle;
                                timeThisFrame = Routine.DeltaTime;
                                delay = -timeThisFrame;
                                UpdateSkipHeld();
                            }
                            break;
                        }
                }
            }
        
            if (bPlayType)
            {
                bPlayType = false;
                PlayTypingSound();
            }

            if (IsShowing() && !m_CurrentState.AutoContinue && !string.IsNullOrEmpty(m_CurrentState.Text.RichText))
            {
                yield return WaitForInput();
            }
        }

        private void UpdateSkipHeld()
        {
            if (Input.GetMouseButton(0))
            {
                m_CurrentState.SkipHoldTimer += Routine.DeltaTime;
                m_CurrentState.SkipHeld = m_CurrentState.SkipHoldTimer >= 0.25f;
            }
            else
            {
                m_CurrentState.SkipHoldTimer = 0;
                m_CurrentState.SkipHeld = false;
            }
        }

        private float GetSkipMultiplier()
        {
            return m_CurrentState.SkipHeld ? 0.25f : 1;
        }

        private void PlayTypingSound()
        {
            string typeSfx = string.IsNullOrEmpty(m_CurrentState.TypeSFX) ? "text_type" : m_CurrentState.TypeSFX;
            Services.Audio.PostEvent(typeSfx);
        }

        private float GetDelay(float inBase)
        {
            if (m_CurrentState.Speed == 0)
                return 0;

            return inBase / m_CurrentState.Speed * GetSkipMultiplier();
        }

        private void SetTarget(string inTarget)
        {
            m_CurrentState.TypeSFX = "text_type_" + inTarget;
            if (inTarget == "kevin")
            {
                SetSpeaker("Kevin, Your Science Familiar");
            }
            else if (inTarget == "player")
            {
                SetSpeaker(Environment.UserName);
            }
            else if (inTarget == "mechanic")
            {
                SetSpeaker("Jan, The Mechanic");
            }
            else if (inTarget == "radio")
            {
                SetSpeaker("Radio");
            }
            else
            {
                SetSpeaker("???");
            }
        }

        private bool SetSpeaker(string inSpeaker)
        {
            if (m_CurrentState.SpeakerName == inSpeaker)
                return false;

            m_CurrentState.SpeakerName = inSpeaker;

            if (string.IsNullOrEmpty(inSpeaker))
            {
                m_SpeakerContainer.gameObject.SetActive(false);
                m_SpeakerLabel.SetText(string.Empty);
            }
            else
            {
                m_SpeakerLabel.SetText(inSpeaker);
                m_SpeakerContainer.gameObject.SetActive(true);
            }

            return true;
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
                m_Button.onClick.WaitForInvoke(),
                Routine.WaitCondition(() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
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
    
        #endregion // BasePanel
    }
}