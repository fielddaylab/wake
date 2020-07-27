using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAqua
{
    public class TweakMgr : MonoBehaviour, IService
    {
        #region Inspector

        [SerializeField] private TweakAsset[] m_Assets = null;

        #endregion // Inspector

        [NonSerialized] private readonly HashSet<TweakAsset> m_LoadedTweaks = new HashSet<TweakAsset>();
        [NonSerialized] private readonly Dictionary<long, TweakAsset> m_TweakMap = new Dictionary<long, TweakAsset>();

        #region IService

        void IService.OnDeregisterService()
        {
            Debug.LogFormat("[TweakMgr] Unloading...");

            foreach(var tweak in m_LoadedTweaks)
            {
                Unload(tweak, false);
            }

            m_LoadedTweaks.Clear();
            m_TweakMap.Clear();

            Debug.LogFormat("[TweakMgr] ...done");
        }

        void IService.OnRegisterService()
        {
            Debug.LogFormat("[TweakMgr] Initializing...");

            foreach(var tweak in m_Assets)
            {
                Load(tweak);
            }

            Debug.LogFormat("[TweakMgr] ...done");
        }

        FourCC IService.ServiceId()
        {
            return ServiceIds.Tweaks;
        }

        #endregion // IService

        public void Load(TweakAsset inTweaks)
        {
            if (!m_LoadedTweaks.Add(inTweaks))
                return;

            m_TweakMap.Add(GetKey(inTweaks), inTweaks);
            inTweaks.OnAdded();

            Debug.LogFormat("[TweakMgr] Loaded tweak '{0}' ({1})", inTweaks.name, inTweaks.GetType().Name);
        }

        public void Unload(TweakAsset inTweaks)
        {
            Unload(inTweaks, true);
        }

        private void Unload(TweakAsset inTweaks, bool inbRemove)
        {
            if (inbRemove && !m_LoadedTweaks.Remove(inTweaks))
                return;

            m_TweakMap.Remove(GetKey(inTweaks));
            inTweaks.OnRemoved();

            Debug.LogFormat("[TweakMgr] Unloaded tweak '{0}' ({1})", inTweaks.name, inTweaks.GetType().Name);
        }

        public T Get<T>() where T : TweakAsset
        {
            long key = GetKey(typeof(T));
            TweakAsset asset;
            m_TweakMap.TryGetValue(key, out asset);
            return asset as T;
        }

        #if UNITY_EDITOR

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            ValidationUtils.EnsureUnique(ref m_Assets);
        }

        [ContextMenu("Find All")]
        private void FindAllTweaks()
        {
            m_Assets = ValidationUtils.FindAllAssets<TweakAsset>();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        #endif // UNITY_EDITOR

        static private long GetKey(TweakAsset inAsset)
        {
            return inAsset.GetType().TypeHandle.Value.ToInt64();
        }

        static private long GetKey(Type inType)
        {
            return inType.TypeHandle.Value.ToInt64();
        }
    }
}