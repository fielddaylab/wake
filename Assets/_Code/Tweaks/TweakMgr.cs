using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using Aqua;
using UnityEngine;

namespace Aqua
{
    public class TweakMgr : ServiceBehaviour
    {
        #region Inspector

        [SerializeField, Required] private TweakAsset[] m_Assets = null;

        #endregion // Inspector

        [NonSerialized] private readonly HashSet<TweakAsset> m_LoadedTweaks = new HashSet<TweakAsset>();
        [NonSerialized] private readonly Dictionary<Type, TweakAsset> m_TweakMap = new Dictionary<Type, TweakAsset>();

        #region IService

        protected override void Shutdown()
        {
            foreach(var tweak in m_LoadedTweaks)
            {
                Unload(tweak, false);
            }

            m_LoadedTweaks.Clear();
            m_TweakMap.Clear();
        }

        protected override void Initialize()
        {
            foreach(var tweak in m_Assets)
            {
                Load(tweak);
            }
        }

        #endregion // IService

        public void Load(TweakAsset inTweaks)
        {
            if (!m_LoadedTweaks.Add(inTweaks))
                return;

            m_TweakMap.Add(inTweaks.GetType(), inTweaks);
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

            m_TweakMap.Remove(inTweaks.GetType());
            inTweaks.OnRemoved();

            Debug.LogFormat("[TweakMgr] Unloaded tweak '{0}' ({1})", inTweaks.name, inTweaks.GetType().Name);
        }

        public T Get<T>() where T : TweakAsset
        {
            TweakAsset asset;
            m_TweakMap.TryGetValue(typeof(T), out asset);
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
    }
}