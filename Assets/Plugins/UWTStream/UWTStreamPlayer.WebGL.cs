#if UNITY_WEBGL && !UNITY_EDITOR

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BeauUWT
{
    // WebGL implementation
    public partial class UWTStreamPlayer : MonoBehaviour
    {
        public const bool IsNative = true;

        [NonSerialized] private uint m_WebHandle;
        [NonSerialized] private Coroutine m_LoadCoroutine;
        static private bool s_Initialized;

        #region Resources

        private void InitializeResources()
        {
            if (!s_Initialized)
            {
                UWTStreamInit(16);
                s_Initialized = true;
            }
        }

        private void EnableResources()
        {
        }

        private void DisableResources()
        {
            FreeHandle();
        }

        private void CleanupResources()
        {
            FreeHandle();
        }

        private void FreeHandle(ErrorCode inError = ErrorCode.NoError)
        {
            if (m_WebHandle != 0)
            {
                UWTStreamFree(m_WebHandle);
                m_WebHandle = 0;
            }

            if (m_LoadCoroutine != null)
            {
                StopCoroutine(m_LoadCoroutine);
                m_LoadCoroutine = null;
            }

            m_LastError = inError;
            
            if (inError != ErrorCode.NoError)
            {
                Debug.LogErrorFormat("[UWTStreamPlayer] Playback failed due to error {0}", inError);
            }
        }

        #endregion // Resources

        private void UpdateStreamURL(string inSource)
        {
            if (!isActiveAndEnabled)
                return;

            FreeHandle();
        }

        private bool GetIsPlaying()
        {
            return m_WebHandle != 0 && UWTStreamIsPlaying(m_WebHandle);
        }

        private float GetDuration()
        {
            if (m_WebHandle != 0)
                return UWTStreamGetDuration(m_WebHandle);
            return 0;
        }

        private void UpdateLoop(bool inbLoop)
        {
            if (m_WebHandle != 0)
                UWTStreamSetLoop(m_WebHandle, inbLoop);
        }

        private void UpdateMute(bool inbMute)
        {
            if (m_WebHandle != 0)
                UWTStreamSetMute(m_WebHandle, inbMute);
        }

        private float GetTime()
        {
            if (m_WebHandle != 0)
                return UWTStreamGetPosition(m_WebHandle);
            return 0;
        }

        private void SetTime(float inTime)
        {
            if (m_WebHandle != 0)
                UWTStreamSetPosition(m_WebHandle, inTime);
        }

        private ulong GetHiResTime()
        {
            return (ulong) (GetTime() * HiResScale_Double);
        }

        private void SetHiResTime(ulong inTime)
        {
            SetTime((float) (inTime * HiResScaleInv));
        }

        private void UpdateVolume(float inVolume)
        {
            if (m_WebHandle != 0)
                UWTStreamSetVolume(m_WebHandle, inVolume);
        }

        #region Operations

        /// <summary>
        /// Pauses playing the stream.
        /// </summary>
        public void Pause()
        {
            m_PlayRequested = false;
            if (m_WebHandle != 0)
                UWTStreamPause(m_WebHandle);
        }

        /// <summary>
        /// Starts playing the stream.
        /// </summary>
        public void Play()
        {
            if (!isActiveAndEnabled)
                return;
            
            m_PlayRequested = true;
            if (m_WebHandle != 0 && m_LoadCoroutine == null)
                UWTStreamPlay(m_WebHandle, false);
            else
                Preload();
        }

        /// <summary>
        /// Stops playing the stream.
        /// </summary>
        public void Stop()
        {
            m_PlayRequested = false;
            if (m_WebHandle != 0)
                UWTStreamStop(m_WebHandle);
        }

        /// <summary>
        /// Resumes playback of the stream.
        /// </summary>
        public void UnPause()
        {
            if (!isActiveAndEnabled)
                return;

            m_PlayRequested = true;
            if (m_WebHandle != 0 && m_LoadCoroutine == null)
                UWTStreamPlay(m_WebHandle, false);
        }

        #endregion // Operations

        #region Loading

        /// <summary>
        /// Sets up the stream.
        /// </summary>
        public void Preload()
        {
            if (!isActiveAndEnabled || string.IsNullOrEmpty(m_StreamURL) || m_WebHandle != 0)
                return;
            
            m_WebHandle = UWTStreamAlloc(m_StreamURL);
            UWTStreamSetLoop(m_WebHandle, m_Loop);
            UWTStreamSetMute(m_WebHandle, m_Mute);
            UWTStreamSetVolume(m_WebHandle, m_Volume);

            m_LoadCoroutine = StartCoroutine(PreloadWatch());
        }

        private IEnumerator PreloadWatch()
        {
            while(m_WebHandle != 0)
            {
                yield return null;

                ErrorCode error = (ErrorCode) UWTStreamGetError(m_WebHandle);
                if (error != ErrorCode.NoError)
                {
                    FreeHandle(error);
                    yield break;
                }

                bool bReady = UWTStreamIsReady(m_WebHandle);
                if (bReady)
                {
                    StopCoroutine(m_LoadCoroutine);
                    m_LoadCoroutine = null;

                    if (m_PlayRequested)
                        Play();
                    
                    yield break;
                }
            }
        }

        /// <summary>
        /// Unloads the stream.
        /// </summary>
        public void Unload()
        {
            DisableResources();
        }

        /// <summary>
        /// Returns if the stream is ready.
        /// </summary>
        public bool IsReady()
        {
            return m_WebHandle != 0 && UWTStreamIsReady(m_WebHandle);
        }

        #endregion // Loading

        #region Internal

        [DllImport("__Internal")]
        static public extern bool UWTStreamInit(int poolSize);

        [DllImport("__Internal")]
        static public extern uint UWTStreamAlloc(string path);

        [DllImport("__Internal")]
        static public extern bool UWTStreamFree(uint id);

        [DllImport("__Internal")]
        static public extern bool UWTStreamIsReady(uint id);

        [DllImport("__Internal")]
        static public extern int UWTStreamGetError(uint id);

        [DllImport("__Internal")]
        static public extern float UWTStreamGetDuration(uint id);

        [DllImport("__Internal")]
        static public extern bool UWTStreamIsPlaying(uint id);

        [DllImport("__Internal")]
        static public extern bool UWTStreamGetMute(uint id);

        [DllImport("__Internal")]
        static public extern bool UWTStreamSetMute(uint id, bool mute);

        [DllImport("__Internal")]
        static public extern bool UWTStreamGetLoop(uint id);

        [DllImport("__Internal")]
        static public extern bool UWTStreamSetLoop(uint id, bool loop);

        [DllImport("__Internal")]
        static public extern float UWTStreamGetVolume(uint id);

        [DllImport("__Internal")]
        static public extern bool UWTStreamSetVolume(uint id, float volume);

        [DllImport("__Internal")]
        static public extern float UWTStreamGetPosition(uint id);

        [DllImport("__Internal")]
        static public extern bool UWTStreamSetPosition(uint id, float position);

        [DllImport("__Internal")]
        static public extern bool UWTStreamPlay(uint id, bool reset);

        [DllImport("__Internal")]
        static public extern bool UWTStreamPause(uint id);

        [DllImport("__Internal")]
        static public extern bool UWTStreamStop(uint id);

        #endregion // Internal
    }
}

#endif // UNITY_WEBGL && !UNITY_EDITOR