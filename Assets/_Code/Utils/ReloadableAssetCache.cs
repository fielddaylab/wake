using System;
using System.Collections.Generic;
using System.Text;
using Aqua.Debugging;
using BeauUtil;
using BeauUtil.IO;
using UnityEngine;

namespace Aqua
{
    #if UNITY_EDITOR
    public class ReloadableAssetCache : UnityEditor.AssetPostprocessor
    #else
    public class ReloadableAssetCache
    #endif // UNITY_EDITOR
    {
        static private readonly HotReloadBatcher s_AssetCache = new HotReloadBatcher();

        static public event Action OnReload;

        static public bool Add(IHotReloadable inReloadable)
        {
            if (s_AssetCache.Add(inReloadable))
            {
                DebugService.Log(LogMask.Loading, "[ReloadableAssetCache] Added asset '{0}'", inReloadable.Id);
                return true;
            }

            return false;
        }

        static public bool Remove(IHotReloadable inReloadable)
        {
            if (s_AssetCache.Remove(inReloadable))
            {
                DebugService.Log(LogMask.Loading, "[ReloadableAssetCache] Removed asset '{0}'", inReloadable.Id);
                
                return true;
            }

            return false;
        }

        static public void TryReloadAll(bool inbForce = false)
        {
            HashSet<HotReloadResult> results = new HashSet<HotReloadResult>();
            s_AssetCache.TryReloadAll(results, inbForce);
            LogResults(results);
            if (results.Count > 0)
                OnReload?.Invoke();
        }

        static public void TryReloadTag(StringHash32 inTag, bool inbForce = false)
        {
            HashSet<HotReloadResult> results = new HashSet<HotReloadResult>();
            s_AssetCache.TryReloadTag(inTag, results, inbForce);
            LogResults(results);
            if (results.Count > 0)
                OnReload?.Invoke();
        }

        static private void LogResults(HashSet<HotReloadResult> inResults)
        {
            if (inResults.Count == 0)
            {
                Debug.Log("[ReloadableAssetCache] Reloaded 0 assets");
                return;
            }

            StringBuilder builder = new StringBuilder(1024);
            builder.AppendFormat("[ReloadableAssetCache] Reloaded {0} assets", inResults.Count);
            foreach(var result in inResults)
            {
                builder.Append("\n  ").Append(result.ToDebugString());
            }
            Debug.Log(builder.Flush());
        }

        #if UNITY_EDITOR

        static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!Application.isPlaying)
                return;
            
            UnityEditor.EditorApplication.delayCall += () => TryReloadAll();
        }

        #endif // UNITY_EDITOR
    }

    #if UNITY_EDITOR
    public class ReloadableAssetRef<T> where T : ScriptableObject
    {
        private T m_Asset;
        private string m_EditorPath;

        public ReloadableAssetRef(T asset)
        {
            m_Asset = asset;
            m_EditorPath = asset ? UnityEditor.AssetDatabase.GetAssetPath(m_Asset) : null;
        }

        public ReloadableAssetRef()
        {
            m_Asset = null;
            m_EditorPath = null;
        }

        public T Asset
        {
            get {
                if (m_Asset.IsReferenceDestroyed())
                {
                    m_Asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(m_EditorPath);
                }
                return m_Asset;
            }
        }

        static public implicit operator T(ReloadableAssetRef<T> assetRef)
        {
            return assetRef.Asset;
        }
    }
    #else
    public struct ReloadableAssetRef<T> where T : ScriptableObject
    {
        private T m_Asset;

        public ReloadableAssetRef(T asset)
        {
            m_Asset = asset;
        }

        public T Asset
        {
            get { return m_Asset; }
        }

        static public implicit operator T(ReloadableAssetRef<T> assetRef)
        {
            return assetRef.m_Asset;
        }
    }
    
    #endif // UNITY_EDITOR
}