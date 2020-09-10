using System;
using System.Collections;
using BeauPools;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace ProtoAudio
{
    internal class AudioPlaybackTrack : MonoBehaviour, IPooledObject<AudioPlaybackTrack>
    {
        private enum State { Idle, PlayRequested, Playing, Paused, Stopped };

        #region Inspector

        [SerializeField] private AudioSource m_Source = null;

        [SerializeField, Range(0, 1)] private float m_VolumeMultiplier = 1;
        [SerializeField, Range(-64, 64)] private float m_PitchMultiplier = 1;

        #endregion // Inspector

        [NonSerialized] private AudioEvent m_CurrentEvent;
        [NonSerialized] private uint m_CurrentId;

        [NonSerialized] private State m_CurrentState;
        [NonSerialized] private float m_CurrentDelay;
        [NonSerialized] private int m_StopCounter;

        [NonSerialized] private AudioPropertyBlock m_EventSettings;
        [NonSerialized] private AudioPropertyBlock m_LocalSettings;
        [NonSerialized] private AudioPropertyBlock m_LastKnownSettings;

        [NonSerialized] private Routine m_VolumeRoutine;
        [NonSerialized] private Routine m_PitchRoutine;

        [NonSerialized] private Action<float> m_SetVolumeDelegate;
        [NonSerialized] private Action<float> m_SetPitchDelegate;

        [NonSerialized] private Action m_StopDelegate;

        #region Operations

        /// <summary>
        /// Attempts to load the given event into this track.
        /// </summary>
        public AudioHandle TryLoad(AudioEvent inEvent, uint inId, System.Random inRandom)
        {
            if (!inEvent.Load(m_Source, inRandom, out m_EventSettings, out m_CurrentDelay))
                return AudioHandle.Null;

            m_CurrentEvent = inEvent;
            m_CurrentId = inId;

            #if UNITY_EDITOR
            gameObject.name = m_CurrentEvent.Id().ToDebugString();
            #endif // UNITY_EDITOR

            return new AudioHandle(m_CurrentId, this);
        }

        /// <summary>
        /// Queues playback.
        /// </summary>
        public void Play()
        {
            if (m_CurrentState != State.PlayRequested)
                m_Source.Stop();

            m_CurrentState = State.PlayRequested;
        }

        /// <summary>
        /// Pauses playback.
        /// </summary>
        public void Pause()
        {
            m_LocalSettings.Pause = true;
        }

        /// <summary>
        /// Resumes playback.
        /// </summary>
        public void Resume()
        {
            m_LocalSettings.Pause = false;
        }

        /// <summary>
        /// Stops playback, with an optional fade-out.
        /// </summary>
        public void Stop(float inFadeDuration = 0, Curve inCurve = Curve.Linear)
        {
            if (inFadeDuration <= 0 || m_CurrentState != State.Playing)
            {
                m_Source.Stop();
                m_CurrentState = State.Stopped;
                m_VolumeRoutine.Stop();
                return;
            }

            m_VolumeRoutine.Replace(this, Tween.Float(m_LocalSettings.Volume, 0, m_SetVolumeDelegate, inFadeDuration).Ease(inCurve).OnComplete(m_StopDelegate));
        }

        #endregion // Operations

        #region State

        /// <summary>
        /// Direct access to audio settings.
        /// </summary>
        public ref AudioPropertyBlock Settings { get { return ref m_LocalSettings; } }

        /// <summary>
        /// Sets volume over time.
        /// </summary>
        public void SetVolume(float inVolume, float inDuration = 0, Curve inCurve = Curve.Linear)
        {
            if (inDuration <= 0)
            {
                m_LocalSettings.Volume = inVolume;
                m_VolumeRoutine.Stop();
                return;
            }

            m_VolumeRoutine.Replace(this, Tween.Float(m_LocalSettings.Volume, inVolume, m_SetVolumeDelegate, inDuration).Ease(inCurve));
        }

        /// <summary>
        /// Sets pitch over time.
        /// </summary>
        public void SetPitch(float inPitch, float inDuration = 0, Curve inCurve = Curve.Linear)
        {
            if (inDuration <= 0)
            {
                m_LocalSettings.Pitch = inPitch;
                m_PitchRoutine.Stop();
                return;
            }

            m_PitchRoutine.Replace(this, Tween.Float(m_LocalSettings.Pitch, inPitch, m_SetPitchDelegate, inDuration).Ease(inCurve));
        }

        /// <summary>
        /// Returns if the track is currently playing.
        /// </summary>
        public bool IsPlaying()
        {
            return m_CurrentState == State.Playing;
        }

        #endregion // State

        #region Updates

        public bool UpdatePlayback(in AudioPropertyBlock inParentSettings, in float inDeltaTime)
        {
            m_LastKnownSettings = inParentSettings;
            AudioPropertyBlock.Combine(m_LastKnownSettings, m_EventSettings, ref m_LastKnownSettings);
            AudioPropertyBlock.Combine(m_LastKnownSettings, m_LocalSettings, ref m_LastKnownSettings);

            #if UNITY_EDITOR
            m_LastKnownSettings.Pitch *= m_PitchMultiplier;
            m_LastKnownSettings.Volume *= m_VolumeMultiplier;
            #endif // UNITY_EDITOR

            switch(m_CurrentState)
            {
                case State.PlayRequested:
                    {
                        if (!m_LastKnownSettings.Pause)
                        {
                            m_CurrentDelay -= inDeltaTime;
                            if (m_CurrentDelay <= 0)
                            {
                                SyncAudioSource();
                                m_Source.Play();
                                m_CurrentState = State.Playing;
                            }
                        }
                        break;
                    }

                case State.Playing:
                    {
                        SyncAudioSource();

                        if (m_LastKnownSettings.Pause)
                        {
                            m_Source.Pause();
                            m_StopCounter = 0;
                            m_CurrentState = State.Paused;
                            break;
                        }

                        if (!m_Source.isPlaying)
                        {
                            if (++m_StopCounter > 3)
                            {
                                m_Source.Stop();
                                m_CurrentState = State.Stopped;
                                return false;
                            }
                        }
                        else
                        {
                            m_StopCounter = 0;
                        }

                        break;
                    }

                case State.Paused:
                    {
                        if (!m_LastKnownSettings.Pause)
                        {
                            SyncAudioSource();
                            m_Source.UnPause();
                            m_CurrentState = State.Playing;
                        }
                        break;
                    }

                case State.Stopped:
                    {
                        return false;
                    }
            }

            return true;
        }

        private void SyncAudioSource()
        {
            m_Source.volume = m_LastKnownSettings.Volume;
            m_Source.pitch = m_LastKnownSettings.Pitch;
            m_Source.mute = !m_LastKnownSettings.IsAudible();
        }

        #endregion // Updates

        internal bool IsId(in uint inId)
        {
            return inId == m_CurrentId;
        }

        internal IEnumerator Wait(uint inId)
        {
            while(inId > 0 && m_CurrentId == inId && IsPlaying())
                yield return null;
        }

        internal bool IsEvent(AudioEvent inEvent)
        {
            return m_CurrentEvent == inEvent;
        }

        #region IPooledObject

        void IPooledObject<AudioPlaybackTrack>.OnAlloc()
        {
            m_LocalSettings = AudioPropertyBlock.Default;
            m_CurrentState = State.Idle;
            m_CurrentDelay = 0;
        }

        void IPooledObject<AudioPlaybackTrack>.OnConstruct(IPool<AudioPlaybackTrack> inPool)
        {
            #if UNITY_EDITOR
            gameObject.name = "Unused";
            #endif // UNITY_EDITOR

            m_Source.spatialBlend = 0;

            m_SetVolumeDelegate = (f) => m_LocalSettings.Volume = f;
            m_SetPitchDelegate = (f) => m_LocalSettings.Pitch = f;
            m_StopDelegate = () => Stop(0);
        }

        void IPooledObject<AudioPlaybackTrack>.OnDestruct()
        {
        }

        void IPooledObject<AudioPlaybackTrack>.OnFree()
        {
            m_CurrentId = 0;
            m_CurrentEvent = null;
            m_Source.Stop();
            m_Source.clip = null;
            m_VolumeRoutine.Stop();
            m_PitchRoutine.Stop();

            m_PitchMultiplier = 1;
            m_VolumeMultiplier = 1;

            #if UNITY_EDITOR
            gameObject.name = "Unused";
            #endif // UNITY_EDITOR
        }

        #endregion // IPooledObject
    }
}