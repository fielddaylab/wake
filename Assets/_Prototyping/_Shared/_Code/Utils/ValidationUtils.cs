using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace ProtoAqua
{
    static public class ValidationUtils
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

        static public T[] FindAllAssets<T>(params string[] inDirectories) where T : UnityEngine.Object
        {
            if (inDirectories.Length == 0)
                inDirectories = null;
            
            string[] assetGuids = AssetDatabase.FindAssets("t:" + typeof(T).FullName, inDirectories);
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

        #endif // UNITY_EDITOR
    }
}