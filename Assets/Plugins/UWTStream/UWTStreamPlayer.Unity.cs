#if UNITY_EDITOR || !UNITY_WEBGL

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace BeauUWT
{
    // Unity AudioSource implementation
    public partial class UWTStreamPlayer : MonoBehaviour
    {
        private enum TimeMode
        {
            Seconds,
            HiRes
        }

        public const bool IsNative = false;

        private const ulong HeaderBufferSize = 1024 * 16;

        [NonSerialized] private AudioSource m_UnitySource;

        [NonSerialized] private UnityWebRequest m_WebRequest;
        [NonSerialized] private DownloadHandlerAudioClip m_AudioClipDownloadHandler;
        [NonSerialized] private AsyncOperation m_Operation;
        [NonSerialized] private Coroutine m_LoadCoroutine;
        [NonSerialized] private AudioClip m_StreamingClip;
        [NonSerialized] private float m_QueuedTime = 0;
        [NonSerialized] private int m_QueuedTimeHiRes = 0;
        [NonSerialized] private TimeMode m_QueuedTimeMode = TimeMode.Seconds;

        #region Resources

        private void InitializeResources()
        {
            EnsureSource();
        }

        private void EnableResources()
        {
            EnsureSource().enabled = true;
        }

        private void DisableResources()
        {
            DisposeDownload();

            if (m_UnitySource)
                m_UnitySource.enabled = false;
        }

        private void CleanupResources()
        {
            DisposeDownload();
        }

        private void DisposeDownload(ErrorCode inError = ErrorCode.NoError)
        {
            if (m_WebRequest != null)
            {
                m_WebRequest.Dispose();
                m_WebRequest = null;
            }
            if (m_AudioClipDownloadHandler != null)
            {
                m_AudioClipDownloadHandler.Dispose();
                m_AudioClipDownloadHandler = null;
            }
            if (m_StreamingClip)
            {
                AudioClip.DestroyImmediate(m_StreamingClip);
                m_UnitySource.clip = null;
            }
            if (m_LoadCoroutine != null)
            {
                StopCoroutine(m_LoadCoroutine);
                m_LoadCoroutine = null;
            }

            m_Operation = null;
            m_LastError = inError;

            if (inError != ErrorCode.NoError)
            {
                m_PlayRequested = false;
                Debug.LogErrorFormat("[UWTStreamPlayer] Playback failed due to error {0}", inError);
            }
        }

        #endregion // Resources

        private AudioSource EnsureSource()
        {
            if (object.ReferenceEquals(m_UnitySource, null))
            {
                m_UnitySource = GetComponent<AudioSource>();
                if (!m_UnitySource)
                    m_UnitySource = gameObject.AddComponent<AudioSource>();

                m_UnitySource.playOnAwake = false;
                m_UnitySource.spatialBlend = 0;

                m_UnitySource.volume = m_Volume;
                m_UnitySource.mute = m_Mute;
                m_UnitySource.loop = m_Loop;
            }

            return m_UnitySource;
        }

        private void UpdateStreamURL(string inSource)
        {
            if (!isActiveAndEnabled)
                return;

            DisposeDownload();
        }

        private bool GetIsPlaying()
        {
            return EnsureSource().isPlaying;
        }

        private float GetDuration()
        {
            if (m_StreamingClip != null)
                return m_StreamingClip.length;
            return 0;
        }

        private void UpdateLoop(bool inbLoop)
        {
            EnsureSource().loop = inbLoop;
        }

        private void UpdateMute(bool inbMute)
        {
            EnsureSource().mute = inbMute;
        }

        private float GetTime()
        {
            if (!object.ReferenceEquals(m_UnitySource, null))
                return m_UnitySource.time;
            return 0;
        }

        private void SetTime(float inTime)
        {
            if (m_PlayRequested)
            {
                m_QueuedTime = inTime;
                m_QueuedTimeMode = TimeMode.Seconds;
            }
            EnsureSource().time = inTime;
        }

        private ulong GetHiResTime()
        {
            if (!object.ReferenceEquals(m_UnitySource, null))
                return (ulong) (m_UnitySource.time * HiResScale_Double);
            return 0;
        }

        private void SetHiResTime(ulong inTime)
        {
            if (m_PlayRequested)
            {
                m_QueuedTime = (float) (inTime * HiResScaleInv);
                m_QueuedTimeMode = TimeMode.Seconds;
            }
            if (m_StreamingClip)
            {
                EnsureSource().time = (float) (inTime * HiResScaleInv);
            }
        }

        private void UpdateVolume(float inVolume)
        {
            EnsureSource().volume = inVolume;
        }

        #region Operations

        /// <summary>
        /// Pauses playing the stream.
        /// </summary>
        public void Pause()
        {
            m_PlayRequested = false;
            if (m_UnitySource != null)
                m_UnitySource.Pause();
        }

        /// <summary>
        /// Starts playing the stream.
        /// </summary>
        public void Play()
        {
            if (!isActiveAndEnabled)
                return;
            
            m_PlayRequested = true;
            m_QueuedTime = 0;
            if (m_StreamingClip != null)
            {
                var source = EnsureSource();
                source.enabled = true;
                source.Play();
            }
            else
            {
                Preload();
            }
        }

        /// <summary>
        /// Stops playing the stream.
        /// </summary>
        public void Stop()
        {
            m_PlayRequested = false;
            if (m_UnitySource)
                m_UnitySource.Stop();
        }

        /// <summary>
        /// Resumes playback of the stream.
        /// </summary>
        public void UnPause()
        {
            if (!isActiveAndEnabled)
                return;

            m_PlayRequested = true;
            m_QueuedTime = 0;
            if (m_StreamingClip != null)
                EnsureSource().UnPause();
        }

        #endregion // Operations

        #region Loading

        /// <summary>
        /// Sets up the stream.
        /// </summary>
        public void Preload()
        {
            if (!isActiveAndEnabled || string.IsNullOrEmpty(m_StreamURL) || m_WebRequest != null || m_StreamingClip)
                return;

            Debug.LogFormat("[UWTStream] Downloading from {0}", m_StreamURL);

            m_WebRequest = new UnityWebRequest(new Uri(m_StreamURL), UnityWebRequest.kHttpVerbGET);
            m_WebRequest.useHttpContinue = false;
            
            m_WebRequest.downloadHandler = m_AudioClipDownloadHandler = new DownloadHandlerAudioClip(m_StreamURL, GetAudioTypeForURL(m_StreamURL));
            m_AudioClipDownloadHandler.streamAudio = true;
            m_AudioClipDownloadHandler.compressed = true;

            m_Operation = m_WebRequest.SendWebRequest();
            m_LoadCoroutine = StartCoroutine(PreloadRoutine());
            m_Operation.completed += OnWebRequestCompleted;
        }

        /// <summary>
        /// Unloads the stream.
        /// </summary>
        public void Unload()
        {
            DisableResources();
        }

        static private AudioType GetAudioTypeForURL(string inURL)
        {
            string extension = System.IO.Path.GetExtension(inURL);

            if (extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                return AudioType.MPEG;
            if (extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
                return AudioType.OGGVORBIS;
            if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                return AudioType.WAV;

            if (extension.Equals(".acc", StringComparison.OrdinalIgnoreCase))
                return AudioType.ACC;
            if (extension.Equals(".aiff", StringComparison.OrdinalIgnoreCase))
                return AudioType.AIFF;
            if (extension.Equals(".it", StringComparison.OrdinalIgnoreCase))
                return AudioType.IT;
            if (extension.Equals(".mod", StringComparison.OrdinalIgnoreCase))
                return AudioType.MOD;
            if (extension.Equals(".mp2", StringComparison.OrdinalIgnoreCase))
                return AudioType.MPEG;
            if (extension.Equals(".s3m", StringComparison.OrdinalIgnoreCase))
                return AudioType.S3M;
            if (extension.Equals(".xm", StringComparison.OrdinalIgnoreCase))
                return AudioType.XM;
            if (extension.Equals(".xma", StringComparison.OrdinalIgnoreCase))
                return AudioType.XMA;
            if (extension.Equals(".vag", StringComparison.OrdinalIgnoreCase))
                return AudioType.VAG;

            #if UNITY_IOS && !UNITY_EDITOR
            return AudioType.AUDIOQUEUE;
            #else
            return AudioType.UNKNOWN;
            #endif // UNITY_IOS && !UNITY_EDITOR
        }

        private IEnumerator PreloadRoutine()
        {
            yield return null;

            while(m_WebRequest.downloadedBytes < HeaderBufferSize)
                yield return null;

            if (m_WebRequest.isNetworkError)
            {
                DisposeDownload(ErrorCode.Network);
                yield break;
            }

            if (m_WebRequest.isHttpError)
            {
                DisposeDownload(ErrorCode.Network);
                yield break;
            }

            if (m_LoadCoroutine != null)
            {
                StopCoroutine(m_LoadCoroutine);
                m_LoadCoroutine = null;
            }

            ReadyAudioClip();
        }

        private void OnWebRequestCompleted(AsyncOperation inOperation)
        {
            if (m_Operation != inOperation)
                return;
            
            if (m_WebRequest.isNetworkError)
            {
                DisposeDownload(ErrorCode.Network);
                return;
            }

            if (m_WebRequest.isHttpError)
            {
                DisposeDownload(ErrorCode.Network);
                return;
            }

            Debug.LogFormat("[UWTStreamPlayer] Clip is fully downloaded");
            if (m_LoadCoroutine != null)
            {
                StopCoroutine(m_LoadCoroutine);
                m_LoadCoroutine = null;
                ReadyAudioClip();
            }

            m_WebRequest.Dispose();
            m_WebRequest = null;

            m_AudioClipDownloadHandler.Dispose();
            m_AudioClipDownloadHandler = null;
        }

        private void ReadyAudioClip()
        {
            if (m_StreamingClip == null)
            {
                m_StreamingClip = m_AudioClipDownloadHandler.audioClip;
                if (m_StreamingClip == null)
                {
                    DisposeDownload(ErrorCode.UnknownError);
                    return;
                }

                m_StreamingClip.name = Path.GetFileName(m_StreamURL);
                EnsureSource().clip = m_StreamingClip;
            }

            if (m_PlayRequested)
            {
                Debug.LogFormat("[UWTStreamPlayer] Playing clip (download progress is {0})", m_WebRequest.downloadProgress);
                m_UnitySource.enabled = true;
                m_UnitySource.time = m_QueuedTime;
                m_UnitySource.Play();
                m_PlayRequested = false;
            }
        }

        /// <summary>
        /// Returns if the stream is ready.
        /// </summary>
        public bool IsReady()
        {
            return m_StreamingClip != null && m_LoadCoroutine == null && m_LastError == ErrorCode.NoError;
        }

        #endregion // Loading
    
        #if UNITY_EDITOR

        private void OnValidate()
        {
            if (!Application.IsPlaying(this))
                return;

            if (m_UnitySource == null)
                return;

            m_UnitySource.mute = m_Mute;
            m_UnitySource.volume = m_Volume;
            m_UnitySource.loop = m_Loop;
        }

        #endif // UNITY_EDITOR
    }
}

#endif // UNITY_EDITOR || !UNITY_WEBGL