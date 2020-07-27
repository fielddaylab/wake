using BeauData;
using ProtoAudio;
using UnityEngine;

namespace ProtoAqua
{
    public class Services
    {
        #region Cache

        // prevent instantiation
        private Services() { }

        static private readonly ServiceCache s_ServiceCache = new ServiceCache();
        
        static protected T Retrieve<T>(ref T ioStorage, FourCC inId) where T : IService
        {
            if (object.ReferenceEquals(ioStorage, null))
            {
                ioStorage = s_ServiceCache.Get<T>(inId);
            }
            return ioStorage;
        }
        
        static protected T RetrieveOrFind<T>(ref T ioStorage, FourCC inId) where T : UnityEngine.Object, IService
        {
            if (object.ReferenceEquals(ioStorage, null))
            {
                ioStorage = s_ServiceCache.Get<T>(inId);
                if (object.ReferenceEquals(ioStorage, null))
                {
                    T objectInScene = UnityEngine.Object.FindObjectOfType<T>();
                    if (!object.ReferenceEquals(objectInScene, null))
                    {
                        ioStorage = objectInScene;
                        s_ServiceCache.Register(objectInScene);
                    }
                }
            }

            return ioStorage;
        }

        static protected void Store<T>(ref T ioStorage, T inValue) where T : IService
        {
            if (object.ReferenceEquals(ioStorage, inValue))
                return;

            if (inValue == null)
                s_ServiceCache.Deregister(ioStorage);
            else
                s_ServiceCache.Register(inValue);

            ioStorage = inValue;
        }

        #endregion // Cache

        #region Accessors

        static private AudioMgr s_CachedAudioMgr;
        static public AudioMgr Audio
        {
            get { return RetrieveOrFind(ref s_CachedAudioMgr, ServiceIds.Audio); }
            set { Store(ref s_CachedAudioMgr, value); }
        }

        static private TweakMgr s_CachedTweakMgr;
        static public TweakMgr Tweaks
        {
            get { return RetrieveOrFind(ref s_CachedTweakMgr, ServiceIds.Tweaks); }
            set { Store(ref s_CachedTweakMgr, value); }
        }
    
        #endregion // Accessors

        #region Setup

        static public void AutoSetup(GameObject inRoot)
        {
            foreach(var service in inRoot.GetComponentsInChildren<IService>())
            {
                s_ServiceCache.Register(service);
            }
        }

        #endregion // Setup
    }
}