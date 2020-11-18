using System;
using System.Collections;
using BeauRoutine;

namespace AquaAudio
{
    public struct AudioHandle : IEquatable<AudioHandle>
    {
        private uint m_Id;
        private AudioPlaybackTrack m_Track;

        internal AudioHandle(uint inId, AudioPlaybackTrack inTrack)
        {
            m_Id = inId;
            m_Track = inTrack;
        }

        #region Operations

        public AudioHandle Play()
        {
            GetTrack()?.Play();
            return this;
        }

        public AudioHandle Pause()
        {
            GetTrack()?.Pause();
            return this;
        }

        public AudioHandle Resume()
        {
            GetTrack()?.Resume();
            return this;
        }

        public AudioHandle Stop(float inFadeDuration = 0)
        {
            GetTrack()?.Stop(inFadeDuration);
            return this;
        }

        public float GetVolume()
        {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                return track.Settings.Volume;
            return 1;
        }

        public AudioHandle SetVolume(float inVolume, float inDuration = 0, Curve inCurve = Curve.Linear)
        {
            GetTrack()?.SetVolume(inVolume, inDuration, inCurve);
            return this;
        }

        public float GetPitch()
        {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                return track.Settings.Pitch;
            return 1;
        }

        public AudioHandle SetPitch(float inPitch, float inDuration = 0, Curve inCurve = Curve.Linear)
        {
            GetTrack()?.SetPitch(inPitch, inDuration, inCurve);
            return this;
        }

        #endregion // Operations

        #region Checks

        public bool Exists()
        {
            var track = GetTrack();
            return track != null;
        }

        public bool IsPlaying()
        {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                return track.IsPlaying();
            return false;
        }

        public bool IsPaused()
        {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                return track.Settings.Pause;
            return false;
        }

        public IEnumerator Wait()
        {
            var track = GetTrack();
            if (!object.ReferenceEquals(track, null))
                return track.Wait(m_Id);
            return null;
        }

        private AudioPlaybackTrack GetTrack()
        {
            if (m_Id == 0)
                return null;

            if (m_Track == null || !m_Track.IsId(m_Id))
            {
                m_Id = 0;
                m_Track = null;
                return null;
            }

            return m_Track;
        }

        #endregion // Checks

        #region Overrides

        public override bool Equals(object other)
        {
            if (other is AudioHandle)
                return Equals((AudioHandle) other);
            return false;
        }

        public override int GetHashCode()
        {
            return m_Id.GetHashCode();
        }

        public bool Equals(AudioHandle other)
        {
            return m_Id == other.m_Id;
        }

        static public bool operator==(AudioHandle inLeft, AudioHandle inRight)
        {
            return inLeft.m_Id == inRight.m_Id;
        }

        static public bool operator!=(AudioHandle inLeft, AudioHandle inRight)
        {
            return inLeft.m_Id != inRight.m_Id;
        }

        #endregion // Overrides

        #region Null

        static private readonly AudioHandle s_NullHandle = default(AudioHandle);

        static public AudioHandle Null { get { return s_NullHandle; } }

        #endregion // Null
    }
}