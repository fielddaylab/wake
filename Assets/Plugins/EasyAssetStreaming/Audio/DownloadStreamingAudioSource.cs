#if USING_BEAUUTIL
using BeauUtil;
#endif // USING_BEAUUTIL

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace EasyAssetStreaming {
    [RequireComponent(typeof(AudioSource))]
    public sealed class DownloadStreamingAudioSource : MonoBehaviour, IStreamingAudioComponent {
        
        #region Inspector

        [SerializeField, HideInInspector] private AudioSource m_AudioSource;
        
        [SerializeField, StreamingAudioPath, FormerlySerializedAs("m_Url")] private string m_Path = null;
        [Space]
        [SerializeField] private bool m_Mute = false;
        [SerializeField] private bool m_Loop = false;
        [SerializeField, Range(0, 1)] private float m_Volume = 1;
        [SerializeField, Range(-3, 3)] private float m_Pitch = 1;

        #endregion // Inspector

        [NonSerialized] private StreamingAssetHandle m_Handle;
        [NonSerialized] private AudioClip m_LoadedClip;
        private readonly Streaming.AssetCallback m_OnUpdatedEvent;
        private bool m_PlayRequested;
        private float? m_PlayRequestedTime;

        private DownloadStreamingAudioSource() {
            m_OnUpdatedEvent = (StreamingAssetHandle id, Streaming.AssetStatus status, object asset) => {
                if (status == Streaming.AssetStatus.Loaded) {
                    m_LoadedClip = (AudioClip) asset;
                    m_AudioSource.clip = m_LoadedClip;
                    if (m_PlayRequested) {
                        Play(m_PlayRequestedTime.GetValueOrDefault());
                    }
                } else {
                    m_LoadedClip = null;
                    m_AudioSource.clip = null;
                }
                OnUpdated?.Invoke(this, status);
            };
        }

        /// <summary>
        /// Event invoked when asset status is updated.
        /// </summary>
        public event StreamingComponentEvent OnUpdated;

        #region Properties

        /// <summary>
        /// Path or URL to the audio clip.
        /// </summary>
        public string Path {
            get { return m_Path; }
            set {
                if (m_Path != value) {
                    m_Path = value;
                    if (isActiveAndEnabled) {
                        LoadClip();
                    }
                }
            }
        }

        /// <summary>
        /// Returns if the clip is fully loaded.
        /// </summary>
        public bool IsLoaded() {
            return Streaming.IsLoaded(m_Handle);
        }

        /// <summary>
        /// Returns if the clip is currently loading.
        /// </summary>
        public bool IsLoading() {
            return (Streaming.Status(m_Handle) & Streaming.AssetStatus.PendingLoad) != 0;;
        }

        /// <summary>
        /// Loaded clip.
        /// </summary>
        public AudioClip Clip {
            get { return m_LoadedClip; }
        }

        /// <summary>
        /// Duration of the loaded clip.
        /// </summary>
        public float Duration {
            get { return m_LoadedClip != null ? m_LoadedClip.length : 0; }
        }

        /// <summary>
        /// If the audio source is playing.
        /// </summary>
        public bool IsPlaying {
            get { return m_AudioSource.isPlaying; }
        }

        /// <summary>
        /// Audio source time.
        /// </summary>
        public float Time {
            get { return m_AudioSource.time; }
            set {
                m_AudioSource.time = value;
                if (m_PlayRequested) {
                    m_PlayRequestedTime = value;
                }
            }
        }

        /// <summary>
        /// Whether or not the audio source is muted.
        /// </summary>
        public bool Mute {
            get { return m_Mute; }
            set {
                if (m_Mute != value) {
                    m_Mute = value;
                    m_AudioSource.mute = value;
                }
            }
        }

        /// <summary>
        /// Whether or not the audio source should loop.
        /// </summary>
        public bool Loop {
            get { return m_Loop; }
            set {
                if (m_Loop != value) {
                    m_Loop = value;
                    m_AudioSource.loop = value;
                }
            }
        }

        /// <summary>
        /// Output volume.
        /// </summary>
        public float Volume {
            get { return m_Volume; }
            set {
                if (m_Volume != value) {
                    m_Volume = value;
                    m_AudioSource.volume = value;
                }
            }
        }

        /// <summary>
        /// Output pitch.
        /// </summary>
        public float Pitch {
            get { return m_Pitch; }
            set {
                if (m_Pitch != value) {
                    m_Pitch = value;
                    m_AudioSource.pitch = value;
                }
            }
        }

        #endregion // Properties

        #region Unity Events

        private void OnEnable() {
        }

        private void OnDisable() {
            Unload();
        }

        private void OnDestroy() {
            Unload();
        }

        #endregion // Unity Events

        #region Resources

        /// <summary>
        /// Prefetches
        /// </summary>
        public void Preload() {
            LoadClip();
        }

        private void LoadClip() {
            if (!Streaming.Audio(m_Path, ref m_Handle, m_OnUpdatedEvent)) {
                if (!m_Handle) {
                    m_AudioSource.Stop();
                    m_AudioSource.clip = null;
                    m_AudioSource.enabled = false;
                    m_LoadedClip = null;
                }
                return;
            }

            LoadSettings();

            m_AudioSource.clip = m_LoadedClip;
            m_AudioSource.enabled = m_Handle;
            m_AudioSource.Stop();

            Streaming.AssetStatus status = Streaming.Status(m_Handle);

            if ((status & Streaming.AssetStatus.Loaded) != 0) {
                if (m_PlayRequested) {
                    Play(m_PlayRequestedTime.GetValueOrDefault());
                }
            } else {
                OnUpdated?.Invoke(this, status);
            }
        }

        private void LoadSettings() {
            m_AudioSource.volume = m_Volume;
            m_AudioSource.pitch = m_Pitch;
            m_AudioSource.loop = m_Loop;
            m_AudioSource.mute = m_Mute;
        }

        /// <summary>
        /// Unloads all resources owned by the StreamingWorldTexture.
        /// </summary>
        public void Unload() {
            if (m_AudioSource) {
                m_AudioSource.Stop();
                m_AudioSource.clip = null;
            }

            if (Streaming.Unload(ref m_Handle, m_OnUpdatedEvent)) {
                m_LoadedClip = null;
                OnUpdated?.Invoke(this, Streaming.AssetStatus.Unloaded);
            }
        }

        #endregion // Resources

        #region Playback

        /// <summary>
        /// Requests playback.
        /// </summary>
        public void Play() {
            if (!isActiveAndEnabled)
                return;

            if (m_LoadedClip != null) {
                m_AudioSource.Play();
                m_PlayRequested = false;
                m_PlayRequestedTime = null;
            } else {
                m_PlayRequested = true;
            }
        }

        /// <summary>
        /// Requests playback from a specific point.
        /// </summary>
        public void Play(float time) {
            if (!isActiveAndEnabled)
                return;

            if (m_LoadedClip != null) {
                m_AudioSource.time = time;
                m_AudioSource.Play();
                m_PlayRequested = false;
                m_PlayRequestedTime = null;
            } else {
                m_PlayRequested = true;
                m_PlayRequestedTime = time;
            }
        }

        /// <summary>
        /// Pauses playback.
        /// </summary>
        public void Pause() {
            m_PlayRequested = false;
            m_AudioSource.Pause();
        }

        /// <summary>
        /// Resumes playback.
        /// </summary>
        public void UnPause() {
            m_PlayRequested = true;
            if (isActiveAndEnabled && m_LoadedClip != null) {
                if (m_PlayRequestedTime.HasValue) {
                    m_AudioSource.time = m_PlayRequestedTime.Value;
                }
                m_AudioSource.UnPause();
            }
        }

        /// <summary>
        /// Stops playback.
        /// </summary>
        public void Stop() {
            m_PlayRequested = false;
            m_PlayRequestedTime = null;
            m_AudioSource.Stop();
        }

        #endregion // Playback

        #region Editor

        #if UNITY_EDITOR

        private void Reset() {
            m_AudioSource = GetComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
        }

        private void OnValidate() {
            if (!EditorApplication.isPlaying || PrefabUtility.IsPartOfPrefabAsset(this)) {
                return;
            }

            m_AudioSource = GetComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;

            EditorApplication.delayCall += () => {
                if (!this) {
                    return;
                }

                LoadSettings();
            };
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}