using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using ProtoAqua.Profile;
using UnityEngine;

namespace ProtoAqua
{
    public partial class ScriptingService : ServiceBehaviour
    {
        #region Parser

        private void InitParser()
        {
            m_TagEventParser = new CustomTagParserConfig();

            // Replace Tags
            m_TagEventParser.AddReplace("n", "\n").WithAliases("newline");
            m_TagEventParser.AddReplace("highlight", "<color=yellow>").CloseWith("</color>");
            m_TagEventParser.AddReplace("player_name", () => Services.Data.CurrentCharacterName());
            m_TagEventParser.AddReplace("cash", "<#a6c8ff>").CloseWith("ø</color>");
            m_TagEventParser.AddReplace("gears", "<#c9c86d>").CloseWith("‡</color>");
            m_TagEventParser.AddReplace("pg", ReplacePlayerGender);

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
            m_TagEventParser.AddEvent("broadcast-event", ScriptEvents.Global.BroadcastEvent).WithStringData();

            // Dialog-Specific Events
            m_TagEventParser.AddEvent("auto", ScriptEvents.Dialog.Auto);
            m_TagEventParser.AddEvent("clear", ScriptEvents.Dialog.Clear);
            m_TagEventParser.AddEvent("input-continue", ScriptEvents.Dialog.InputContinue).WithAliases("continue");
            m_TagEventParser.AddEvent("speaker", ScriptEvents.Dialog.Speaker).WithStringData();
            m_TagEventParser.AddEvent("speed", ScriptEvents.Dialog.Speed).WithFloatData(1);
            m_TagEventParser.AddEvent("@*", ScriptEvents.Dialog.Target).ProcessWith(ParseTargetEventData);
            m_TagEventParser.AddEvent("type", ScriptEvents.Dialog.SetTypeSFX).WithStringData();
        }

        static private CustomTagParserConfig.EventDelegate ParseTargetEventData = (TagData t, object o, ref TagEventData e) => {
            e.StringArgument = t.Id.Substring(1).ToString();
        };

        static private CustomTagParserConfig.ReplaceWithContextDelegate ReplacePlayerGender = (TagData t, object o) => {
            TempList16<StringSlice> slices = new TempList16<StringSlice>();
            int sliceCount = t.Data.Split(ChoiceSplitChars, StringSplitOptions.None, ref slices);
            Pronouns playerPronouns = Services.Data.CurrentCharacterPronouns();
            
            if (sliceCount < 3)
            {
                Debug.LogWarningFormat("[ScriptingService] Expected 3 arguments to '{0}' tag", t.Id);
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
        };

        static private readonly char[] ChoiceSplitChars = new char[] { '|' };

        #endregion // Parser

        #region Event Setup

        private void InitHandlers()
        {
            m_TagEventHandler = new TagStringEventHandler();
            m_ArgListSplitter = new StringUtils.ArgsList.Splitter();

            // m_TagEventHandler.Register()

            m_TagEventHandler
                .Register(ScriptEvents.Global.HideDialog, () => { Services.UI.Dialog().Hide(); } )
                .Register(ScriptEvents.Global.LetterboxOff, () => Services.UI.HideLetterbox() )
                .Register(ScriptEvents.Global.LetterboxOn, () => Services.UI.ShowLetterbox() )
                .Register(ScriptEvents.Global.PitchBGM, (e, o) => {
                    TempList16<StringSlice> args = new TempList16<StringSlice>();
                    int argCount = ExtractArgs(e.StringArgument, ref args);
                    float pitch = 1;
                    float fadeTime = 0.5f;
                    if (argCount >= 1)
                    {
                        pitch = StringParser.ParseFloat(args[0], 1);
                    }
                    if (argCount >= 2)
                    {
                        fadeTime = StringParser.ParseFloat(args[1], 0.5f);
                    }
                    Services.Audio.CurrentMusic().SetPitch(pitch, fadeTime);
                })
                .Register(ScriptEvents.Global.PlayBGM, (e, o) => {
                    TempList16<StringSlice> args = new TempList16<StringSlice>();
                    int argCount = ExtractArgs(e.StringArgument, ref args);
                    if (argCount > 0)
                    {
                        float crossFadeTime = 0.5f;
                        string name = args[0].ToString();
                        if (argCount >= 2)
                        {
                            crossFadeTime = StringParser.ParseFloat(args[1], 0.5f);
                        }
                        Services.Audio.SetMusic(name, crossFadeTime);
                    }
                })
                .Register(ScriptEvents.Global.PlaySound, (e, o) => {
                    Services.Audio.PostEvent(e.StringArgument);
                })
                .Register(ScriptEvents.Global.ShowDialog, () => { Services.UI.Dialog().Show(); } )
                .Register(ScriptEvents.Global.StopBGM, (e, o) => {
                    Services.Audio.StopMusic(e.Argument0.AsFloat());
                })
                .Register(ScriptEvents.Global.Wait, (e, o) => {
                    return Routine.WaitSeconds(e.Argument0.AsFloat());
                })
                .Register(ScriptEvents.Global.WaitAbsolute, (e, o) => {
                    return Routine.WaitSeconds(e.Argument0.AsFloat());
                })
                .Register(ScriptEvents.Global.EnableObject, (e, o) => {
                    TempList16<StringSlice> args = new TempList16<StringSlice>();
                    int argCount = ExtractArgs(e.StringArgument, ref args);

                });
        }

        private int ExtractArgs(string inString, ref TempList16<StringSlice> outArgs)
        {
            return StringSlice.Split(inString, m_ArgListSplitter, StringSplitOptions.None, ref outArgs);
        }

        #endregion // Event Setup
    }
}