using BeauUtil;

namespace Aqua.Scripting
{
    static public class ScriptEvents
    {
        static public class Global
        {
            static public readonly StringHash32 TriggerResponse = "trigger-response";

            static public readonly StringHash32 BoxStyle = "box-style";
            static public readonly StringHash32 BroadcastEvent = "broadcast-event";
            static public readonly StringHash32 BlockInput = "input-block";
            static public readonly StringHash32 UnblockInput = "input-unblock";
            static public readonly StringHash32 DisableObject = "disable-object";
            static public readonly StringHash32 EnableObject = "enable-object";
            static public readonly StringHash32 FadeIn = "fade-in";
            static public readonly StringHash32 FadeOut = "fade-out";
            static public readonly StringHash32 HideDialog = "hide-dialog";
            static public readonly StringHash32 ReleaseDialog = "release-dialog";
            static public readonly StringHash32 CutsceneOff = "cutscene-off";
            static public readonly StringHash32 CutsceneOn = "cutscene-on";
            static public readonly StringHash32 PitchBGM = "bgm-pitch";
            static public readonly StringHash32 PlayBGM = "bgm-play";
            static public readonly StringHash32 PlaySound = "sound-play";
            static public readonly StringHash32 ScreenFlash = "flash";
            static public readonly StringHash32 ScreenWipeIn = "wipe-in";
            static public readonly StringHash32 ScreenWipeOut = "wipe-out";
            static public readonly StringHash32 ShowDialog = "show-dialog";
            static public readonly StringHash32 StopBGM = "bgm-stop";
            static public readonly StringHash32 VolumeBGM = "bgm-volume";
            static public readonly StringHash32 WaitAbsolute = "wait-abs";
        }

        static public class Dialog
        {
            static public readonly StringHash32 Auto = "auto-continue";
            static public readonly StringHash32 Clear = "clear";
            static public readonly StringHash32 DoNotClose = "do-not-close";
            static public readonly StringHash32 InputContinue = "input-continue";
            static public readonly StringHash32 SetTypeSFX = "set-type-sfx";
            static public readonly StringHash32 SetVoiceType = "set-voice-type";
            static public readonly StringHash32 Speaker = "set-speaker";
            static public readonly StringHash32 Speed = "set-speed";
        }
    }
}