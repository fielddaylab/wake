using System;
using System.Collections;
using Aqua.Debugging;
using Aqua.Entity;
using BeauRoutine;
using BeauUtil.Debugger;
using BeauUWT;
using UnityEngine;

namespace AquaAudio
{
    internal class AudioTrackState
    {
        public enum StateId : byte { Idle, PlayRequested, Playing, Paused, Stopped };

        public ushort Id;
        public ushort InstanceId;
        public StateId State;
        public AudioEvent.PlaybackMode Mode;
        public AudioPlaybackFlags Flags;

        public AudioEvent Event;
        public AudioBusId Bus;
        public AudioEmitterMode EmitterMode;

        public AudioSource Sample;
        public UWTStreamPlayer Stream;
        public Transform Position;

        public float Delay;
        public byte StopCounter;
        public ulong LastKnownTime;
        public double LastStartTime;

        public Transform PositionSource;
        public Vector3 PositionOffset;
        public IActiveEntity SourceEntity;
        public AudioCallback OnLoop;

        public AudioPropertyBlock EventProperties;
        public AudioPropertyBlock LocalProperties;
        public AudioPropertyBlock LastKnownProperties;

        public Routine VolumeChangeRoutine;
        public Routine PitchChangeRoutine;

        private Action<float> m_VolumeSetter;
        private Action<float> m_PitchSetter;
        private Action m_StopDelegate;

        public AudioTrackState() {
            m_VolumeSetter = (f) => LocalProperties.Volume = f;
            m_PitchSetter = (f) => LocalProperties.Pitch = f;
            m_StopDelegate = () => Stop(this, 0);
        }

        #region Loading

        static public AudioHandle LoadSample(AudioTrackState state, AudioEvent evt, AudioSource samplePlayer, ushort id, System.Random random) {
            state.InstanceId = id;
            state.Sample = samplePlayer;
            state.Position = samplePlayer.transform;
            state.Mode = AudioEvent.PlaybackMode.Sample;
            state.Event = evt;
            state.Bus = evt.Bus();

            evt.LoadSample(samplePlayer, random, out state.EventProperties, out state.Delay);

            state.LocalProperties = AudioPropertyBlock.Default;
            state.LastKnownTime = 0;
            state.State = StateId.Idle;
            state.Flags = 0;
            state.StopCounter = 0;
            state.PositionSource = null;
            state.PositionOffset = default(Vector3);
            state.SourceEntity = null;

            #if UNITY_EDITOR
            samplePlayer.gameObject.name = evt.name;
            #endif // UNITY_EDITOR

            return new AudioHandle(state, id);
        }

        static public AudioHandle LoadStream(AudioTrackState state, AudioEvent evt, UWTStreamPlayer streamPlayer, ushort id, System.Random random) {
            state.InstanceId = id;
            state.Stream = streamPlayer;
            state.Position = streamPlayer.transform;
            state.Mode = AudioEvent.PlaybackMode.Stream;
            state.Event = evt;
            state.Bus = evt.Bus();

            evt.LoadStream(streamPlayer, random, out state.EventProperties, out state.Delay);

            state.LocalProperties = AudioPropertyBlock.Default;
            state.LastKnownTime = 0;
            state.Delay = 0;
            state.State = StateId.Idle;
            state.Flags = 0;
            state.StopCounter = 0;
            state.PositionSource = null;
            state.PositionOffset = default(Vector3);
            state.SourceEntity = null;

            #if UNITY_EDITOR
            streamPlayer.gameObject.name = evt.name;
            #endif // UNITY_EDITOR

            return new AudioHandle(state, id);
        }

        static public void Unload(AudioTrackState state) {
            #if UNITY_EDITOR
            if (state.Sample) {
                state.Sample.gameObject.name = "[Unused Sample]";
            } else if (state.Stream) {
                state.Stream.gameObject.name = "[Unused Stream]";
            }
            #endif // UNITY_EDITOR

            state.InstanceId = 0;
            state.Sample = null;
            state.Stream = null;
            state.Position = null;
            state.Event = null;
            state.OnLoop = null;
            state.PositionSource = null;
            state.PositionOffset = default(Vector3);
            state.SourceEntity = null;
            state.Bus = AudioBusId.Master;
            state.VolumeChangeRoutine.Stop();
            state.PitchChangeRoutine.Stop();
        }

        #endregion // Loading

        #region Playback

        static public void Play(AudioTrackState state) {
            if (state.State != StateId.PlayRequested) {
                state.Sample?.Stop();
                state.Stream?.Stop();
                state.LastKnownTime = 0;

                state.State = StateId.PlayRequested;
            }
        }

        static public void Preload(AudioTrackState state) {
            switch(state.Mode) {
                case AudioEvent.PlaybackMode.Stream: {
                    state.Stream.Preload();
                    break;
                }
            }
        }

        static public void Pause(AudioTrackState state) {
            state.LocalProperties.Pause = true;
        }

        static public void Resume(AudioTrackState state) {
            state.LocalProperties.Pause = false;
        }

        static public void SetLoop(AudioTrackState state, bool loop) {
            switch(state.Mode) {
                case AudioEvent.PlaybackMode.Sample: {
                    state.Sample.loop = loop;
                    break;
                }
                case AudioEvent.PlaybackMode.Stream: {
                    state.Stream.Loop = loop;
                    break;
                }
            }
        }

        static public void Stop(AudioTrackState state, float duration = 0, Curve curve = Curve.Linear) {
            if (duration <= 0 || state.State != StateId.Playing) {
                if (state.Sample) {
                    state.Sample.Stop();
                }
                if (state.Stream) {
                    state.Stream.Stop();
                }
                state.VolumeChangeRoutine.Stop();
                state.State = StateId.Stopped;
                return;
            }

            state.VolumeChangeRoutine.Replace(Tween.Float(state.LocalProperties.Volume, 0, state.m_VolumeSetter, duration).Ease(curve).OnComplete(state.m_StopDelegate));
        }

        static public void Restore(AudioTrackState state) {
            if (state.State != StateId.Playing) {
                return;
            }

            switch(state.Mode) {
                case AudioEvent.PlaybackMode.Sample: {
                    if (state.Sample.loop || ((int) state.LastKnownTime < (state.Sample.clip.samples - state.Sample.clip.frequency))) {
                        state.Sample.timeSamples = (int) state.LastKnownTime;
                        state.Sample.Play();
                    }
                    break;
                }

                case AudioEvent.PlaybackMode.Stream: {
                    if (state.Stream.Loop) {
                        state.Stream.Unload();
                        state.Stream.Play();
                        state.Stream.HighResTime = state.LastKnownTime;
                    }
                    break;
                }
            }
        }

        #endregion // Playback

        #region Properties

        static public void SetVolume(AudioTrackState state, float volume, float duration = 0, Curve curve = Curve.Linear) {
            if (duration <= 0) {
                state.LocalProperties.Volume = volume;
                state.VolumeChangeRoutine.Stop();
                return;
            }

            state.VolumeChangeRoutine.Replace(Tween.Float(state.LocalProperties.Volume, volume, state.m_VolumeSetter, duration).Ease(curve));
        }

        static public void SetPitch(AudioTrackState state, float pitch, float duration = 0, Curve curve = Curve.Linear) {
            if (duration <= 0) {
                state.LocalProperties.Pitch = pitch;
                state.PitchChangeRoutine.Stop();
                return;
            }

            state.PitchChangeRoutine.Replace(Tween.Float(state.LocalProperties.Pitch, pitch, state.m_PitchSetter, duration).Ease(curve));
        }

        #endregion // Properties

        #region Status

        static public bool IsReady(AudioTrackState state) {
            switch(state.Mode) {
                case AudioEvent.PlaybackMode.Sample: {
                    return state.Sample.clip.loadState >= AudioDataLoadState.Loaded;
                }
                case AudioEvent.PlaybackMode.Stream: {
                    return state.Stream.GetError() == 0 && state.Stream.IsReady();
                }
                default: {
                    Assert.Fail("unknown audiotrackstate mode");
                    return false;
                }
            }
        }

        static public bool IsPlaying(AudioTrackState state) {
            return state.State == StateId.Playing;
        }

        static public IEnumerator Wait(AudioTrackState state, ushort instanceId) {
            if (instanceId == 0) {
                yield break;
            }

            while(state.InstanceId == instanceId && IsPlaying(state)) {
                yield return null;
            }
        }

        #endregion // Status

        #region Update

        static public void SyncPlayback(AudioTrackState source, AudioTrackState target) {
            if (source == target) {
                return;
            }
            if (source.Mode != target.Mode) {
                Log.Error("Cannot sync between sources with different modes");
                return;
            }
            
            ulong sourceTime = 0;
            switch(target.Mode) {
                case AudioEvent.PlaybackMode.Sample: {
                    sourceTime = (ulong) source.Sample.timeSamples;
                    target.Sample.timeSamples = (int) sourceTime;
                    break;
                }
                case AudioEvent.PlaybackMode.Stream: {
                    sourceTime = source.Stream.HighResTime;
                    target.Stream.HighResTime = sourceTime;
                    break;
                }
            }

            target.LastKnownTime = sourceTime;
        }

        static public bool UpdatePlayback(AudioTrackState state, ref AudioPropertyBlock parentSettings, float deltaTime, double currentTime, Vector3 listenerPos) {
            state.LastKnownProperties = parentSettings;
            AudioPropertyBlock.Combine(state.LastKnownProperties, state.EventProperties, ref state.LastKnownProperties);
            AudioPropertyBlock.Combine(state.LastKnownProperties, state.LocalProperties, ref state.LastKnownProperties);

            if (state.SourceEntity != null && state.SourceEntity.ActiveStatus != EntityActiveStatus.AwakeAndActive) {
                state.LastKnownProperties.Mute = true;
            }

            switch(state.State) {
                case StateId.PlayRequested: {
                    return UpdatePlayRequested(state, deltaTime, currentTime, listenerPos);
                }
                case StateId.Playing: {
                    return UpdatePlaying(state, listenerPos);
                }
                case StateId.Paused: {
                    return UpdatePaused(state, listenerPos);
                }
                case StateId.Idle: {
                    return true;
                }
                case StateId.Stopped: {
                    return false;
                }
                default: {
                    Assert.Fail("Unknown playback state {0}", state.State);
                    return false;
                }
            }
        }

        static private bool UpdatePlayRequested(AudioTrackState state, float deltaTime, double currentTime, Vector3 listenerPos) {
            if (state.LastKnownProperties.Pause) {
                return true;
            }
            
            state.Delay -= deltaTime;
            if (state.Delay <= 0 && (state.Stream == null || state.Stream.IsReady())) {
                SyncSettings(state);
                SyncPosition(state, listenerPos);
                state.State = StateId.Playing;
                state.LastStartTime = currentTime;
                state.Sample?.Play();
                state.Stream?.Play();
                SyncTime(state);
            }

            return true;
        }

        static private bool UpdatePlaying(AudioTrackState state, Vector3 listenerPos) {
            SyncSettings(state);

            if (state.LastKnownProperties.Pause) {
                state.Sample?.Pause();
                state.Stream?.Pause();
                state.StopCounter = 0;
                state.State = StateId.Paused;
                return true;
            }

            bool bIsPlaying = false;
            ulong currentTime = 0;

            switch(state.Mode) {
                case AudioEvent.PlaybackMode.Sample: {
                    bIsPlaying = state.Sample.isPlaying;
                    currentTime = (ulong) state.Sample.timeSamples;
                    break;
                }
                case AudioEvent.PlaybackMode.Stream: {
                    bIsPlaying = state.Stream.IsPlaying;
                    currentTime = state.Stream.HighResTime;
                    break;
                }
            }

            if (!bIsPlaying) {
                if (++state.StopCounter > 5) {
                    state.Sample?.Stop();
                    state.Stream?.Stop();
                    state.State = StateId.Stopped;
                    return false;
                } else {
                    return true;
                }
            } else {
                SyncPosition(state, listenerPos);
                if (state.LastKnownTime > currentTime) {
                    state.OnLoop?.Invoke(new AudioHandle(state, state.InstanceId));
                }
                state.LastKnownTime = currentTime;
                state.StopCounter = 0;
                return true;
            }
        }

        static private bool UpdatePaused(AudioTrackState state, Vector3 listenerPos) {
            if (!state.LastKnownProperties.Pause) {
                SyncSettings(state);
                SyncTime(state);
                SyncPosition(state, listenerPos);
                state.Sample?.UnPause();
                state.Stream?.UnPause();
                state.State = StateId.Playing;
            }
            return true;
        }

        static private void SyncSettings(AudioTrackState state) {
            switch(state.Mode) {
                case AudioEvent.PlaybackMode.Sample: {
                    state.Sample.volume = state.LastKnownProperties.Volume;
                    state.Sample.pitch = state.LastKnownProperties.Pitch;
                    state.Sample.mute = !state.LastKnownProperties.IsAudible();
                    break;
                }

                case AudioEvent.PlaybackMode.Stream: {
                    state.Stream.Volume = state.LastKnownProperties.Volume;
                    state.Stream.Mute = !state.LastKnownProperties.IsAudible();
                    break;
                }
            }
        }

        static private void SyncPosition(AudioTrackState state, Vector3 listenerPos) {
            AudioEmitterMode mode = state.Event.EmitterMode();
            if (mode == AudioEmitterMode.Flat || state.Mode != AudioEvent.PlaybackMode.Sample) {
                return;
            }

            Vector3 pos = state.PositionOffset;
            if (state.PositionSource) {
                pos += state.PositionSource.position;
            }

            if (mode == AudioEmitterMode.ListenerRelative) {
                pos += listenerPos;
            }

            state.Position.position = pos;
        }

        static private void SyncTime(AudioTrackState state) {
            switch(state.Mode) {
                case AudioEvent.PlaybackMode.Sample: {
                    state.Sample.timeSamples = (int) state.LastKnownTime;
                    break;
                }

                case AudioEvent.PlaybackMode.Stream: {
                    state.Stream.HighResTime = state.LastKnownTime;
                    break;
                }
            }
        }
    
        #endregion // Update
    }
}