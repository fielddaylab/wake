using System;
using System.Collections.Generic;
using System.IO;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;

namespace Aqua.Editor
{
    static public class SceneArtProfiler {
        public struct AssetStats {
            public string RootName;
            public Dictionary<int, int> UseCount;
            public long AssetSize;
        }

        public struct AssetMetaDB {
            public Dictionary<int, AssetMeta> Metadata;
        }

        public struct AssetStatDB {
            public AssetStats Total;
            public AssetMetaDB Metadata;
            public List<AssetStats> ByRoot;
        }

        private struct IdWithRefCount {
            public int Id;
            public long Size;
            public int Count;

            public IdWithRefCount(KeyValuePair<int, int> kv, AssetMetaDB metaDB) {
                Id = kv.Key;
                Count = kv.Value;
                Size = metaDB.Metadata[Id].Size;
            }
        }

        public struct AssetMeta {
            public UnityEngine.Object Asset;
            public int Id;
            public string Path;
            public long Size;
            public int[] SubAssets;
        }

        [MenuItem("Aqualab/DEBUG/Generate Art Stats")]
        static private void GenerateStats() {
            string currentPath = EditorSceneManager.GetActiveScene().path;
            List<SceneBinding> allScenes = new List<SceneBinding>(SceneHelper.AllBuildScenes(true));
            AssetStatDB statDB = new AssetStatDB();
            statDB.Total.UseCount = new Dictionary<int, int>();
            statDB.Metadata.Metadata = new Dictionary<int, AssetMeta>();
            statDB.ByRoot = new List<AssetStats>(allScenes.Count);

            try
            {
                using(Profiling.Time("Profiling scenes")) {
                    Log.Msg("[SceneArtProfiler] Profiling scenes");
                    for(int i = 0; i < allScenes.Count; i++)
                    {
                        SceneBinding scene = allScenes[i];
                        AssetStats forScene = new AssetStats();
                        forScene.RootName = scene.Name;
                        forScene.UseCount = new Dictionary<int, int>();

                        bool cancel = EditorUtility.DisplayCancelableProgressBar("Profiling scene", string.Format("{0} ({1}/{2})", scene.Name, i + 1, allScenes.Count), (float) i / allScenes.Count);
                        if (cancel) {
                            return;
                        }
                        Log.Msg("[SceneArtProfiler] Loading '{0}'", scene.Path);
                        EditorSceneManager.OpenScene(scene.Path, OpenSceneMode.Single);
                        SceneProcessor.DEBUGBakeSceneForManifest();

                        foreach(var go in scene.Scene.GetRootGameObjects()) {
                            foreach(var meshFilter in go.GetComponentsInChildren<MeshFilter>(true)) {
                                if (!meshFilter.GetComponent<StreamingQuadTexture>()) {
                                    Reference(meshFilter.sharedMesh, ref forScene, ref statDB);
                                }
                            }

                            foreach(var renderer in go.GetComponentsInChildren<Renderer>(true)) {
                                foreach(var material in renderer.sharedMaterials) {
                                    Reference(material, ref forScene, ref statDB);
                                }
                            }

                            foreach(var skinned in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                                Reference(skinned.sharedMesh, ref forScene, ref statDB);
                            }

                            foreach(var spriteRenderer in go.GetComponentsInChildren<SpriteRenderer>(true)) {
                                Reference(spriteRenderer.sprite, ref forScene, ref statDB);
                            }
                        }

                        statDB.ByRoot.Add(forScene);
                    }

                    var allResourcePrefabs = Resources.LoadAll<GameObject>("");
                    for(int i = 0; i < allResourcePrefabs.Length; i++)
                    {
                        GameObject prefab = allResourcePrefabs[i];
                        AssetStats forPrefab = new AssetStats();
                        forPrefab.RootName = prefab.name;
                        forPrefab.UseCount = new Dictionary<int, int>();

                        bool cancel = EditorUtility.DisplayCancelableProgressBar("Profiling prefab", string.Format("{0} ({1}/{2})", prefab.name, i + 1, allResourcePrefabs.Length), (float) i / allResourcePrefabs.Length);
                        if (cancel) {
                            return;
                        }
                        Log.Msg("[SceneArtProfiler] Loading '{0}'", prefab.name);

                        GameObject instantiated = null;

                        try {
                            instantiated = GameObject.Instantiate(prefab);
                            foreach(var meshFilter in instantiated.GetComponentsInChildren<MeshFilter>(true)) {
                                if (!meshFilter.GetComponent<StreamingQuadTexture>()) {
                                    Reference(meshFilter.sharedMesh, ref forPrefab, ref statDB);
                                }
                            }

                            foreach(var renderer in instantiated.GetComponentsInChildren<Renderer>(true)) {
                                foreach(var material in renderer.sharedMaterials) {
                                    Reference(material, ref forPrefab, ref statDB);
                                }
                            }

                            foreach(var skinned in instantiated.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                                Reference(skinned.sharedMesh, ref forPrefab, ref statDB);
                            }

                            foreach(var spriteRenderer in instantiated.GetComponentsInChildren<SpriteRenderer>(true)) {
                                Reference(spriteRenderer.sprite, ref forPrefab, ref statDB);
                            }

                            statDB.ByRoot.Add(forPrefab);
                        } finally {
                            if (instantiated) {
                                GameObject.DestroyImmediate(instantiated);
                            }
                        }
                    }

                    using(var writer = new StreamWriter(File.Open("SceneStats.txt", FileMode.Create))) {
                        writer.Write("BY ROOT\n----\n\n");
                        foreach(var sceneStats in statDB.ByRoot) {
                            writer.Write("Root: ");
                            writer.Write(sceneStats.RootName);
                            writer.Write(" (");
                            writer.Write(EditorUtility.FormatBytes(sceneStats.AssetSize));
                            writer.Write(")\n");
                            foreach(var kv in sceneStats.UseCount) {
                                AssetMeta meta = GetMeta(kv.Key, statDB.Metadata);
                                writer.Write(" - ");
                                writer.Write(meta.Path);
                                writer.Write(" (");
                                writer.Write(EditorUtility.FormatBytes(meta.Size));
                                writer.Write(") - Referenced ");
                                writer.Write(kv.Value);
                                writer.Write(" time(s)");
                                writer.Write("\n");
                            }

                            writer.Write("\n");
                        }

                        writer.Write("\nNUM ROOTS REFERENCING ASSETS\n---\n\n");
                        
                        IdWithRefCount[] refs = ArrayUtils.MapFrom(statDB.Total.UseCount, (kv) => new IdWithRefCount(kv, statDB.Metadata));
                        Array.Sort(refs, (a, b) => {
                            int countDiff = b.Count - a.Count;
                            if (countDiff == 0) {
                                return (int) (b.Size - a.Size);
                            }
                            return countDiff;
                        });

                        int lastRefCount = refs[0].Count;
                        foreach(var r in refs) {
                            if (lastRefCount != r.Count) {
                                lastRefCount = r.Count;
                                writer.Write("\n");
                            }
                            AssetMeta meta = GetMeta(r.Id, statDB.Metadata);
                            writer.Write(" - ");
                            writer.Write(meta.Path);
                            writer.Write(" (");
                            writer.Write(EditorUtility.FormatBytes(meta.Size));
                            writer.Write(") - Referenced in ");
                            writer.Write(r.Count);
                            writer.Write(" root(s)");
                            writer.Write("\n");
                        }

                        writer.Write("\nBY SIZE\n---\n\n");
                        Array.Sort(refs, (a, b) => {
                            return (int) (b.Size - a.Size);
                        });

                        foreach(var r in refs) {
                            AssetMeta meta = GetMeta(r.Id, statDB.Metadata);
                            writer.Write(" - ");
                            writer.Write(meta.Path);
                            writer.Write(" (");
                            writer.Write(EditorUtility.FormatBytes(meta.Size));
                            writer.Write(") - Referenced in ");
                            writer.Write(r.Count);
                            writer.Write(" root(s)");
                            writer.Write("\n");
                        }

                        writer.Write("TOTAL ASSET SIZE: ");
                        writer.Write(EditorUtility.FormatBytes(statDB.Total.AssetSize));
                    }
                }

                EditorUtility.OpenWithDefaultApp("SceneStats.txt");
            }
            finally
            {
                if (!UnityEditorInternal.InternalEditorUtility.inBatchMode) {
                    EditorSceneManager.OpenScene(currentPath, OpenSceneMode.Single);
                }
                EditorUtility.ClearProgressBar();
            }
        }

        static private void Reference(UnityEngine.Object asset, ref AssetStats currentStats, ref AssetStatDB db) {
            if (!asset) {
                return;
            }

            AssetMeta meta = GetMeta(asset, db.Metadata);
            if (Increment(currentStats.UseCount, meta.Id)) {
                if (Increment(db.Total.UseCount, meta.Id)) {
                    db.Total.AssetSize += meta.Size;
                }
                currentStats.AssetSize += meta.Size;
            }

            if (meta.SubAssets != null) {
                for(int i = 0; i < meta.SubAssets.Length; i++) {
                    Reference(meta.SubAssets[i], ref currentStats, ref db);
                }
            }
        }

        static private void Reference(int assetId, ref AssetStats currentStats, ref AssetStatDB db) {
            AssetMeta meta = GetMeta(assetId, db.Metadata);
            if (Increment(currentStats.UseCount, meta.Id)) {
                if (Increment(db.Total.UseCount, meta.Id)) {
                    db.Total.AssetSize += meta.Size;
                }
                currentStats.AssetSize += meta.Size;
            }

            if (meta.SubAssets != null) {
                for(int i = 0; i < meta.SubAssets.Length; i++) {
                    Reference(meta.SubAssets[i], ref currentStats, ref db);
                }
            }
        }

        static private bool Increment(Dictionary<int, int> useCount, int key) {
            bool had = useCount.TryGetValue(key, out int counter);
            useCount[key] = counter + 1;
            return !had;
        }

        static private AssetMeta GetMeta(UnityEngine.Object asset, AssetMetaDB db) {
            int id = asset.GetInstanceID();
            if (!db.Metadata.TryGetValue(id, out AssetMeta meta)) {
                meta.Asset = asset;
                meta.Id = id;
                meta.Path = GetPath(asset);
                meta.Size = Profiler.GetRuntimeMemorySizeLong(asset);
                db.Metadata.Add(id, meta);
                if (GatherSubAssets(ref meta, db) > 0) {
                    db.Metadata[id] = meta;
                }
            }
            return meta;
        }

        static private AssetMeta GetMeta(int id, AssetMetaDB db) {
            if (!db.Metadata.TryGetValue(id, out AssetMeta meta)) {
                throw new ArgumentException("id");
            }
            return meta;
        }

        static private unsafe int GatherSubAssets(ref AssetMeta meta, AssetMetaDB db) {
            Material m = meta.Asset as Material;
            if (m != null) {
                int[] texturePropIds = m.GetTexturePropertyNameIDs();
                int* textureRefBuffer = stackalloc int[texturePropIds.Length];
                int textureCount = 0;

                for(int i = 0; i < texturePropIds.Length; i++) {
                    Texture t = m.GetTexture(texturePropIds[i]);
                    if (t != null) {
                        int textureId = GetMeta(t, db).Id;
                        textureRefBuffer[textureCount++] = textureId;
                    }
                }

                if (textureCount > 0) {
                    meta.SubAssets = new int[textureCount];
                    Unsafe.CopyArray(textureRefBuffer, textureCount, meta.SubAssets);
                }

                return textureCount;
            }

            Sprite spr = meta.Asset as Sprite;
            if (spr != null) {
                int textureId = GetMeta(spr.texture, db).Id;
                meta.SubAssets = new int[1];
                meta.SubAssets[0] = textureId;
                return 1;
            }

            return 0;
        }

        static private string GetPath(UnityEngine.Object asset) {
            if (AssetDatabase.Contains(asset)) {
                if (AssetDatabase.IsSubAsset(asset)) {
                    return string.Format("{0}::{1} ({2})", AssetDatabase.GetAssetPath(asset), asset.name, asset.GetType().Name);
                } else {
                    return AssetDatabase.GetAssetPath(asset);
                }
            } else {
                return asset.name;
            }
        }
    }
}