using System.Collections.Generic;
using System.Text;
using BeauUtil;
using BeauUtil.IO;
using UnityEngine;

namespace ProtoAqua
{
    #if UNITY_EDITOR
    public class ReloadableAssetCache : UnityEditor.AssetPostprocessor
    #else
    public class ReloadableAssetCache
    #endif // UNITY_EDITOR
    {
        static private readonly HotReloadBatcher s_AssetCache = new HotReloadBatcher();

        static public bool Add(IHotReloadable inReloadable)
        {
            if (s_AssetCache.Add(inReloadable))
            {
                Debug.LogFormat("[ReloadableAssetCache] Added asset '{0}'", inReloadable.Id.ToDebugString());
                return true;
            }

            return false;
        }

        static public bool Remove(IHotReloadable inReloadable)
        {
            if (s_AssetCache.Remove(inReloadable))
            {
                Debug.LogFormat("[ReloadableAssetCache] Removed asset '{0}'", inReloadable.Id.ToDebugString());
                return true;
            }

            return false;
        }

        static public void TryReloadAll(bool inbForce = false)
        {
            HashSet<HotReloadResult> results = new HashSet<HotReloadResult>();
            s_AssetCache.TryReloadAll(results, inbForce);
            LogResults(results);
        }

        static public void TryReloadTag(StringHash32 inTag, bool inbForce = false)
        {
            HashSet<HotReloadResult> results = new HashSet<HotReloadResult>();
            s_AssetCache.TryReloadTag(inTag, results, inbForce);
            LogResults(results);
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
}