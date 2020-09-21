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
            static public readonly StringHash HideDialog = "hide-dialog";
            static public readonly StringHash PitchBGM = "bgm-pitch";
            static public readonly StringHash PlayBGM = "bgm-play";
            static public readonly StringHash PlaySound = "sound-play";
            static public readonly StringHash ShowDialog = "show-dialog";
            static public readonly StringHash StopBGM = "bgm-stop";
            static public readonly StringHash Wait = "wait";
            static public readonly StringHash BroadcastEvent = "broadcast-event";
            static public readonly StringHash WaitAbsolute = "wait-abs";
            static public readonly StringHash LetterboxOn = "letterbox-on";
            static public readonly StringHash LetterboxOff = "letterbox-off";
            static public readonly StringHash EnableObject = "enable-object";
            static public readonly StringHash DisableObject = "disable-object";
            static public readonly StringHash SetVariable = "set-variable";
        }

        static public class Dialog
        {
            static public readonly StringHash Auto = "auto-continue";
            static public readonly StringHash Clear = "clear";
            static public readonly StringHash InputContinue = "input-continue";
            static public readonly StringHash SetTypeSFX = "set-type-sfx";
            static public readonly StringHash Speaker = "set-speaker";
            static public readonly StringHash Speed = "set-speed";
            static public readonly StringHash Target = "set-target";
        }
    }
}