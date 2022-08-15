using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using TMPro;
using System;

namespace Aqua.Compression {
    public class PackageBuilder {
        public readonly Dictionary<string, ushort> StringBank = new Dictionary<string, ushort>();
        public ushort StringCount;

        public readonly Dictionary<UnityEngine.Object, ushort> AssetBank = new Dictionary<UnityEngine.Object, ushort>();
        public ushort AssetCount;

        public void Reset() {
            StringBank.Clear();
            StringCount = 0;
            AssetBank.Clear();
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

            return AddToBank(obj, AssetBank, ref AssetCount);
        }

        static private ushort AddToBank<T>(T val, Dictionary<T, ushort> map, ref ushort count) {
            ushort current;
            if (!map.TryGetValue(val, out current)) {
                current = count++;
                map.Add(val, current);
            }
            return current;
        }
    }

    [Serializable]
    public class PackageBank {
        public const ushort NullIndex = ushort.MaxValue;

        [Multiline] public string[] StringBank;
        public UnityEngine.Object[] AssetBank;

        public PackageBank() { }

        public PackageBank(PackageBuilder compressor) {
            StringBank = new string[compressor.StringCount];
            compressor.StringBank.Keys.CopyTo(StringBank, 0);

            AssetBank = new UnityEngine.Object[compressor.AssetCount];
            compressor.AssetBank.Keys.CopyTo(AssetBank, 0);
        }

        public string GetString(ushort idx) {
            return idx == NullIndex ? string.Empty : StringBank[idx];
        }

        public UnityEngine.Object GetAsset(ushort idx) {
            return idx == NullIndex ? null : AssetBank[idx];
        }
    }
}