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
                m_Parser.EventProcessor = new EventParser();
                m_Parser.ReplaceProcessor = new ReplaceParser();
            }
            return m_Parser;
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
                            IEnumerator handle = HandleEvent(node.Event);
                            if (handle != null)
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

            if (IsShowing() && !m_CurrentState.AutoContinue)
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

        private IEnumerator HandleEvent(in TagString.EventData inEvent)
        {
            if (inEvent.Type == "speaker")
            {
                SetSpeaker(inEvent.StringArgument);
                return null;
            }

            if (inEvent.Type == "wait")
            {
                return Routine.WaitSeconds(inEvent.NumberArgument * GetSkipMultiplier());
            }

            if (inEvent.Type == "speed")
            {
                m_CurrentState.Speed = inEvent.NumberArgument;
                return Routine.WaitSeconds(0.15f);
            }

            if (inEvent.Type == "input_continue")
            {
                return WaitForInput();
            }

            if (inEvent.Type == "play_sound")
            {
                Services.Audio.PostEvent(inEvent.StringArgument);
                return null;
            }

            if (inEvent.Type == "play_bgm")
            {
                bool bCrossFade = m_BGM.IsPlaying();
                m_BGM.Stop(0.5f);

                m_BGM = Services.Audio.PostEvent(inEvent.StringArgument);
                if (bCrossFade)
                {
                    m_BGM.SetVolume(0).SetVolume(1, 0.5f);
                }
                return null;
            }

            if (inEvent.Type == "pitch_bgm")
            {
                float pitch, duration;
                if (string.IsNullOrEmpty(inEvent.StringArgument))
                {
                    pitch = 1;
                    duration = 0.5f;
                }
                else
                {
                    string[] split = inEvent.StringArgument.Split(' ');
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
                return null;
            }

            if (inEvent.Type == "stop_bgm")
            {
                m_BGM.Stop(inEvent.NumberArgument);
                m_BGM = AudioHandle.Null;
                return null;
            }

            if (inEvent.Type == "show")
            {
                Show();
                return null;
            }

            if (inEvent.Type == "hide")
            {
                Hide();
                return null;
            }

            if (inEvent.Type == "auto_continue")
            {
                m_CurrentState.AutoContinue = true;
                return null;
            }

            if (inEvent.Type == "set_type_sfx")
            {
                m_CurrentState.TypeSFX = inEvent.StringArgument;
                return null;
            }

            if (inEvent.Type == "clear")
            {
                m_TextDisplay.SetText(string.Empty);
                return null;
            }

            if (inEvent.Type == "target")
            {
                if (string.IsNullOrEmpty(inEvent.StringArgument))
                {
                    m_CurrentState.TypeSFX = inEvent.StringArgument;
                }
                else
                {
                    m_CurrentState.TypeSFX = "text_type_" + inEvent.StringArgument;
                    if (inEvent.StringArgument == "kevin")
                    {
                        SetSpeaker("Kevin, Your Science Familiar");
                    }
                    else if (inEvent.StringArgument == "player")
                    {
                        SetSpeaker("Player");
                    }
                }
                return null;
            }

            return null;
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

        private class EventParser : TagStringParser.IEventProcessor
        {
            public bool TryProcess(TagStringParser.TagData inData, out TagString.EventData outEvent)
            {
                if (inData.Id.Equals("wait", true))
                {
                    float seconds = float.Parse(inData.Data.ToString());
                    outEvent = new TagString.EventData("wait", seconds);
                    return true;
                }

                if (inData.Id.Equals("speed", true))
                {
                    float multiplier;
                    if (inData.IsEnd())
                    {
                        multiplier = 1;
                    }
                    else
                    {
                        multiplier = float.Parse(inData.Data.ToString());
                    }
                    outEvent = new TagString.EventData("speed", multiplier);
                    return true;
                }

                if (inData.Id.Equals("input_continue", true))
                {
                    outEvent = new TagString.EventData("input_continue");
                    return true;
                }

                if (inData.Id.Equals("speaker", true))
                {
                    outEvent = new TagString.EventData("speaker", inData.Data);
                    return true;
                }

                if (inData.Id.Equals("sfx", true) || inData.Id.Equals("sound", true))
                {
                    outEvent = new TagString.EventData("play_sound", inData.Data);
                    return true;
                }

                if (inData.Id.Equals("bgm", true))
                {
                    outEvent = new TagString.EventData("play_bgm", inData.Data);
                    return true;
                }

                if (inData.Id.Equals("bgm_pitch", true))
                {
                    outEvent = new TagString.EventData("pitch_bgm", inData.Data);
                    return true;
                }

                if (inData.Id.Equals("stop_bgm", true))
                {
                    float fadeTime = 0.5f;
                    if (!inData.Data.IsEmpty)
                        fadeTime = float.Parse(inData.Data.ToString());
                    outEvent = new TagString.EventData("stop_bgm", fadeTime);
                    return true;
                }

                if (inData.Id.Equals("show", true))
                {
                    outEvent = new TagString.EventData("show");
                    return true;
                }

                if (inData.Id.Equals("hide", true))
                {
                    outEvent = new TagString.EventData("hide");
                    return true;
                }

                if (inData.Id.Equals("clear", true))
                {
                    outEvent = new TagString.EventData("clear");
                    return true;
                }

                if (inData.Id.Equals("auto", true))
                {
                    outEvent = new TagString.EventData("auto_continue");
                    return true;
                }

                if (inData.Id.Equals("type", true))
                {
                    outEvent = new TagString.EventData("set_type_sfx", inData.Data);
                    return true;
                }

                if (inData.Id.Equals("target", true) || inData.Id.Equals("t", true))
                {
                    outEvent = new TagString.EventData("target", inData.Data);
                    return true;
                }

                outEvent = default(TagString.EventData);
                return false;
            }
        }

        private class ReplaceParser : TagStringParser.ITextProcessor
        {
            public bool TryReplace(TagStringParser.TagData inData, out string outReplace)
            {
                if (inData.Id == "n")
                {
                    outReplace = "\n";
                    return true;
                }

                if (inData.Id == "highlight")
                {
                    if (inData.IsEnd())
                        outReplace = "</color>";
                    else
                        outReplace = "<color=yellow>";
                    return true;
                }

                outReplace = null;
                return false;
            }
        }
    }
}