using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAudio
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
        private readonly Dictionary<string, AudioEvent> m_EventLookup = new Dictionary<string, AudioEvent>(StringComparer.Ordinal);

        private System.Random m_Random;
        private AudioPropertyBlock m_MasterProperties;
        private uint m_Id;

        private AudioHandle m_BGM;

        #region IService

        protected override void OnDeregisterService()
        {
            Debug.LogFormat("[AudioMgr] Unloading...");

            DestroyPool();

            m_LoadedPackages.Clear();
            m_EventLookup.Clear();

            Debug.LogFormat("[AudioMgr] ...done");

            base.OnDeregisterService();
        }

        protected override void OnRegisterService()
        {
            base.OnRegisterService();

            Debug.LogFormat("[AudioMgr] Initializing...");

            m_Random = new System.Random(Environment.TickCount ^ ServiceIds.Audio.GetHashCode());
            m_MasterProperties = AudioPropertyBlock.Default;
            
            InitPool();
            if (m_DefaultPackage != null)
                Load(m_DefaultPackage);

            Debug.LogFormat("[AudioMgr] ...done");
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

        public AudioHandle PostEvent(string inId)
        {
            if (string.IsNullOrEmpty(inId))
                return AudioHandle.Null;
            
            AudioEvent evt = GetEvent(inId);
            if (evt == null)
            {
                Debug.LogErrorFormat("[AudioMgr] No event with id '{0}' loaded", inId);
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

        public AudioHandle SetMusic(string inId, float inCrossFade = 0)
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
                m_EventLookup.Remove(evt.Id());
            }
        }

        public AudioEvent GetEvent(string inId)
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
    }
}