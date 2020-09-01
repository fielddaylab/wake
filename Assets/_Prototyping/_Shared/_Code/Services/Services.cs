using System.Collections.Generic;
using System.Reflection;
using BeauData;
using BeauUtil;
using ProtoAudio;
using UnityEngine;

namespace ProtoAqua
{
    public class Services
    {
        #region Cache

        static Services()
        {
            InitFields();

            Application.quitting += () => { s_Quitting = true; };
        }
        
        static private readonly HashSet<FieldInfo> s_ServiceCacheFields = new HashSet<FieldInfo>();
        static private readonly ServiceCache s_ServiceCache = new ServiceCache();
        static private bool s_Quitting;
        
        static protected T RetrieveOrFind<T>(ref T ioStorage, FourCC inId) where T : UnityEngine.Object, IService
        {
            if (object.ReferenceEquals(ioStorage, null))
            {
                if (s_Quitting)
                    return null;
                
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
            if (s_Quitting || object.ReferenceEquals(ioStorage, inValue))
                return;

            if (inValue == null)
                s_ServiceCache.Deregister(ioStorage);
            else
                s_ServiceCache.Register(inValue);

            ioStorage = inValue;
        }

        static private void InitFields()
        {
            foreach(var type in Reflect.FindDerivedTypes(typeof(Services), Reflect.FindAllAssemblies(0, Reflect.AssemblyType.DefaultNonUserMask)))
            {
                foreach(var field in type.GetFields(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (typeof(IService).IsAssignableFrom(field.FieldType))
                    {
                        s_ServiceCacheFields.Add(field);
                        Debug.LogFormat("[Services] Located cached service field '{0}::{1}'", field.DeclaringType.FullName, field.Name);
                    }
                }
            }
        }

        #endregion // Cache

        #region Accessors

        static private AudioMgr s_CachedAudioMgr;
        static public AudioMgr Audio
        {
            get { return RetrieveOrFind(ref s_CachedAudioMgr, ServiceIds.Audio); }
        }

        static private EventService s_CachedEventService;
        static public EventService Events
        {
            get { return RetrieveOrFind(ref s_CachedEventService, ServiceIds.Events); }
        }

        static private InputService s_CachedInputService;
        static public InputService Input
        {
            get { return RetrieveOrFind(ref s_CachedInputService, ServiceIds.Input); }
        }

        static private ScriptingService s_CachedScripting;
        static public ScriptingService Script
        {
            get { return RetrieveOrFind(ref s_CachedScripting, ServiceIds.Scripting); }
        }

        static private TweakMgr s_CachedTweakMgr;
        static public TweakMgr Tweaks
        {
            get { return RetrieveOrFind(ref s_CachedTweakMgr, ServiceIds.Tweaks); }
        }

        static private UIMgr s_CachedUIMgr;
        static public UIMgr UI
        {
            get { return RetrieveOrFind(ref s_CachedUIMgr, ServiceIds.CommonUI); }
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

        static public void AttemptRegister(IService inService)
        {
            s_ServiceCache.Register(inService);
        }

        static public void AttemptDeregister(IService inService)
        {
            if (s_ServiceCache.Deregister(inService))
            {
                foreach(var field in s_ServiceCacheFields)
                {
                    if (field.GetValue(null) == inService)
                    {
                        field.SetValue(null, null);
                    }
                }
            }
        }

        #endregion // Setup
    }
}