using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using Aqua;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauRoutine;
using Aqua.Debugging;
using BeauUtil.Debugger;
using Leaf.Runtime;
using UnityEngine.Scripting;
using BeauUWT;

namespace AquaAudio
{
    public class AudioMgr : ServiceBehaviour, ILoadable
    {
        public const int BusCount = (int) AudioBusId.LENGTH;
        private const int MaxSampleTracks = 48;
        private const int MaxStreamTracks = 16;
        private const int MaxTracks = MaxSampleTracks + MaxStreamTracks;

        #region Types

        [Serializable] private class SamplePool : SerializablePool<AudioSource> { }
        [Serializable] private class StreamPool : SerializablePool<UWTStreamPlayer> { }

        #endregion // Types

        #region Inspector

        [SerializeField] private AudioPackage m_DefaultPackage = null;
        [SerializeField] private SamplePool m_SamplePlayers = null;
        [SerializeField] private StreamPool m_StreamPlayers = null;
        [SerializeField] private float m_DefaultMusicCrossfade = 0.5f;

        #endregion // Inspector

        private readonly HashSet<AudioPackage> m_LoadedPackages = new HashSet<AudioPackage>();
        private readonly Dictionary<StringHash32, AudioEvent> m_EventLookup = new Dictionary<StringHash32, AudioEvent>();

        private readonly FixedPool<AudioTrackState> m_TrackPool = new FixedPool<AudioTrackState>(MaxTracks, Pool.DefaultConstructor<AudioTrackState>());
        private readonly RingBuffer<AudioTrackState> m_ActiveSamples = new RingBuffer<AudioTrackState>(MaxSampleTracks, RingBufferMode.Fixed);
        private readonly RingBuffer<AudioTrackState> m_ActiveStreams = new RingBuffer<AudioTrackState>(MaxStreamTracks, RingBufferMode.Fixed);

        private System.Random m_Random;
        private AudioPropertyBlock m_MasterProperties;
        private AudioPropertyBlock m_MixerProperties;
        private AudioPropertyBlock m_DebugProperties;
        private ushort m_Id;

        private AudioPropertyBlock[] m_BusMixes;

        private AudioHandle m_BGM;
        private float m_FadeMultiplier = 1;
        private Routine m_FadeMultiplierRoutine;

        #region IService

        protected override void Shutdown()
        {
            DestroyPool();

            m_LoadedPackages.Clear();
            m_EventLookup.Clear();

            SceneHelper.OnSceneLoaded -= OnGlobalSceneLoaded;

            AudioSettings.OnAudioConfigurationChanged -= OnAudioSettingsChanged;
        }

        protected override void Initialize()
        {
            m_Random = new System.Random(Environment.TickCount ^ name.GetHashCode());
            m_MasterProperties = AudioPropertyBlock.Default;
            m_MixerProperties = AudioPropertyBlock.Default;
            m_DebugProperties = AudioPropertyBlock.Default;

            m_BusMixes = new AudioPropertyBlock[BusCount - 1];
            for(int i = 0; i < m_BusMixes.Length; ++i)
                m_BusMixes[i] = AudioPropertyBlock.Default;
            
            InitPool();
            if (m_DefaultPackage != null)
                Load(m_DefaultPackage);

            SceneHelper.OnSceneLoaded += OnGlobalSceneLoaded;
            AudioSettings.OnAudioConfigurationChanged += OnAudioSettingsChanged;
        }

        #endregion // IService

        #region Unity Events

        private void LateUpdate()
        {
            UnsafeUpdate();
        }

        private unsafe void UnsafeUpdate()
        {
            AudioPropertyBlock masterProperties = m_MasterProperties;
            AudioPropertyBlock.Combine(masterProperties, m_MixerProperties, ref masterProperties);
            AudioPropertyBlock.Combine(masterProperties, m_DebugProperties, ref masterProperties);

            AudioPropertyBlock* properties = stackalloc AudioPropertyBlock[BusCount];
            properties[0] = masterProperties;

            for(int i = 0; i < m_BusMixes.Length; ++i)
                AudioPropertyBlock.Combine(masterProperties, m_BusMixes[i], ref properties[1 + i]);

            // multiply loading volume
            properties[(int) AudioBusId.SFX].Volume *= m_FadeMultiplier;
            properties[(int) AudioBusId.Ambient].Volume *= m_FadeMultiplier;
            properties[(int) AudioBusId.Voice].Volume *= m_FadeMultiplier;

            float deltaTime = Time.deltaTime;
            AudioTrackState track;

            double currentTime = AudioSettings.dspTime;

            int count = m_ActiveStreams.Count;
            for(int i = count - 1; i >= 0; i--)
            {
                track = m_ActiveStreams[i];
                if (!AudioTrackState.UpdatePlayback(track, ref properties[(int) track.Bus], deltaTime, currentTime))
                {
                    FreePlayer(track);
                    m_ActiveStreams.FastRemoveAt(i);
                }
            }

            count = m_ActiveSamples.Count;
            for(int i = count - 1; i >= 0; i--)
            {
                track = m_ActiveSamples[i];
                if (!AudioTrackState.UpdatePlayback(track, ref properties[(int) track.Bus], deltaTime, currentTime))
                {
                    FreePlayer(track);
                    m_ActiveSamples.FastRemoveAt(i);
                }
            }
        }

        #endregion // Unity Events

        #region Playback

        public AudioHandle PostEvent(StringHash32 inId, AudioPlaybackFlags inFlags = 0)
        {
            if (inId.IsEmpty)
                return AudioHandle.Null;
            
            AudioEvent evt = GetEvent(inId);
            if (evt == null)
            {
                Log.Error("[AudioMgr] No event with id '{0}' loaded", inId);
                return AudioHandle.Null;
            }

            return PostEvent(evt, inFlags);
        }

        public AudioHandle PostEvent(AudioEvent inEvent, AudioPlaybackFlags inFlags = 0)
        {
            if (!inEvent)
                return AudioHandle.Null;

            if (!inEvent.CanPlay())
                return AudioHandle.Null;

            AudioTrackState track = m_TrackPool.Alloc();
            AudioHandle handle = default;

            double currentTime = AudioSettings.dspTime;

            DebugService.Log(LogMask.Audio, "[AudioMgr] Allocating event track '{0}' (type {1})...", inEvent.Id().ToDebugString(), inEvent.Mode());

            switch(inEvent.Mode()) {
                case AudioEvent.PlaybackMode.Sample: {
                    EnsureFreeSlot(m_ActiveSamples, currentTime);
                    handle = AudioTrackState.LoadSample(track, inEvent, m_SamplePlayers.Alloc(), NextId(), m_Random);
                    m_ActiveSamples.PushBack(track);
                    break;
                }

                case AudioEvent.PlaybackMode.Stream: {
                    EnsureFreeSlot(m_ActiveStreams, currentTime);
                    handle = AudioTrackState.LoadStream(track, inEvent, m_StreamPlayers.Alloc(), NextId(), m_Random);
                    m_ActiveStreams.PushBack(track);
                    break;
                }
            }

            AudioTrackState.Preload(track);
            if ((inFlags & AudioPlaybackFlags.PreloadOnly) == 0) {
                AudioTrackState.Play(track);
            }
            return handle;
        }

        public void StopAll()
        {
            foreach(var player in m_SamplePlayers.ActiveObjects)
            {
                player.Stop();
            }
            foreach(var player in m_StreamPlayers.ActiveObjects)
            {
                player.Stop();
            }
        }

        private ushort NextId()
        {
            if (m_Id == ushort.MaxValue)
                return (m_Id = 1);
            else
                return (++m_Id);
        }

        private void EnsureFreeSlot(RingBuffer<AudioTrackState> stateList, double currentTime) {
            if (stateList.Count < stateList.Capacity) {
                return;
            }

            // free up all stopped objects
            AudioTrackState state;
            int count = stateList.Count;
            for(int i = count - 1; i >= 0; i--) {
                state = stateList[i];
                if (state.State == AudioTrackState.StateId.Stopped) {
                    FreePlayer(state);
                    stateList.FastRemoveAt(i);
                }
            }

            // if we removed anything, exit
            if (stateList.Count < count) {
                return;
            }

            Log.Warn("[AudioMgr] Not enough space available for new playback track - freeing oldest one");
            
            int highestScoreIndex = -1;
            double highestScore = 0;

            double score;
            for(int i = count - 1; i >= 0; i--) {
                state = stateList[i];

                // oldest scores higher
                score = currentTime - state.LastStartTime;

                // manual pause scores a little lower
                if (state.LocalProperties.Pause) {
                    score *= 0.5;
                }

                // audible events score lower
                if (state.LastKnownProperties.IsAudible() && state.LastKnownProperties.Volume > 0.1f) {
                    score *= 0.5f;
                }

                // looping scores lower (interrupting these is more damaging to the experience)
                if (state.Event.Looping()) {
                    score *= 0.2;
                }

                if (score > highestScore) {
                    highestScore = score;
                    highestScoreIndex = i;
                }
            }

            Assert.True(highestScoreIndex >= 0);
            state = stateList[highestScoreIndex];
            FreePlayer(state);
            stateList.FastRemoveAt(highestScoreIndex);
        }

        private void FreePlayer(AudioTrackState state) {
            switch(state.Mode) {
                case AudioEvent.PlaybackMode.Sample: {
                    AudioSource sample = state.Sample;
                    sample.Stop();
                    sample.clip = null;
                    m_SamplePlayers.Free(sample);
                    break;
                }
                case AudioEvent.PlaybackMode.Stream: {
                    UWTStreamPlayer stream = state.Stream;
                    stream.Stop();
                    stream.SourceURL = null;
                    m_StreamPlayers.Free(stream);
                    break;
                }
            }

            DebugService.Log(LogMask.Audio, "[AudioMgr] Freeing event track '{0}'", state.Event.Id().ToDebugString());
            
            AudioTrackState.Unload(state);
            m_TrackPool.Free(state);
        }

        #endregion // Playback

        #region Background Music

        public AudioHandle CurrentMusic() { return m_BGM; }

        public AudioHandle SetMusic(StringHash32 inId)
        {
            return SetMusic(inId, m_DefaultMusicCrossfade);
        }

        public AudioHandle SetMusic(AudioHandle inHandle)
        {
            return SetMusic(inHandle, m_DefaultMusicCrossfade);
        }

        public AudioHandle SetMusic(StringHash32 inId, float inCrossFade)
        {
            if (m_BGM.EventId() == inId)
                return m_BGM;

            m_BGM.Stop(inCrossFade);
            m_BGM = PostEvent(inId);
            if (inCrossFade > 0)
                m_BGM.SetVolume(0).SetVolume(1, inCrossFade);
            return m_BGM;
        }

        public AudioHandle SetMusic(AudioHandle inHandle, float inCrossFade)
        {
            if (m_BGM.EventId() == inHandle.EventId())
            {
                inHandle.Stop();
                return m_BGM;
            }

            m_BGM.Stop(inCrossFade);
            m_BGM = inHandle;
            m_BGM.Play();
            if (inCrossFade > 0)
                m_BGM.SetVolume(0).SetVolume(1, inCrossFade);
            return m_BGM;
        }

        public void StopMusic()
        {
            m_BGM.Stop(m_DefaultMusicCrossfade);
        }

        public void StopMusic(float inFade)
        {
            m_BGM.Stop(inFade);
        }

        #endregion // Background Music

        #region Volume Fade

        public void FadeOut(float inDuration)
        {
            if (inDuration <= 0)
            {
                m_FadeMultiplier = 0;
                m_FadeMultiplierRoutine.Stop();
                return;
            }

            m_FadeMultiplierRoutine.Replace(this, Tween.Float(m_FadeMultiplier, 0, (f) => m_FadeMultiplier = f, inDuration));
        }

        public void FadeIn(float inDuration)
        {
            if (inDuration <= 0)
            {
                m_FadeMultiplier = 1;
                m_FadeMultiplierRoutine.Stop();
                return;
            }

            m_FadeMultiplierRoutine.Replace(this, Tween.Float(m_FadeMultiplier, 1, (f) => m_FadeMultiplier = f, inDuration));
        }

        #endregion // Volume Fade

        #region Properties

        public ref AudioPropertyBlock Mix
        {
            get { return ref m_MixerProperties; }
        }

        internal ref AudioPropertyBlock DebugMix
        {
            get { return ref m_DebugProperties; }
        }

        public ref AudioPropertyBlock BusMix(AudioBusId inBusId)
        {
            Assert.True(inBusId >= 0 && inBusId < AudioBusId.LENGTH, "Invalid bus id '{0}'", inBusId);

            if (inBusId == AudioBusId.Master)
                return ref m_MasterProperties;

            return ref m_BusMixes[(int) inBusId - 1];
        }

        #endregion // Properties

        #region Database

        public void Load(AudioPackage inPackage)
        {
            inPackage.IncrementRefCount();

            if (!m_LoadedPackages.Add(inPackage))
                return;

            DebugService.Log(LogMask.Audio | LogMask.Loading, "[AudioMgr] Loaded package '{0}'", inPackage.name);

            foreach(var evt in inPackage.Events())
            {
                m_EventLookup.Add(evt.Id(), evt);
            }
        }

        public void Unload(AudioPackage inPackage, bool inbImmediate = false)
        {
            if (!m_LoadedPackages.Contains(inPackage))
                return;
            
            if (!inPackage.DecrementRefCount())
                return;

            if (!inbImmediate)
                return;

            UnloadPackage(inPackage);
        }

        public void UnloadUnusedPackages()
        {
            if (m_LoadedPackages.Count == 0)
                return;
            
            using(PooledSet<AudioPackage> toUnload = PooledSet<AudioPackage>.Create())
            {
                foreach(var package in m_LoadedPackages)
                {
                    if (package.ShouldUnload())
                        toUnload.Add(package);
                }

                foreach(var package in toUnload)
                {
                    UnloadPackage(package);
                }
            }
        }

        private void UnloadPackage(AudioPackage inPackage)
        {
            m_LoadedPackages.Remove(inPackage);

            DebugService.Log(LogMask.Audio | LogMask.Loading, "[AudioMgr] Unloaded package '{0}'", inPackage.name);
            
            foreach(var evt in inPackage.Events())
            {
                foreach(var player in m_ActiveSamples) {
                    if (player.Event == evt) {
                        AudioTrackState.Stop(player);
                    }
                }

                foreach(var player in m_ActiveStreams) {
                    if (player.Event == evt) {
                        AudioTrackState.Stop(player);
                    }
                }

                m_EventLookup.Remove(evt.Id());
            }
        }

        public bool HasEvent(StringHash32 inId) {
            if (inId.IsEmpty)
                return false;

            return GetEvent(inId) != null;
        }

        public AudioEvent GetEvent(StringHash32 inId)
        {
            AudioEvent evt;
            m_EventLookup.TryGetValue(inId, out evt);
            return evt;
        }

        #endregion // Database

        #region Load/Unload

        private void InitPool()
        {
            m_SamplePlayers.Reset();
            m_SamplePlayers.TryInitialize();

            m_StreamPlayers.Reset();
            m_StreamPlayers.TryInitialize();

            m_TrackPool.Prewarm();
        }

        private void DestroyPool()
        {
            m_SamplePlayers.Destroy();
            m_StreamPlayers.Destroy();

            m_Id = 0;
        }

        bool ILoadable.IsLoading() {
            for(int i = 0; i < m_ActiveStreams.Count; i++) {
                var stream = m_ActiveStreams[i].Stream;
                if (!stream.IsReady() && stream.GetError() == UWTStreamPlayer.ErrorCode.NoError) {
                    return true;
                }
            }

            return false;
        }
    
        #endregion // Load/Unload

        #region Callbacks

        private void OnAudioSettingsChanged(bool deviceWasChanged) {
            Async.InvokeAsync(RestoreAudio);
        }

        private void RestoreAudio() {
            // TODO: investigate why webgl builds crash when changing speaker mode
            foreach(var player in m_ActiveSamples) {
                AudioTrackState.Restore(player);
            }

            foreach(var player in m_ActiveStreams) {
                AudioTrackState.Restore(player);
            }
        }

        private void OnGlobalSceneLoaded(SceneBinding inScene, object inContext)
        {
            UnloadUnusedPackages();

            List<AudioListener> otherListeners = new List<AudioListener>(1);
            inScene.Scene.GetAllComponents<AudioListener>(otherListeners);

            if (otherListeners.Count > 0)
            {
                Log.Warn("[AudioMgr] Discovered {0} other AudioListeners in the scene. Please delete them as soon as possible.", otherListeners.Count);
                foreach(var listener in otherListeners)
                {
                    GameObject.Destroy(listener);
                }
            }
        }

        #endregion // Callbacks

        #region Leaf

        [LeafMember("AudioOneShot"), Preserve]
        static private void LeafOneShot(StringHash32 id) {
            Services.Audio.PostEvent(id);
        }

        [LeafMember("AudioSetBGM"), Preserve]
        static private void LeafSetBGM(StringHash32 id) {
            Services.Audio.SetMusic(id);
        }

        [LeafMember("AudioStopAll"), Preserve]
        static private void LeafStopAll() {
            Services.Audio.StopAll();
        }

        #endregion // Leaf
    }

    public enum AudioPlaybackFlags {
        PreloadOnly = 0x01
    }

    public delegate void AudioCallback(AudioHandle handle);
}