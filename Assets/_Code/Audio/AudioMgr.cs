using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using Aqua;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauRoutine;

namespace AquaAudio
{
    public class AudioMgr : ServiceBehaviour
    {
        #region Types

        [Serializable] private class AudioPool : SerializablePool<AudioPlaybackTrack> { }

        #endregion // Types

        #region Inspector

        [SerializeField] private AudioPackage m_DefaultPackage = null;
        [SerializeField] private AudioPool m_Pool = null;

        #endregion // Inspector

        private readonly HashSet<AudioPackage> m_LoadedPackages = new HashSet<AudioPackage>();
        private readonly Dictionary<StringHash32, AudioEvent> m_EventLookup = new Dictionary<StringHash32, AudioEvent>();

        private System.Random m_Random;
        private AudioPropertyBlock m_MasterProperties;
        private AudioPropertyBlock m_MixerProperties;
        private AudioPropertyBlock m_DebugProperties;
        private uint m_Id;

        private AudioHandle m_BGM;
        private float m_FadeMultiplier;
        private Routine m_FadeMultiplierRoutine;

        #region IService

        protected override void OnDeregisterService()
        {
            DestroyPool();

            m_LoadedPackages.Clear();
            m_EventLookup.Clear();

            SceneHelper.OnSceneLoaded -= OnGlobalSceneLoaded;
        }

        protected override void OnRegisterService()
        {
            m_Random = new System.Random(Environment.TickCount ^ ServiceIds.Audio.GetHashCode());
            m_MasterProperties = AudioPropertyBlock.Default;
            m_MixerProperties = AudioPropertyBlock.Default;
            m_DebugProperties = AudioPropertyBlock.Default;
            
            InitPool();
            if (m_DefaultPackage != null)
                Load(m_DefaultPackage);

            SceneHelper.OnSceneLoaded += OnGlobalSceneLoaded;
        }

        public override FourCC ServiceId()
        {
            return ServiceIds.Audio;
        }

        #endregion // IService

        #region Unity Events

        private void LateUpdate()
        {
            using(PooledList<AudioPlaybackTrack> playingTracks = PooledList<AudioPlaybackTrack>.Create())
            {
                playingTracks.AddRange(m_Pool.ActiveObjects);
                int count = playingTracks.Count;
                
                AudioPropertyBlock properties = m_MasterProperties;
                AudioPropertyBlock.Combine(properties, m_MixerProperties, ref properties);
                AudioPropertyBlock.Combine(properties, m_DebugProperties, ref properties);

                // multiply loading volume
                properties.Volume *= m_FadeMultiplier;

                float deltaTime = Time.deltaTime;
                AudioPlaybackTrack track;

                for(int i = count - 1; i >= 0; --i)
                {
                    track = playingTracks[i];
                    if (!track.UpdatePlayback(properties, deltaTime))
                    {
                        m_Pool.Free(track);
                    }
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
                Debug.LogErrorFormat("[AudioMgr] No event with id '{0}' loaded", inId.ToDebugString());
                return AudioHandle.Null;
            }

            if (!evt.CanPlay())
                return AudioHandle.Null;

            AudioPlaybackTrack track = m_Pool.Alloc();
            AudioHandle handle = track.TryLoad(evt, NextId(), m_Random);
            if (handle != AudioHandle.Null)
            {
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

        public AudioHandle SetMusic(StringHash32 inId, float inCrossFade = 0)
        {
            m_BGM.Stop(inCrossFade);
            m_BGM = PostEvent(inId);
            if (inCrossFade > 0)
                m_BGM.SetVolume(0).SetVolume(1, inCrossFade);
            return m_BGM;
        }

        public void StopMusic(float inFade = 0)
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

        #endregion // Properties

        #region Database

        public void Load(AudioPackage inPackage)
        {
            inPackage.IncrementRefCount();

            if (!m_LoadedPackages.Add(inPackage))
                return;

            Debug.LogFormat("[AudioMgr] Loaded package '{0}'", inPackage.name);

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

            Debug.LogFormat("[AudioMgr] Unloaded package '{0}'", inPackage.name);
            
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
                Debug.LogWarningFormat("[AudioMgr] Discovered {0} other AudioListeners in the scene. Please delete them as soon as possible.", otherListeners.Count);
                foreach(var listener in otherListeners)
                {
                    GameObject.Destroy(listener);
                }
            }
        }

        #endregion // Callbacks
    }
}