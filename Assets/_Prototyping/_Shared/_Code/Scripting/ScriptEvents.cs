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

namespace ProtoAqua
{
    static public class ScriptEvents
    {
        static public class Global
        {
            static public readonly PropertyName HideDialog = "hide-dialog";
            static public readonly PropertyName PitchBGM = "bgm-pitch";
            static public readonly PropertyName PlayBGM = "bgm-play";
            static public readonly PropertyName PlaySound = "sound-play";
            static public readonly PropertyName ShowDialog = "show-dialog";
            static public readonly PropertyName StopBGM = "bgm-stop";
            static public readonly PropertyName Wait = "wait";
            static public readonly PropertyName WaitReal = "wait-real";
            static public readonly PropertyName LetterboxOn = "letterbox-on";
            static public readonly PropertyName LetterboxOff = "letterbox-off";
            static public readonly PropertyName EnableObject = "enable-object";
            static public readonly PropertyName DisableObject = "disable-object";
        }

        static public class Dialog
        {
            static public readonly PropertyName Auto = "auto-continue";
            static public readonly PropertyName Clear = "clear";
            static public readonly PropertyName InputContinue = "input-continue";
            static public readonly PropertyName SetTypeSFX = "set-type-sfx";
            static public readonly PropertyName Speaker = "set-speaker";
            static public readonly PropertyName Speed = "set-speed";
            static public readonly PropertyName Target = "set-target";
        }
    }
}