using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using Aqua;
using UnityEngine;
using System.IO;
using BeauUtil.Debugger;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua
{
    #if UNITY_EDITOR
    public class ValidationUtils : UnityEditor.AssetPostprocessor
    #else
    static public class ValidationUtils
    #endif // UNITY_EDITOR
    {
        static public void EnsureUnique<T>(ref T[] ioArray) where T : UnityEngine.Object
        {
            if (ioArray == null)
                return;

            int originalLength = ioArray.Length;
            int length = originalLength;
            using(PooledSet<T> duplicateTracker = PooledSet<T>.Create())
            {
                for(int i = length - 1; i >= 0; --i)
                {
                    if (!ioArray[i] || !duplicateTracker.Add(ioArray[i]))
                        ArrayUtils.FastRemoveAt(ioArray, ref length, i);
                }
            }

            if (length != originalLength)
                Array.Resize(ref ioArray, length);
        }

        static public void EnsureUnique<T>(List<T> ioList) where T : UnityEngine.Object
        {
            if (ioList == null)
                return;

            int length = ioList.Count;
            using(PooledSet<T> duplicateTracker = PooledSet<T>.Create())
            {
                for(int i = length - 1; i >= 0; --i)
                {
                    if (!ioList[i] || !duplicateTracker.Add(ioList[i]))
                        ListUtils.FastRemoveAt(ioList, i);
                }
            }
        }

        #if UNITY_EDITOR

        static public void StripDebugInfo(ref SerializedHash32 hash) {
            #if !PRESERVE_DEBUG_SYMBOLS && !DEVELOPMENT
            hash = new SerializedHash32(hash.Hash());
            #endif // !PRESERVE_DEBUG_SYMBOLS && !DEVELOPMENT
        }

        static public void StripDebugInfo(ref TextId id) {
            #if !PRESERVE_DEBUG_SYMBOLS && !DEVELOPMENT
            id = id.Hash();
            #endif // !PRESERVE_DEBUG_SYMBOLS && !DEVELOPMENT
        }

        static public T FindAsset<T>() where T : UnityEngine.Object
        {
            string[] assetGuids = AssetDatabase.FindAssets(NameFilter(typeof(T)));
            if (assetGuids == null)
                return null;
            
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    T asset = obj as T;
                    if (asset)
                        return asset;
                }
            }

            return null;
        }

        static public T FindAsset<T>(string inName) where T : UnityEngine.Object
        {
            string[] assetGuids = AssetDatabase.FindAssets(inName + " " + NameFilter(typeof(T)));
            if (assetGuids == null)
                return null;
            
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    T asset = obj as T;
                    if (asset && asset.name == inName)
                        return asset;
                }
            }

            return null;
        }

        static public T FindAsset<T>(StringHash32 inId) where T : UnityEngine.Object
        {
            string[] assetGuids = AssetDatabase.FindAssets(NameFilter(typeof(T)));
            if (assetGuids == null)
                return null;
            
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    T asset = obj as T;
                    if (asset && asset.name == inId)
                        return asset;
                }
            }

            return null;
        }

        static public SceneAsset FindScene(string inName)
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:SceneAsset");
            if (assetGuids == null)
                return null;
            
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                var obj = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (obj.name != inName)
                    continue;
                return obj;
            }

            return null;
        }

        static public T FindPrefab<T>(string inName) where T : Component
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:GameObject");
            if (assetGuids == null)
                return null;
            
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (obj.name != inName)
                    continue;
                T component = obj.GetComponent<T>();
                if (component)
                    return component;
            }

            return null;
        }

        static public T FindPrefab<T>(string inName, params string[] inDirectories) where T : Component
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:GameObject", inDirectories);
            if (assetGuids == null)
                return null;
            
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (obj.name != inName)
                    continue;
                T component = obj.GetComponent<T>();
                if (component)
                    return component;
            }

            return null;
        }

        static public T[] FindAllAssets<T>(params string[] inDirectories) where T : UnityEngine.Object
        {
            if (inDirectories.Length == 0)
                inDirectories = null;
            
            string[] assetGuids = AssetDatabase.FindAssets(NameFilter(typeof(T)), inDirectories);
            if (assetGuids == null)
                return null;
            
            HashSet<T> assets = new HashSet<T>();
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    T asset = obj as T;
                    if (asset)
                        assets.Add(asset);
                }
            }

            T[] arr = new T[assets.Count];
            assets.CopyTo(arr);
            return arr;
        }

        static public T[] FindAllAssets<T>(Predicate<T> inPredicate, params string[] inDirectories) where T : UnityEngine.Object
        {
            if (inDirectories.Length == 0)
                inDirectories = null;
            
            string[] assetGuids = AssetDatabase.FindAssets(NameFilter(typeof(T)), inDirectories);
            if (assetGuids == null)
                return null;
            
            HashSet<T> assets = new HashSet<T>();
            for (int i = 0; i < assetGuids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    T asset = obj as T;
                    if (asset && inPredicate(asset))
                        assets.Add(asset);
                }
            }

            T[] arr = new T[assets.Count];
            assets.CopyTo(arr);
            return arr;
        }

        static public readonly Predicate<UnityEngine.Object> IgnoreTemplates = (o) => {
            return char.IsLetterOrDigit(o.name[0]);
        };

        [InitializeOnLoadMethod]
        static private void HashAllIds() {
            using(Profiling.Time("hashing all DBObject names")) {
                foreach(var asset in FindAllAssets<DBObject>()) {
                    new StringHash32(asset.name);
                }
            }
        }

        static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            foreach(var path in importedAssets) {
                new StringHash32(Path.GetFileNameWithoutExtension(path));
            }

            foreach(var path in movedAssets) {
                new StringHash32(Path.GetFileNameWithoutExtension(path));
            }
        }

        static private string NameFilter(Type type) {
            string fullname = type.FullName;
            if (fullname.StartsWith("UnityEngine.") || fullname.StartsWith("UnityEditor."))
            {
                fullname = fullname.Substring(12);
            }
            return "t:" + fullname;
        }

        #endif // UNITY_EDITOR
    }
}