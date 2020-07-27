using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAudio
{
    public class AudioMgr : MonoBehaviour, IService
    {
        #region Types

        [Serializable] private class AudioPool : SerializablePool<AudioPlaybackTrack> { }

        #endregion // Types

        #region Inspector

        [SerializeField] private AudioPackage m_DefaultPackage = null;
        [SerializeField] private AudioPool m_Pool;

        #endregion // Inspector

        private readonly HashSet<AudioPackage> m_LoadedPackages = new HashSet<AudioPackage>();
        private readonly Dictionary<string, AudioEvent> m_EventLookup = new Dictionary<string, AudioEvent>();

        private System.Random m_Random;
        private AudioPropertyBlock m_MasterProperties;
        private uint m_Id;

        #region IService

        void IService.OnDeregisterService()
        {
            Debug.LogFormat("[AudioMgr] Unloading...");

            DestroyPool();

            m_LoadedPackages.Clear();
            m_EventLookup.Clear();

            Debug.LogFormat("[AudioMgr] ...done");
        }

        void IService.OnRegisterService()
        {
            Debug.LogFormat("[AudioMgr] Initializing...");

            m_Random = new System.Random(Environment.TickCount ^ ServiceIds.Audio.GetHashCode());
            m_MasterProperties = AudioPropertyBlock.Default;
            
            InitPool();
            if (m_DefaultPackage != null)
                Load(m_DefaultPackage);

            Debug.LogFormat("[AudioMgr] ...done");
        }

        FourCC IService.ServiceId()
        {
            return ServiceIds.Audio;
        }

        #endregion // IService

        #region Unity Events

        private void OnEnable()
        {
            Services.Audio = this;
        }

        private void OnDisable()
        {
            if (Services.Audio == this)
                Services.Audio = null;
        }

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

        public void Unload(AudioPackage inPackage)
        {
            if (!m_LoadedPackages.Contains(inPackage))
                return;
            
            if (!inPackage.DecrementRefCount())
                return;

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