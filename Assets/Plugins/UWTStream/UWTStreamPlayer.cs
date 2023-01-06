using System;
using System.IO;
using UnityEngine;

namespace BeauUWT
{
    /// <summary>
    /// Audio stream player.
    /// </summary>
    public partial class UWTStreamPlayer : MonoBehaviour
    {
        static private string CachedStreamingPath;

        /// <summary>
        /// Error codes.
        /// </summary>
        public enum ErrorCode
        {
            NoError = 0,

            Aborted = 1,
            Network = 2,
            Decode = 3,
            NotSupported = 4,

            UnknownError = 255
        }

        private const ulong HiResScale = 0x100000;
        private const double HiResScale_Double = (double) HiResScale;
        private const double HiResScaleInv = 1.0 / HiResScale_Double;

        #region Inspector

        [SerializeField] private string m_StreamURL = null;
        
        [Space]
        [SerializeField] private bool m_Mute = false;
        [SerializeField] private bool m_PlayOnAwake = true;
        [SerializeField] private bool m_Loop = false;

        [SerializeField, Range(0, 1)] private float m_Volume = 1;

        #endregion // Inspector

        [NonSerialized] private bool m_PlayRequested;
        [NonSerialized] private ErrorCode m_LastError;

        /// <summary>
        /// URL to stream from.
        /// </summary>
        public string SourceURL
        {
            get { return m_StreamURL; }
            set
            {
                if (m_StreamURL == value)
                    return;

                m_StreamURL = value;
                UpdateStreamURL(value);
            }
        }

        /// <summary>
        /// If set to true, the audio stream will automatically load and start playing on awake.
        /// </summary>
        public bool PlayOnAwake
        {
            get { return m_PlayOnAwake; }
            set { m_PlayOnAwake = value; }
        }

        #region State

        /// <summary>
        /// Returns if audio playing right now
        /// </summary>
        public bool IsPlaying
        {
            get { return GetIsPlaying(); }
        }

        /// <summary>
        /// Returns the audio stream duration.
        /// </summary>
        public float Duration
        {
            get { return GetDuration(); }
        }

        /// <summary>
        /// Returns if the stream encountered an error.
        /// </summary>
        public ErrorCode GetError()
        {
            return m_LastError;
        }

        #endregion // State

        #region Properties

        /// <summary>
        /// Is the audio stream looping
        /// </summary>
        public bool Loop
        {
            get { return m_Loop; }
            set
            {
                if (m_Loop == value)
                    return;

                m_Loop = value;
                UpdateLoop(value);
            }
        }

        /// <summary>
        /// Mutes and unmutes the audio source
        /// </summary>
        public bool Mute
        {
            get { return m_Mute; }
            set
            {
                if (m_Mute == value)
                    return;

                m_Mute = value;
                UpdateMute(value);
            }
        }

        /// <summary>
        /// Playback position in seconds.
        /// </summary>
        public float Time
        {
            get { return GetTime(); }
            set { SetTime(value); }
        }

        /// <summary>
        /// Playback position in samples.
        /// </summary>
        public ulong HighResTime
        {
            get { return GetHiResTime(); }
            set { SetHiResTime(value); }
        }

        /// <summary>
        /// The volume of the audio source (0.0 to 1.0);
        /// </summary>
        public float Volume
        {
            get { return m_Volume; }
            set
            {
                value = Mathf.Clamp01(value);
                
                if (m_Volume == value)
                    return;

                m_Volume = value;
                UpdateVolume(value);
            }
        }

        #endregion // Properties

        #region Unity Events

        private void Awake()
        {
            InitializeResources();
            EnableResources();
            
            if (m_PlayOnAwake)
                Play();
        }

        private void OnEnable()
        {
            EnableResources();
        }

        private void OnDisable()
        {
            Stop();
            DisableResources();
        }

        private void OnDestroy()
        {
            CleanupResources();
        }

        #endregion // Unity Events

        #region Path Correction

        /// <summary>
        /// Sets the source url from a file path.
        /// </summary>
        public void SetURLFromPath(string inPath)
        {
            SourceURL = PathToURL(inPath);
        }

        /// <summary>
        /// Sets the source url from a file path relative to streaming assets.
        /// </summary>
        public void SetURLFromStreamingAssets(string inPath)
        {
            SourceURL = PathToURL(Path.Combine(CachedStreamingPath ?? (CachedStreamingPath = Application.streamingAssetsPath), inPath));
        }

        static private string PathToURL(string inFilePath)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.WebGLPlayer:
                    return inFilePath;

                case RuntimePlatform.WSAPlayerARM:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerX86:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "file:///" + inFilePath;

                default:
                    return "file://" + inFilePath;
            }
        }

        #endregion // Path Correction
    }
}