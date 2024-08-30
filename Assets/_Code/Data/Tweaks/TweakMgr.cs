using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using Aqua;
using UnityEngine;
using BeauUtil.Services;
using Aqua.Debugging;

using TweakIndex = BeauUtil.TypeIndex<Aqua.TweakAsset>;

namespace Aqua
{
    [ServiceDependency(typeof(AssetsService))]
    public class TweakMgr : ServiceBehaviour
    {
        #region Inspector

        [SerializeField, Required] private TweakAsset[] m_Assets = null;

        #endregion // Inspector

        [NonSerialized] private readonly HashSet<TweakAsset> m_LoadedTweaks = Collections.NewSet<TweakAsset>(16);
        [NonSerialized] private readonly TweakAsset[] m_TweakMap = new TweakAsset[TweakIndex.Capacity];

        #region IService

        protected override void Shutdown()
        {
            for(int i = 0; i < TweakIndex.Count; i++) {
                ref var tweak = ref m_TweakMap[i];
                if (tweak != null) {
                    Unload(tweak, false);
                    tweak = null;
                }
            }

            m_LoadedTweaks.Clear();
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

            m_TweakMap[TweakIndex.Get(inTweaks.GetType())] = inTweaks;
            inTweaks.OnAdded();

            DebugService.Log(LogMask.Loading, "[TweakMgr] Loaded tweak '{0}' ({1})", inTweaks.name, inTweaks.GetType().Name);
        }

        public void Unload(TweakAsset inTweaks)
        {
            Unload(inTweaks, true);
        }

        private void Unload(TweakAsset inTweaks, bool inbRemove)
        {
            if (inbRemove && !m_LoadedTweaks.Remove(inTweaks))
                return;

            m_TweakMap[TweakIndex.Get(inTweaks.GetType())] = null;
            inTweaks.OnRemoved();

            DebugService.Log(LogMask.Loading, "[TweakMgr] Unloaded tweak '{0}' ({1})", inTweaks.name, inTweaks.GetType().Name);
        }

        public T Get<T>() where T : TweakAsset
        {
            return m_TweakMap[TweakIndex.Get<T>()] as T;
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