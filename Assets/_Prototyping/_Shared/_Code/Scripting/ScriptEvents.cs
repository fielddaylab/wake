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
using BeauUtil.Tags;

namespace ProtoAqua.Scripting
{
    static public class ScriptEvents
    {
        static public class Global
        {
            static public readonly StringHash32 HideDialog = "hide-dialog";
            static public readonly StringHash32 PitchBGM = "bgm-pitch";
            static public readonly StringHash32 PlayBGM = "bgm-play";
            static public readonly StringHash32 PlaySound = "sound-play";
            static public readonly StringHash32 ShowDialog = "show-dialog";
            static public readonly StringHash32 StopBGM = "bgm-stop";
            static public readonly StringHash32 Wait = "wait";
            static public readonly StringHash32 BroadcastEvent = "broadcast-event";
            static public readonly StringHash32 WaitAbsolute = "wait-abs";
            static public readonly StringHash32 LetterboxOn = "letterbox-on";
            static public readonly StringHash32 LetterboxOff = "letterbox-off";
            static public readonly StringHash32 EnableObject = "enable-object";
            static public readonly StringHash32 DisableObject = "disable-object";
            static public readonly StringHash32 SetVariable = "set-variable";
        }

        static public class Dialog
        {
            static public readonly StringHash32 Auto = "auto-continue";
            static public readonly StringHash32 Clear = "clear";
            static public readonly StringHash32 InputContinue = "input-continue";
            static public readonly StringHash32 SetTypeSFX = "set-type-sfx";
            static public readonly StringHash32 Speaker = "set-speaker";
            static public readonly StringHash32 Speed = "set-speed";
            static public readonly StringHash32 Target = "set-target";
        }
    }
}