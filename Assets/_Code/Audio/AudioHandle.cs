using System;
using System.Collections;
using Aqua.Entity;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace AquaAudio {
    public struct AudioHandle : IEquatable<AudioHandle> {
        private AudioTrackState m_State;
        private ushort m_InstanceId;

        internal AudioHandle(AudioTrackState state, ushort instanceId) {
            m_State = state;
            m_InstanceId = instanceId;
        }

        #region Operations

        public AudioHandle Play() {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                AudioTrackState.Play(track);
            return this;
        }

        public AudioHandle Pause() {
            var track = GetTrack();
            if (track != null) {
                track.LocalProperties.Pause = true;
            }
            return this;
        }

        public AudioHandle Resume() {
            var track = GetTrack();
            if (track != null) {
                track.LocalProperties.Pause = false;
            }
            return this;
        }

        public AudioHandle Stop(float inFadeDuration = 0) {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null)) {
                AudioTrackState.Stop(track, inFadeDuration);
            }
            return this;
        }

        public float GetVolume() {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null)) {
                return track.LocalProperties.Volume;
            }
            return 1;
        }

        public AudioHandle SetVolume(float inVolume, float inDuration = 0, Curve inCurve = Curve.Linear) {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null)) {
                AudioTrackState.SetVolume(track, inVolume, inDuration, inCurve);
            }
            return this;
        }

        public float GetPitch() {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                return track.LocalProperties.Pitch;
            return 1;
        }

        public AudioHandle SetPitch(float inPitch, float inDuration = 0, Curve inCurve = Curve.Linear) {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null)) {
                AudioTrackState.SetPitch(track, inPitch, inDuration, inCurve);
            }
            return this;
        }

        public AudioHandle OverrideLoop(bool loop) {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null)) {
                AudioTrackState.SetLoop(track, loop);
            }
            return this;
        }

        #endregion // Operations

        #region Checks

        public bool Exists() {
            var track = GetTrack();
            return track != null;
        }

        public bool IsPlaying() {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                return track.State == AudioTrackState.StateId.Playing;
            return false;
        }

        public bool IsPaused() {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                return track.LocalProperties.Pause;
            return false;
        }

        public IEnumerator Wait() {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                return AudioTrackState.Wait(track, m_InstanceId);
            return null;
        }

        private AudioTrackState GetTrack() {
            if (m_InstanceId == 0)
                return null;

            if (m_State == null || m_State.InstanceId != m_InstanceId) {
                m_InstanceId = 0;
                m_State = null;
                return null;
            }

            return m_State;
        }

        public StringHash32 EventId() {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                return track.Event.Id();
            return StringHash32.Null;
        }

        #endregion // Checks

        #region Callbacks

        public AudioHandle SetLoopCallback(AudioCallback callback) {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null)) {
                track.OnLoop = callback;
            }
            return this;
        }

        #endregion // Callbacks

        #region Positions

        public AudioHandle TrackPosition(Transform position, Vector3 offset = default(Vector3)) {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null)) {
                track.PositionSource = position;
                track.SourceEntity = position ? position.GetComponent<IActiveEntity>() : null;
                track.PositionOffset = offset;
            }
            return this;
        }

        public AudioHandle SetPosition(Transform position, Vector3 offset = default(Vector3)) {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null)) {
                track.PositionSource = null;
                track.SourceEntity = null;
                if (position) {
                    offset += position.position;
                }
                track.PositionOffset = offset;
            }
            return this;
        }

        public AudioHandle SetPosition(Vector3 offset) {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null)) {
                track.PositionSource = null;
                track.SourceEntity = null;
                track.PositionOffset = offset;
            }
            return this;
        }

        #endregion // Positions

        #region Overrides

        public override bool Equals(object other) {
            if (other is AudioHandle)
                return Equals((AudioHandle)other);
            return false;
        }

        public override int GetHashCode() {
            return m_InstanceId.GetHashCode();
        }

        public bool Equals(AudioHandle other) {
            return m_InstanceId == other.m_InstanceId && m_State == other.m_State;
        }

        static public bool operator ==(AudioHandle inLeft, AudioHandle inRight) {
            return inLeft.m_InstanceId == inRight.m_InstanceId && inLeft.m_State == inRight.m_State;
        }

        static public bool operator !=(AudioHandle inLeft, AudioHandle inRight) {
            return inLeft.m_InstanceId != inRight.m_InstanceId || inLeft.m_State != inRight.m_State;
        }

        #endregion // Overrides

        #region Null

        static public AudioHandle Null { get { return default(AudioHandle); } }

        #endregion // Null

        #region Sync

        static public void Sync(AudioHandle source, AudioHandle target) {
            var sourceTrack = source.GetTrack();
            var targetTrack = target.GetTrack();
            if (sourceTrack != null && targetTrack != null) {
                AudioTrackState.SyncPlayback(sourceTrack, targetTrack);
            }
        }

        #endregion // Sync
    }
}