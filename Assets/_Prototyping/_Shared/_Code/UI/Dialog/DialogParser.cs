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
    public class DialogParser
    {
        static DialogParser()
        {
            ParserConfig = new CustomTagParserConfig();

            ParserConfig.AddReplace("n", "\n").WithAliases("newline");
            ParserConfig.AddReplace("highlight", "<color=yellow>").CloseWith("</color>");
            ParserConfig.AddReplace("player_name", () => Environment.UserName);
            ParserConfig.AddReplace("cash", "<#a6c8ff>").CloseWith("ø</color>");
            ParserConfig.AddReplace("gears", "<#c9c86d>").CloseWith("‡</color>");

            ParserConfig.AddEvent("auto", Event_Auto);
            ParserConfig.AddEvent("bgm_pitch", Event_PitchBGM).WithFloatData();
            ParserConfig.AddEvent("bgm_stop", Event_StopBGM).WithFloatData(0.5f);
            ParserConfig.AddEvent("bgm", Event_PlayBGM).WithStringData();
            ParserConfig.AddEvent("clear", Event_Clear);
            ParserConfig.AddEvent("hide", Event_Hide);
            ParserConfig.AddEvent("input_continue", Event_InputContinue);
            ParserConfig.AddEvent("sfx", Event_PlaySound).WithAliases("sound").WithStringData();
            ParserConfig.AddEvent("show", Event_Show);
            ParserConfig.AddEvent("speaker", Event_Speaker).WithStringData();
            ParserConfig.AddEvent("speed", Event_Speed).WithFloatData(1);
            ParserConfig.AddEvent("target", Event_Target).WithAliases("t", "@*").ProcessWith(TargetHandler);
            ParserConfig.AddEvent("type", Event_SetTypeSFX).WithStringData();
            ParserConfig.AddEvent("wait", Event_Wait).WithFloatData(0.25f);

            ParserConfig.Lock();
        }

        static private CustomTagParserConfig.EventDelegate TargetHandler = (TagStringParser.TagData t, object o, ref TagString.EventData e) => {
            if (t.Id.StartsWith('@'))
                e.StringArgument = t.Id.Substring(1).ToString();
            else
                e.StringArgument = t.Data.ToString();
        };

        static public readonly CustomTagParserConfig ParserConfig;

        #region Ids

        static public readonly PropertyName Event_Auto = "auto";
        static public readonly PropertyName Event_Clear = "clear";
        static public readonly PropertyName Event_Hide = "hide";
        static public readonly PropertyName Event_InputContinue = "input_continue";
        static public readonly PropertyName Event_PitchBGM = "bgm_pitch";
        static public readonly PropertyName Event_PlayBGM = "bgm_play";
        static public readonly PropertyName Event_PlaySound = "sound_play";
        static public readonly PropertyName Event_SetTypeSFX = "set_type_sfx";
        static public readonly PropertyName Event_Show = "show";
        static public readonly PropertyName Event_Speaker = "speaker";
        static public readonly PropertyName Event_Speed = "speed";
        static public readonly PropertyName Event_StopBGM = "bgm_stop";
        static public readonly PropertyName Event_Target = "target";
        static public readonly PropertyName Event_Wait = "wait";

        #endregion // Ids
    }
}