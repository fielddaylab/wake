#if !UNITY_EDITOR && UNITY_WEBGL
#define WEBGL_NATIVE
#endif // !UNITY_EDITOR && UNITY_WEBGL

using System.Runtime.InteropServices;
using BeauUtil.Debugger;

namespace AquaAudio
{
    static public class TTS
    {
        static public string Language = "en-US";
        static public float Volume = 1;
        static public float Rate = 1;

        static public bool IsAvailable()
        {
            #if WEBGL_NATIVE
            return TTSCheckAvailable();
            #else
            return true; // in a mock sense
            #endif // WEBGL_NATIVE 
        }

        static public bool Cancel()
        {
            Log.Msg("[TTS] Cancelling current utterance");
            #if WEBGL_NATIVE
            return TTSCancel();
            #else
            return true;
            #endif // WEBGL_NATIVE
        }

        static public bool Speak(string inText, float inPitch = 1)
        {
            Log.Msg("[TTS] Speaking text '{0}' with pitch {1}", inText, inPitch);
            #if WEBGL_NATIVE
            return TTSSpeak(inText, Language, Rate, Volume, inPitch);
            #else
            return true;
            #endif // WEBGL_NATIVE
        }

        static public bool IsSpeaking()
        {
            #if WEBGL_NATIVE
            return TTSIsSpeaking();
            #else
            return false;
            #endif // WEBGL_NATIVE
        }

        #region Internal

        [DllImport("__Internal")]
        static public extern bool TTSCheckAvailable();

        [DllImport("__Internal")]
        static public extern bool TTSCancel();

        [DllImport("__Internal")]
        static public extern bool TTSSpeak(string text, string lang, float rate, float volume, float pitch);

        [DllImport("__Internal")]
        static public extern bool TTSIsSpeaking();

        #endregion // Internal
    }
}