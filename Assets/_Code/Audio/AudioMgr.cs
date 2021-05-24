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

namespace AquaAudio
{
    public class AudioMgr : ServiceBehaviour
    {
        public const int BusCount = (int) AudioBusId.LENGTH;

        #region Types

        [Serializable] private class AudioPool : SerializablePool<AudioPlaybackTrack> { }

        #endregion // Types

        #region Inspector

        [SerializeField] private AudioPackage m_DefaultPackage = null;
        [SerializeField] private AudioPool m_Pool = null;
        [SerializeField] private float m_DefaultMusicCrossfade = 0.5f;

        #endregion // Inspector

        private readonly HashSet<AudioPackage> m_LoadedPackages = new HashSet<AudioPackage>();
        private readonly Dictionary<StringHash32, AudioEvent> m_EventLookup = new Dictionary<StringHash32, AudioEvent>();
        private readonly RingBuffer<AudioPlaybackTrack> m_Playing = new RingBuffer<AudioPlaybackTrack>(32, RingBufferMode.Expand);

        private System.Random m_Random;
        private AudioPropertyBlock m_MasterProperties;
        private AudioPropertyBlock m_MixerProperties;
        private AudioPropertyBlock m_DebugProperties;
        private uint m_Id;

        private AudioPropertyBlock[] m_BusMixes;

        private AudioHandle m_BGM;
        private float m_FadeMultiplier;
        private Routine m_FadeMultiplierRoutine;

        #region IService

        protected override void Shutdown()
        {
            DestroyPool();

            m_LoadedPackages.Clear();
            m_EventLookup.Clear();

            SceneHelper.OnSceneLoaded -= OnGlobalSceneLoaded;
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
        }

        #endregion // IService

        #region Unity Events

        private void LateUpdate()
        {
            UnsafeUpdate();
        }

        private unsafe void UnsafeUpdate()
        {
            int count = m_Playing.Count;
            
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
            AudioPlaybackTrack track;

            for(int i = count - 1; i >= 0; --i)
            {
                track = m_Playing[i];
                if (!track.UpdatePlayback(ref properties[(int) track.BusId()], deltaTime))
                {
                    m_Pool.Free(track);
                    m_Playing.FastRemoveAt(i);
                }
            }
        }

        #endregion // Unity Events

        #region Playback

        public bool HasEvent(string inId)
        {
            if (string.IsNullOrEmpty(inId))
                return false;

            return GetEvent(inId) != null;
        }

        public AudioHandle PostEvent(StringHash32 inId)
        {
            if (inId.IsEmpty)
                return AudioHandle.Null;
            
            AudioEvent evt = GetEvent(inId);
            if (evt == null)
            {
                Log.Error("[AudioMgr] No event with id '{0}' loaded", inId);
                return AudioHandle.Null;
            }

            if (!evt.CanPlay())
                return AudioHandle.Null;

            AudioPlaybackTrack track = m_Pool.Alloc();
            AudioHandle handle = track.TryLoad(evt, NextId(), m_Random);
            if (handle != AudioHandle.Null)
            {
                m_Playing.PushBack(track);
                track.Play();
            }
            else
            {
                m_Pool.Free(track);
            }

            return handle;
        }

        public AudioHandle PostEvent(AudioEvent inEvent)
        {
            if (!inEvent)
                return AudioHandle.Null;

            if (!inEvent.CanPlay())
                return AudioHandle.Null;

            AudioPlaybackTrack track = m_Pool.Alloc();
            AudioHandle handle = track.TryLoad(inEvent, NextId(), m_Random);
            if (handle != AudioHandle.Null)
            {
                m_Playing.PushBack(track);
                track.Play();
            }
            else
            {
                m_Pool.Free(track);
            }

            return handle;
        }

        public void StopAll()
        {
            foreach(var player in m_Pool.ActiveObjects)
            {
                player.Stop();
            }
        }

        private uint NextId()
        {
            if (m_Id == uint.MaxValue)
                return (m_Id = 1);
            else
                return (++m_Id);
        }

        #endregion // Playback

        #region Background Music

        public AudioHandle CurrentMusic() { return m_BGM; }

        public AudioHandle SetMusic(StringHash32 inId)
        {
            return SetMusic(inId, m_DefaultMusicCrossfade);
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
                foreach(var player in m_Pool.ActiveObjects)
                {
                    if (player.IsEvent(evt))
                    {
                        player.Stop();
                    }
                }
                m_EventLookup.Remove(evt.Id());
            }
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
            m_Pool.Reset();

            if (m_Pool.IsInitialized())
                return;
            
            m_Pool.Initialize();
        }

        private void DestroyPool()
        {
            m_Pool.Destroy();
            m_Id = 0;
        }
    
        #endregion // Load/Unload

        #region Callbacks

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
    }
}