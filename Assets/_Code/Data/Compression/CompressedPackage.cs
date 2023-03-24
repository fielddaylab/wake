using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using TMPro;
using System;
using System.IO;

namespace Aqua.Compression {
    public class PackageBuilder {
        public readonly Dictionary<string, ushort> StringBank = new Dictionary<string, ushort>();
        public ushort StringCount;

        public readonly Dictionary<UnityEngine.Object, ushort> AssetBank = new Dictionary<UnityEngine.Object, ushort>();
        public readonly List<UnityEngine.Object> AssetList = new List<UnityEngine.Object>();
        public ushort AssetCount;

        public void Reset() {
            StringBank.Clear();
            StringCount = 0;
            AssetBank.Clear();
            AssetList.Clear();
            AssetCount = 0;
        }

        public ushort AddString(string str) {
            if (string.IsNullOrEmpty(str)) {
                return PackageBank.NullIndex;
            }

            return AddToBank(str, StringBank, ref StringCount);
        }

        public ushort AddAsset(UnityEngine.Object obj) {
            if (!obj) {
                return PackageBank.NullIndex;
            }

            ushort current;
            if (AssetBank.TryGetValue(obj, out current)) {
                return current;
            }

            string resourcePath = GetResourcePath(obj);
            if (!string.IsNullOrEmpty(resourcePath)) {
                ushort pathIdx = AddToBank(resourcePath, StringBank, ref StringCount);
                current = (ushort) (PackageBank.UnloadedIndex + pathIdx);
                AssetBank.Add(obj, current);
                return current;
            }

            current = AssetCount++;
            AssetBank.Add(obj, current);
            AssetList.Add(obj);
            return current;
        }

        static private ushort AddToBank<T>(T val, Dictionary<T, ushort> map, ref ushort count) {
            ushort current;
            if (!map.TryGetValue(val, out current)) {
                current = count++;
                map.Add(val, current);
            }
            return current;
        }

        static private string GetResourcePath(UnityEngine.Object obj) {
            #if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            int firstResourcesIdx = path.IndexOf("/Resources/");
            if (firstResourcesIdx >= 0) {
                path = path.Substring(firstResourcesIdx + 11);
                path = Path.ChangeExtension(path, null);
                return path;
            }
            #endif // UNITY_EDITOR

            return null;
        }
    }

    [Serializable]
    public class PackageBank {
        public const ushort NullIndex = ushort.MaxValue;
        public const ushort UnloadedIndex = (ushort) (1u << 15);

        [Multiline] public string[] StringBank;
        public UnityEngine.Object[] AssetBank;

        public PackageBank() { }

        public PackageBank(PackageBuilder compressor) {
            StringBank = new string[compressor.StringCount];
            compressor.StringBank.Keys.CopyTo(StringBank, 0);

            AssetBank = compressor.AssetList.ToArray();
        }

        public string GetString(ushort idx) {
            return idx == NullIndex ? string.Empty : StringBank[idx];
        }

        public T GetAsset<T>(ushort idx, in PrefabDecompressor decompressor) where T : UnityEngine.Object {
            if (idx == NullIndex) {
                return null;
            } if (idx >= UnloadedIndex) {
                UnityEngine.Object loaded = null;
                if (decompressor.ResourceCache != null) {
                    if (!decompressor.ResourceCache.TryGetValue(idx, out loaded)) {
                        loaded = Resources.Load<T>(StringBank[idx - UnloadedIndex]);
                        decompressor.ResourceCache.Add(idx, loaded);
                    }
                } else {
                    loaded = Resources.Load<T>(StringBank[idx - UnloadedIndex]);
                }

                return (T) loaded;
            }
            return (T) AssetBank[idx];
        }
    }
}