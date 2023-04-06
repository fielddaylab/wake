#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
#define ENABLE_REVERSE_HASH
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Aqua.Journal;
using BeauUtil;
using BeauUtil.Debugger;
using TMPro;
using UnityEngine;

namespace Aqua {
    static public partial class Assets {
        static private BestiaryDB BestiaryDB;
        static private ScriptCharacterDB CharacterDB;
        static private InventoryDB InventoryDB;
        static private JobDB JobDB;
        static private ActDB ActDB;
        static private MapDB MapDB;
        static private WaterPropertyDB WaterPropertyDB;
        static private JournalDB JournalDB;

        static private Dictionary<StringHash32, ScriptableObject> s_GlobalLookup;
        static private TMP_FontAsset s_RegularFont;
        static private TMP_FontAsset s_SemiBoldFont;
        static private TMP_FontAsset s_BoldFont;
        #if UNITY_EDITOR
        static private WaterPropertyDesc[] s_EditorWaterProperties;
        #endif // UNITY_EDITOR

        static private Unsafe.ArenaHandle s_DecompressionArena;

        static internal void Assign(AssetsService inService, Unsafe.ArenaHandle inBuffer) {
            BestiaryDB = inService.Bestiary;
            CharacterDB = inService.Characters;
            InventoryDB = inService.Inventory;
            JobDB = inService.Jobs;
            ActDB = inService.Acts;
            MapDB = inService.Map;
            WaterPropertyDB = inService.WaterProp;
            JournalDB = inService.Journal;

            s_GlobalLookup = new Dictionary<StringHash32, ScriptableObject>(1600);

            Import(BestiaryDB);
            Import(CharacterDB);
            Import(InventoryDB);
            Import(JobDB);
            Import(ActDB);
            Import(MapDB);
            Import(WaterPropertyDB);
            Import(JournalDB);

            foreach (var fact in BestiaryDB.AllFacts())
                s_GlobalLookup.Add(fact.Id, fact);

            s_RegularFont = inService.RegularFont;
            s_SemiBoldFont = inService.SemiBoldFont;
            s_BoldFont = inService.BoldFont;

            s_DecompressionArena = inBuffer;

            Log.Msg("[Assets] Imported {0} assets", s_GlobalLookup.Count);
        }

        static private void Import<T>(DBObjectCollection<T> inCollection)where T : DBObject {
            foreach (var obj in inCollection.Objects) {
                s_GlobalLookup.Add(obj.Id(), obj);
            }
        }

        static public ScriptableObject Find(StringHash32 inId) {
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return inId.IsEmpty ? null : ValidationUtils.FindAsset<ScriptableObject>(inId.ToDebugString());
            }
            #endif // UNITY_EDITOR

            if (inId.IsEmpty)
                return null;

            ScriptableObject obj;
            Assert.True(s_GlobalLookup.ContainsKey(inId), "No asset with id '{0}'", inId.ToDebugString());
            s_GlobalLookup.TryGetValue(inId, out obj);
            return obj;
        }

        [MethodImpl(256)]
        static public bool Has(StringHash32 inId) {
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return !inId.IsEmpty && ValidationUtils.FindAsset<ScriptableObject>(inId.ToDebugString()) != null;
            }
            #endif // UNITY_EDITOR
            return s_GlobalLookup.ContainsKey(inId);
        }

        [MethodImpl(256)]
        static public T Find<T>(StringHash32 inId)where T : ScriptableObject {
            return (T)Find(inId);
        }

        [MethodImpl(256)]
        static public BestiaryDesc Bestiary(StringHash32 inId) {
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return ValidationUtils.FindAsset<BestiaryDesc>(inId.ToDebugString());
            }
            #endif // UNITY_EDITOR
            return BestiaryDB.Get(inId);
        }

        [MethodImpl(256)]
        static public BFBase Fact(StringHash32 inId) {
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return inId.IsEmpty ? null : ValidationUtils.FindAsset<BFBase>(inId.ToDebugString());
            }
            #endif // UNITY_EDITOR

            return BestiaryDB.Fact(inId);
        }

        [MethodImpl(256)]
        static public T Fact<T>(StringHash32 inId)where T : BFBase {
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return inId.IsEmpty ? null : ValidationUtils.FindAsset<T>(inId.ToDebugString());
            }
            #endif // UNITY_EDITOR
            return BestiaryDB.Fact<T>(inId);
        }

        [MethodImpl(256)]
        static public ScriptCharacterDef Character(StringHash32 inId) {
            return CharacterDB.Get(inId);
        }

        [MethodImpl(256)]
        static public InvItem Item(StringHash32 inId) {
            return InventoryDB.Get(inId);
        }

        [MethodImpl(256)]
        static public JournalDesc Journal(StringHash32 inId) {
            return JournalDB.Get(inId);
        }

        [MethodImpl(256)]
        static public JobDesc Job(StringHash32 inId) {
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return inId.IsEmpty ? null : ValidationUtils.FindAsset<JobDesc>(inId.ToDebugString());
            }
            #endif // UNITY_EDITOR
            return JobDB.Get(inId);
        }

        [MethodImpl(256)]
        static public ActDesc Act(uint inActIndex) {
            return ActDB.Act(inActIndex);
        }

        [MethodImpl(256)]
        static public MapDesc Map(StringHash32 inId) {
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return ValidationUtils.FindAsset<MapDesc>(inId.ToDebugString());
            }
            #endif // UNITY_EDITOR
            return MapDB.Get(inId);
        }

        [MethodImpl(256)]
        static public WaterPropertyDesc Property(WaterPropertyId inProperty) {
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                if (s_EditorWaterProperties == null) {
                    s_EditorWaterProperties = new WaterPropertyDesc[(int)WaterPropertyId.COUNT];
                    foreach (WaterPropertyDesc prop in ValidationUtils.FindAllAssets<WaterPropertyDesc>()) {
                        s_EditorWaterProperties[(int)prop.Index()] = prop;
                    }
                }

                return s_EditorWaterProperties[(int)inProperty];
            }
            #endif // UNITY_EDITOR
            return WaterPropertyDB.Property(inProperty);
        }

        static public TMP_FontAsset Font(FontWeight inWeight) {
            switch (inWeight) {
                case FontWeight.Regular:
                    return s_RegularFont;
                case FontWeight.SemiBold:
                    return s_SemiBoldFont;
                case FontWeight.Bold:
                    return s_BoldFont;
                default:
                    Assert.Fail("Unsupported font weight {0}", inWeight);
                    return s_RegularFont;
            }
        }

        [MethodImpl(256)]
        static public string NameOf(StringHash32 inAssetId) {
            #if ENABLE_REVERSE_HASH
            return inAssetId.ToDebugString();
            #else
            return Assets.Find(inAssetId)?.name;
            #endif // ENABLE_REVERSE_HASH
        }

        #region Decompression

        static private int s_DecompressCounter;

        static public unsafe byte* Decompress(byte[] inCompressed, int inOffset, int inSize, int* outDecompressedSize) {
            fixed(byte* srcPtr = inCompressed) {
                return Decompress(srcPtr + inOffset, inSize, outDecompressedSize);
            }
        }

        static public unsafe byte* Decompress(byte* inCompressed, int inCompressedSize, int* outDecompressedSize) {
            byte* alloc;
            int allocSize;
            s_DecompressCounter++;

            if (UnsafeExt.PeekCompression(inCompressed, inCompressedSize, out UnsafeExt.CompressionHeader header)) {
                allocSize = (int) header.UncompressedSize;
                alloc = (byte*) Unsafe.Alloc(s_DecompressionArena, allocSize);
                UnsafeExt.Decompress(inCompressed, inCompressedSize, alloc, allocSize, outDecompressedSize);
            } else {
                allocSize = inCompressedSize;
                *outDecompressedSize = allocSize;
                alloc = (byte*) Unsafe.Alloc(s_DecompressionArena, allocSize);
                Unsafe.Copy(inCompressed, inCompressedSize, alloc, allocSize);
            }
            return alloc;
        }

        static public unsafe void FreeDecompress(byte* inDecompressed) {
            Assert.True(s_DecompressionArena.IsValid(inDecompressed));
            s_DecompressCounter--;
            if (s_DecompressCounter == 0) {
                Unsafe.ResetArena(s_DecompressionArena);
            }
        }

        #endregion // Decompression

        #region Unloading

        static public void FullyUnload<T>(ref T asset) where T : UnityEngine.Object {
            FullyUnload(asset);
            asset = null;
        }

        static public void FullyUnload(UnityEngine.Object asset) {
            Debug.LogWarningFormat("[Assets] Manually destroying resource '{0}'", asset.name);
            #if !UNITY_EDITOR
            UnityEngine.Object.DestroyImmediate(asset, true);
            #endif // !UNITY_EDITOR
        }

        static public void FullyUnload<T>(ref T[] assets) where T : UnityEngine.Object {
            for(int i = 0; i < assets.Length; i++) {
                FullyUnload(ref assets[i]);
            }
            assets = null;
        }

        static public void FullyUnload<T>(ref List<T> assets) where T : UnityEngine.Object {
            for(int i = 0; i < assets.Count; i++) {
                FullyUnload(assets[i]);
            }
            assets.Clear();
            assets = null;
        }

        #endregion // Unloading
    }
}