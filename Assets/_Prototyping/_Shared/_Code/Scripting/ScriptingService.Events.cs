using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Variants;
using ProtoAqua.Profile;
using UnityEngine;
using ProtoAqua.Scripting;
using System.Collections;
using ProtoAudio;

namespace ProtoAqua
{
    public partial class ScriptingService : ServiceBehaviour
    {
        #region Parser

        private void InitParsers()
        {
            m_TagEventParser = new CustomTagParserConfig();

            // Replace Tags
            m_TagEventParser.AddReplace("n", "\n").WithAliases("newline");
            m_TagEventParser.AddReplace("highlight", "<color=yellow>").WithAliases("h").CloseWith("</color>");
            m_TagEventParser.AddReplace("player-name", () => Services.Data.CurrentCharacterName());
            m_TagEventParser.AddReplace("cash", "<#a6c8ff>").CloseWith("ø</color>");
            m_TagEventParser.AddReplace("gears", "<#c9c86d>").CloseWith("‡</color>");
            m_TagEventParser.AddReplace("pg", ReplacePlayerGender);
            m_TagEventParser.AddReplace("loc", ReplaceLoc);
            m_TagEventParser.AddReplace("var", ReplaceVariable).WithAliases("var-i", "var-f", "var-b", "var-s");
            m_TagEventParser.AddReplace("switch-var", SwitchOnVariable);
            m_TagEventParser.AddReplace("icon", ReplaceIcon);

            // Extra Replace Tags (with embedded events)

            m_TagEventParser.AddReplace("slow", "{wait 0.05}{speed 0.5}").CloseWith("{/speed}{wait 0.05}");
            m_TagEventParser.AddReplace("reallySlow", "{wait 0.05}{speed 0.25}").CloseWith("{/speed}{wait 0.05}");

            // Global Events
            m_TagEventParser.AddEvent("bgm-pitch", ScriptEvents.Global.PitchBGM).WithFloatData();
            m_TagEventParser.AddEvent("bgm-stop", ScriptEvents.Global.StopBGM).WithFloatData(0.5f);
            m_TagEventParser.AddEvent("bgm", ScriptEvents.Global.PlayBGM).WithStringData();
            m_TagEventParser.AddEvent("hide-dialog", ScriptEvents.Global.HideDialog);
            m_TagEventParser.AddEvent("sfx", ScriptEvents.Global.PlaySound).WithAliases("sound").WithStringData();
            m_TagEventParser.AddEvent("show-dialog", ScriptEvents.Global.ShowDialog);
            m_TagEventParser.AddEvent("wait", ScriptEvents.Global.Wait).WithFloatData(0.25f);
            m_TagEventParser.AddEvent("wait-abs", ScriptEvents.Global.WaitAbsolute).WithFloatData(0.25f);
            m_TagEventParser.AddEvent("letterbox", ScriptEvents.Global.LetterboxOn).CloseWith(ScriptEvents.Global.LetterboxOff);
            m_TagEventParser.AddEvent("enable-object", ScriptEvents.Global.EnableObject).WithStringData();
            m_TagEventParser.AddEvent("disable-object", ScriptEvents.Global.DisableObject).WithStringData();
            m_TagEventParser.AddEvent("set", ScriptEvents.Global.SetVariable).WithStringData();
            m_TagEventParser.AddEvent("broadcast-event", ScriptEvents.Global.BroadcastEvent).WithAliases("broadcast").WithStringData();
            m_TagEventParser.AddEvent("fade-out", ScriptEvents.Global.FadeOut).WithStringData();
            m_TagEventParser.AddEvent("fade-in", ScriptEvents.Global.FadeIn).WithStringData();
            m_TagEventParser.AddEvent("wipe-out", ScriptEvents.Global.ScreenWipeOut).WithStringData();
            m_TagEventParser.AddEvent("wipe-in", ScriptEvents.Global.ScreenWipeIn).WithStringData();
            m_TagEventParser.AddEvent("screen-flash", ScriptEvents.Global.ScreenFlash).WithStringData();
            m_TagEventParser.AddEvent("trigger-response", ScriptEvents.Global.TriggerResponse).WithAliases("trigger").WithStringData();
            m_TagEventParser.AddEvent("load-scene", ScriptEvents.Global.LoadScene).WithStringData();
            m_TagEventParser.AddEvent("style", ScriptEvents.Global.BoxStyle).WithStringHashData();

            // Dialog-Specific Events
            m_TagEventParser.AddEvent("auto", ScriptEvents.Dialog.Auto);
            m_TagEventParser.AddEvent("clear", ScriptEvents.Dialog.Clear);
            m_TagEventParser.AddEvent("input-continue", ScriptEvents.Dialog.InputContinue).WithAliases("continue");
            m_TagEventParser.AddEvent("speaker", ScriptEvents.Dialog.Speaker).WithStringData();
            m_TagEventParser.AddEvent("speed", ScriptEvents.Dialog.Speed).WithFloatData(1);
            m_TagEventParser.AddEvent("@*", ScriptEvents.Dialog.Target).ProcessWith(ParseTargetEventData);
            m_TagEventParser.AddEvent("type", ScriptEvents.Dialog.SetTypeSFX).WithStringHashData();
        }

        static private void ParseTargetEventData(TagData inTag, object inContext, ref TagEventData ioEvent)
        {
            ioEvent.Argument0 = inTag.Id.Substring(1).Hash32();
        }

        #endregion // Parser

        #region Replace Callbacks

        static private string ReplacePlayerGender(TagData inTag, object inContext)
        {
            TempList16<StringSlice> slices = new TempList16<StringSlice>();
            int sliceCount = inTag.Data.Split(ChoiceSplitChars, StringSplitOptions.None, ref slices);
            Pronouns playerPronouns = Services.Data.CurrentCharacterPronouns();
            
            if (sliceCount < 3)
            {
                Debug.LogWarningFormat("[ScriptingService] Expected 3 arguments to '{0}' tag", inTag.Id);
                if (sliceCount == 0)
                {
                    return playerPronouns.ToString();
                }
                else if (sliceCount == 1)
                {
                    return slices[0].ToString();
                }
                else
                {
                    switch(playerPronouns)
                    {
                        case Pronouns.Masculine:
                        default:
                            return slices[0].ToString();
                        case Pronouns.Feminine:
                            return slices[1].ToString();
                    }
                }
            }
            else
            {
                switch(playerPronouns)
                {
                    case Pronouns.Masculine:
                        return slices[0].ToString();
                    case Pronouns.Feminine:
                        return slices[1].ToString();
                    default:
                        return slices[2].ToString();
                }
            }
        }

        static private string ReplaceVariable(TagData inTag, object inContext)
        {
            var thread = Thread(inContext);
            var resolver = thread?.Resolver ?? Services.Data.VariableResolver;
            Variant variable;
            resolver.TryGetVariant(thread?.Context, TableKeyPair.Parse(inTag.Data), out variable);
            
            if (inTag.Id.EndsWith("-i"))
            {
                return variable.AsInt().ToString();
            }
            else if (inTag.Id.EndsWith("-f"))
            {
                return variable.AsFloat().ToString();
            }
            else if (inTag.Id.EndsWith("-b"))
            {
                return variable.AsBool().ToString();
            }
            else if (inTag.Id.EndsWith("-s"))
            {
                return Services.Loc.Localize(variable.AsStringHash(), variable.ToDebugString(), inContext);
            }
            else
            {
                return variable.ToString();
            }
        }

        static private string ReplaceLoc(TagData inTag, object inContext)
        {
            return Services.Loc.Localize(inTag.Data, null, inContext);
        }

        static private string ReplaceIcon(TagData inTag, object inContext)
        {
            return string.Format("<sprite name=\"{0}\">", inTag.Data.ToString());
        }

        static private string SwitchOnVariable(TagData inTag, object inContext)
        {
            int commaIdx = inTag.Data.IndexOf(',');
            if (commaIdx > 0)
            {
                StringSlice varId = inTag.Data.Substring(0, commaIdx).TrimEnd();
                StringSlice args = inTag.Data.Substring(commaIdx + 1).TrimStart();

                TempList16<StringSlice> slices = new TempList16<StringSlice>();
                int sliceCount = args.Split(ChoiceSplitChars, StringSplitOptions.None, ref slices);

                if (slices.Count <= 0)
                {
                    Debug.LogWarningFormat("[ScriptingService] Fewer than expected arguments provided to '{0}' tag", inTag.Id);
                    return varId.ToString();
                }
                
                Variant variable = Services.Data.GetVariable(varId, inContext);
                int idx = 0;
                switch(variable.Type)
                {
                    case VariantType.Null:
                        idx = 0;
                        break;
                    case VariantType.Bool:
                        idx = variable.AsBool() ? 0 : 1;
                        break;
                    case VariantType.Int:
                    case VariantType.Float:
                    case VariantType.UInt:
                        idx = variable.AsInt();
                        break;
                    default:
                        Debug.LogWarningFormat("[ScriptingService] Unsupported variable type {0} provided to '{0}' tag", variable.Type, inTag.Id);
                        return varId.ToString();
                }

                if (idx > slices.Count)
                {
                    Debug.LogWarningFormat("[ScriptingService] Switched on {0}, but result {1} out of range of provided args (count {2})", varId, idx, slices.Count);
                    idx = 0;
                }

                return slices[idx].ToString();
            }
            else
            {
                Debug.LogWarningFormat("[ScriptingService] Fewer than expected arguments provided to '{0}' tag", inTag.Id);
                return string.Empty;
            }
        }

        static private readonly char[] ChoiceSplitChars = new char[] { '|' };

        #endregion // Replace Callbacks

        #region Event Setup

        private void InitHandlers()
        {
            m_TagEventHandler = new TagStringEventHandler();
            m_ArgListSplitter = new StringUtils.ArgsList.Splitter();

            m_TagEventHandler
                .Register(ScriptEvents.Global.HideDialog, EventHideDialog)
                .Register(ScriptEvents.Global.ShowDialog, EventShowDialog)
                .Register(ScriptEvents.Global.LetterboxOff, () => Services.UI.HideLetterbox() )
                .Register(ScriptEvents.Global.LetterboxOn, () => Services.UI.ShowLetterbox() )
                .Register(ScriptEvents.Global.PitchBGM, EventPitchBGM)
                .Register(ScriptEvents.Global.PlayBGM, EventPlayBGM)
                .Register(ScriptEvents.Global.PlaySound, EventPlaySound)
                .Register(ScriptEvents.Global.StopBGM, (e, o) => { Services.Audio.StopMusic(e.Argument0.AsFloat()); })
                .Register(ScriptEvents.Global.Wait, (e, o) => { return Routine.WaitSeconds(e.Argument0.AsFloat()); })
                .Register(ScriptEvents.Global.WaitAbsolute, (e, o) => { return Routine.WaitSeconds(e.Argument0.AsFloat()); })
                .Register(ScriptEvents.Global.SetVariable, EventSetVariable)
                .Register(ScriptEvents.Global.BroadcastEvent, EventBroadcastEvent)
                .Register(ScriptEvents.Global.TriggerResponse, EventTriggerResponse)
                .Register(ScriptEvents.Global.LoadScene, EventLoadScene)
                .Register(ScriptEvents.Global.BoxStyle, EventSetBoxStyle)
                .Register(ScriptEvents.Global.ScreenWipeOut, EventScreenWipeOut)
                .Register(ScriptEvents.Global.ScreenWipeIn, EventScreenWipeIn)
                .Register(ScriptEvents.Global.ScreenFlash, EventScreenFlash)
                .Register(ScriptEvents.Global.FadeOut, EventFadeOut)
                .Register(ScriptEvents.Global.FadeIn, EventFadeIn);
        }

        #endregion // Event Setup
    
        #region Event Callbacks

        static private ScriptThread Thread(object inObject)
        {
            return inObject as ScriptThread;
        }

        private TempList8<StringSlice> ExtractArgs(StringSlice inString)
        {
            TempList8<StringSlice> args = new TempList8<StringSlice>();
            inString.Split(m_ArgListSplitter, StringSplitOptions.None, ref args);
            return args;
        }

        private void EventHideDialog(TagEventData inEvent, object inContext)
        {
            Thread(inContext).Dialog?.Hide();
        }

        private void EventShowDialog(TagEventData inEvent, object inContext)
        {
            Thread(inContext).Dialog?.Show();
        }

        private IEnumerator EventPlaySound(TagEventData inEvent, object inContext)
        {
            var args = ExtractArgs(inEvent.StringArgument);

            StringHash32 id = args[0];
            AudioHandle playback = Services.Audio.PostEvent(id);
            if (args.Count > 1 && args[1] == "wait")
            {
                return playback.Wait();
            }

            return null;
        }

        private void EventPlayBGM(TagEventData inEvent, object inContext)
        {
            var args = ExtractArgs(inEvent.StringArgument);
            if (args.Count > 0)
            {
                float crossFadeTime = 0.5f;
                string name = args[0].ToString();
                if (args.Count >= 2)
                {
                    crossFadeTime = StringParser.ParseFloat(args[1], 0.5f);
                }
                Services.Audio.SetMusic(name, crossFadeTime);
            }
        }

        private void EventPitchBGM(TagEventData inEvent, object inContext)
        {
            var args = ExtractArgs(inEvent.StringArgument);
            float pitch = 1;
            float fadeTime = 0.5f;
            if (args.Count >= 1)
            {
                pitch = StringParser.ParseFloat(args[0], 1);
            }
            if (args.Count >= 2)
            {
                fadeTime = StringParser.ParseFloat(args[1], 0.5f);
            }
            Services.Audio.CurrentMusic().SetPitch(pitch, fadeTime);
        }

        private void EventSetVariable(TagEventData inEvent, object inContext)
        {
            var resolver = Thread(inContext)?.Resolver ?? Services.Data.VariableResolver;
            if (!resolver.TryModify(inContext, inEvent.StringArgument))
            {
                Debug.LogErrorFormat("[ScriptingService] Failed to set variables from string '{0}'", inEvent.StringArgument);
            }
        }

        private void EventBroadcastEvent(TagEventData inEvent, object inContext)
        {
            Services.Events.Dispatch(inEvent.StringArgument);
        }

        private void EventTriggerResponse(TagEventData inEvent, object inContext)
        {
            ScriptThread thread = Thread(inContext);
            var args = ExtractArgs(inEvent.StringArgument);

            StringHash32 trigger = args[0];
            StringHash32 who = args.Count > 1 ? args[1] : StringHash32.Null;

            Services.Script.TriggerResponse(trigger, who, thread?.Context, thread?.Locals);
        }

        private IEnumerator EventLoadScene(TagEventData inEvent, object inContext)
        {
            var args = ExtractArgs(inEvent.StringArgument);
            
            SceneLoadFlags flags = SceneLoadFlags.Default;
            string context = null;
            if (args.Count >= 2 && args[1] == "no-loading-screen")
            {
                flags |= SceneLoadFlags.NoLoadingScreen;
            }
            if (args.Count >= 3)
            {
                context = args[2].ToString();
            }

            return Services.State.LoadScene(args[0].ToString(), context, flags);
        }

        private void EventSetBoxStyle(TagEventData inEvent, object inContext)
        {
            Thread(inContext).Dialog = Services.UI.GetDialog(inEvent.Argument0.AsStringHash());
        }

        private IEnumerator EventScreenWipeOut(TagEventData inEvent, object inContext)
        {
            var args = ExtractArgs(inEvent.StringArgument);

            var faderSource = Services.UI.WorldFaders;
            if (args.Count > 0 && args[0] == "above-ui")
            {
                faderSource = Services.UI.ScreenFaders;
            }
            
            var thread = Thread(inContext);
            if (thread.ScreenWipe == null)
            {
                var wipe = faderSource.AllocWipe().Object;
                thread.ScreenWipe = wipe;
                return wipe.Show();
            }
            return null;
        }

        private IEnumerator EventScreenWipeIn(TagEventData inEvent, object inContext)
        {
            var thread = Thread(inContext);
            if (thread.ScreenWipe != null)
            {
                IEnumerator hide = thread.ScreenWipe.Hide(true);
                thread.ClearWipeWithoutHide();
                return hide;
            }
            return null;
        }

        private IEnumerator EventScreenFlash(TagEventData inEvent, object inContext)
        {
            var args = ExtractArgs(inEvent.StringArgument);

            Color color = Parsing.ParseColor(args[0]);
            float duration = StringParser.ParseFloat(args[1], 0.2f);

            var faders = Services.UI.WorldFaders;
            bool bWait = false;
            for(int i = 2; i < args.Count; ++i)
            {
                var arg = args[i];
                if (arg == "above-ui")
                {
                    faders = Services.UI.ScreenFaders;
                }
                else if (arg == "wait")
                {
                    bWait = true;
                }
            }

            IEnumerator flash = faders.Flash(color, duration);
            if (bWait)
                return flash;
            return null;
        }

        private IEnumerator EventFadeOut(TagEventData inEvent, object inContext)
        {
            var args = ExtractArgs(inEvent.StringArgument);

            Color color = Parsing.ParseColor(args[0]);
            float duration = StringParser.ParseFloat(args[1], 0.2f);

            var faders = Services.UI.WorldFaders;
            bool bWait = false;
            for(int i = 2; i < args.Count; ++i)
            {
                var arg = args[i];
                if (arg == "above-ui")
                {
                    faders = Services.UI.ScreenFaders;
                }
                else if (arg == "wait")
                {
                    bWait = true;
                }
            }
            
            var thread = Thread(inContext);
            if (thread.ScreenFader == null)
            {
                var fader = faders.AllocFader().Object;
                thread.ScreenFader = fader;
            }

            IEnumerator fade = thread.ScreenFader.Show(color, duration);
            if (bWait)
                return fade;
            return null;
        }

        private IEnumerator EventFadeIn(TagEventData inEvent, object inContext)
        {
            var args = ExtractArgs(inEvent.StringArgument);

            float duration = StringParser.ParseFloat(args[0], 0.2f);

            bool bWait = false;
            for(int i = 1; i < args.Count; ++i)
            {
                var arg = args[i];
                if (arg == "wait")
                {
                    bWait = true;
                }
            }
            
            var thread = Thread(inContext);
            if (thread.ScreenFader != null)
            {
                var fader = thread.ScreenFader;
                thread.ClearFaderWithoutHide();
                IEnumerator fadeRoutine = fader.Hide(duration, true);
                if (bWait)
                    return fadeRoutine;
            }

            return null;
        }

        private void EventEnableObject(TagEventData inEvent, object inContext)
        {
            EventEnableDisableObjectImpl(inEvent, inContext, true);
        }

        private void EventDisableObject(TagEventData inEvent, object inContext)
        {
            EventEnableDisableObjectImpl(inEvent, inContext, false);
        }

        private void EventEnableDisableObjectImpl(TagEventData inEvent, object inContext, bool inbActive)
        {

        }

        #endregion // Event Callbacks
    }
}