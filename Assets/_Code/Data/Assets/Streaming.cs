using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using BeauData;
using BeauUtil.Debugger;
using System.IO;
using System;
using BeauUtil.IO;
using Aqua.Debugging;
using System.Collections;

namespace Aqua {
    #if UNITY_EDITOR
    public class Streaming : UnityEditor.AssetPostprocessor {
    #else
    static public class Streaming {
    #endif // UNITY_EDITOR

        static private readonly Color32[] TextureLoadingBytes = new Color32[] {
            Color.black, Color.white, Color.white, Color.black
        };

        #if UNITY_EDITOR

        [UnityEditor.InitializeOnLoadMethod]
        static private void EditorInitialize() {
            UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChange;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            AppDomain.CurrentDomain.DomainUnload += (e, o) => UnloadAll();
        }

        static private void PlayModeStateChange(UnityEditor.PlayModeStateChange stateChange) {
            if (stateChange != UnityEditor.PlayModeStateChange.ExitingEditMode && stateChange != UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                return;
            }

            UnloadAll();
        }

        static private void OnSceneOpened(UnityEngine.SceneManagement.Scene _, UnityEditor.SceneManagement.OpenSceneMode __) {
            if (UnityEditor.EditorApplication.isPlaying) {
                return;
            }
            
            UnloadUnusedSync();
        }

        #endif // UNITY_EDITOR

        private enum AssetType : ushort {
            Texture,
            Audio
        }

        private class AssetMeta {
            public AssetType Type;
            public ushort RefCount;
            public IFuture Loader;
            #if UNITY_EDITOR
            public HotReloadableFileProxy Proxy;
            #endif // UNITY_EDITOR
        }

        static private readonly Dictionary<StringHash32, Texture2D> s_Textures = new Dictionary<StringHash32, Texture2D>();
        static private readonly Dictionary<StringHash32, AudioClip> s_AudioClips = new Dictionary<StringHash32, AudioClip>();
        static private readonly Dictionary<StringHash32, AssetMeta> s_Metas = new Dictionary<StringHash32, AssetMeta>();
        static private readonly Dictionary<int, StringHash32> s_ReverseLookup = new Dictionary<int, StringHash32>();
        static private readonly RingBuffer<StringHash32> s_UnloadQueue = new RingBuffer<StringHash32>();
        static private uint s_LoadCount = 0;
        #if UNITY_EDITOR
        static private readonly HotReloadBatcher s_Batcher = new HotReloadBatcher();
        #endif // UNITY_EDITOR

        #region Textures

        static public bool Texture(string url, ref Texture2D texture) {
            if (string.IsNullOrEmpty(url)) {
                return Unload(ref texture);
            }

            StringHash32 id = url;
            Texture2D loadedTexture;
            AssetMeta meta = GetTextureMeta(id, url, out loadedTexture);

            if (texture != loadedTexture) {
                Dereference(texture);
                texture = loadedTexture;
                meta.RefCount++;
                s_UnloadQueue.FastRemove(id);
                return true;
            }

            return false;
        }

        static private AssetMeta GetTextureMeta(StringHash32 id, string url, out Texture2D texture) {
            Texture2D loadedTexture;
            AssetMeta meta;
            if (!s_Metas.TryGetValue(id, out meta)) {
                meta = new AssetMeta();

                DebugService.Log(LogMask.Loading, "[Streaming] Loading streamed texture '{0}'...", id);
                
                meta.Type = AssetType.Texture;
                #if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying) {
                    loadedTexture = LoadTexture_Editor(id, url, out meta.Proxy);
                    s_Batcher.Add(meta.Proxy);
                } else
                #endif // UNITY_EDITOR
                {
                    loadedTexture = LoadTextureAsync(id, url, out meta.Loader);
                }

                s_Metas[id] = meta;
                s_Textures[id] = loadedTexture;
                s_ReverseLookup[loadedTexture.GetInstanceID()] = id;
            } else {
                loadedTexture = s_Textures[id];
            }

            texture = loadedTexture;
            return meta;
        }

        static public bool Unload(ref Texture2D texture) {
            if (!texture.IsReferenceNull()) {
                Dereference(texture);
                texture = null;
                return true;
            }

            return false;
        }

        #if UNITY_EDITOR

        static private Texture2D LoadTexture_Editor(StringHash32 id, string url, out HotReloadableFileProxy proxy) {
            string correctedPath = PathUtility.StreamingAssetsPath(url);
            if (File.Exists(correctedPath)) {
                byte[] bytes = File.ReadAllBytes(correctedPath);
                Texture2D texture = new Texture2D(1, 1);
                texture.name = url;
                texture.hideFlags = HideFlags.DontSave;
                texture.filterMode = GetTextureFilterMode(url);
                texture.wrapMode = GetTextureWrapMode(url);
                texture.LoadImage(bytes, false);
                DebugService.Log(LogMask.Loading, "[Streaming] ...finished loading (sync) '{0}'", id);
                proxy = new HotReloadableFileProxy(correctedPath, (p, s) => {
                    if (s == HotReloadOperation.Modified) {
                        texture.LoadImage(File.ReadAllBytes(p), false);

                        DebugService.Log(LogMask.Loading, "[Streaming] Texture '{0}' reloaded", id);
                    } else {
                        texture.filterMode = FilterMode.Point;
                        texture.Resize(2, 2);
                        texture.SetPixels32(TextureLoadingBytes);
                        texture.Apply(false, true);

                        DebugService.Log(LogMask.Loading, "[Streaming] Texture '{0}' was deleted", id);
                    }
                });
                return texture;
            } else {
                Log.Error("[Streaming] Failed to load texture from '{0}'", url);
                Texture2D texture = CreatePlaceholderTexture(url, true);
                proxy = null;
                return texture;
            }
        }

        #endif // UNITY_EDITOR

        static private Texture2D LoadTextureAsync(StringHash32 id, string url, out IFuture loader) {
            Texture2D texture = CreatePlaceholderTexture(url, false);

            string correctedUrl = PathUtility.PathToURL(PathUtility.StreamingAssetsPath(url));
            loader = Future.Download.Bytes(correctedUrl)
                .OnComplete((bytes) => OnTextureDownloadCompleted(id, bytes, url))
                .OnFail(() => OnTextureDownloadFail(id, correctedUrl));
            s_LoadCount++;
            return texture;
        }

        static private void OnTextureDownloadCompleted(StringHash32 id, byte[] source, string url) {
            Texture2D dest = s_Textures[id];
            dest.LoadImage(source, true);
            dest.filterMode = GetTextureFilterMode(url);
            dest.wrapMode = GetTextureWrapMode(url);
            s_LoadCount --;
            DebugService.Log(LogMask.Loading, "[Streaming] ...finished loading (async) '{0}'", id);
        }

        static private void OnTextureDownloadFail(StringHash32 id, string url) {
            Log.Error("[Streaming] Failed to load texture '{0}' from '{1}", id, url);
            s_LoadCount--;
        }

        static private FilterMode GetTextureFilterMode(string url) {
            if (url.Contains("[pt]")) {
                return FilterMode.Point;
            } else {
                return FilterMode.Bilinear;
            }
        }

        static private TextureWrapMode GetTextureWrapMode(string url) {
            if (url.Contains("[wrap]")) {
                return TextureWrapMode.Repeat;
            } else {
                return TextureWrapMode.Clamp;
            }
        }

        static private Texture2D CreatePlaceholderTexture(string name, bool final) {
            Texture2D texture;
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.SetPixels32(TextureLoadingBytes);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, final);
            texture.hideFlags = HideFlags.DontSave;
            return texture;
        }

        #endregion // Textures

        #region Management

        static private void Dereference(UnityEngine.Object inInstance) {
            if (!inInstance) {
                return;
            }

            int instanceId = inInstance.GetInstanceID();
            StringHash32 id;
            if (!s_ReverseLookup.TryGetValue(instanceId, out id)) {
                Log.Warn("[Streaming] No asset metadata found for {0}'", inInstance);
                return;
            }

            AssetMeta meta;
            if (s_Metas.TryGetValue(id, out meta)) {
                if (meta.RefCount > 0) {
                    meta.RefCount--;
                    if (meta.RefCount == 0) {
                        s_UnloadQueue.PushBack(id);
                    }
                }
            }
        }

        static public bool IsLoading() {
            return s_LoadCount > 0;
        }

        #if UNITY_EDITOR

        static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (Application.isPlaying)
                return;
            
            s_Batcher.TryReloadAll();
        }

        #endif // UNITY_EDITOR

        #if UNITY_EDITOR

        static internal void UnloadUnusedSync() {
            StringHash32 id;
            AssetMeta meta;
            UnityEngine.Object resource = null;
            while(s_UnloadQueue.TryPopFront(out id)) {
                meta = s_Metas[id];
                if (meta.RefCount > 0) {
                    break;
                }

                s_Metas.Remove(id);
                if (meta.Loader != null) {
                    if (!meta.Loader.IsDone()) {
                        s_LoadCount--;
                    }

                    meta.Loader.Dispose();
                    meta.Loader = null;
                }

                #if UNITY_EDITOR
                if (meta.Proxy != null) {
                    s_Batcher.Remove(meta.Proxy);
                    meta.Proxy.Dispose();
                }
                #endif // UNITY_EDITOR

                switch(meta.Type) {
                    case AssetType.Texture: {
                            resource = s_Textures[id];
                            s_Textures.Remove(id);
                            break;
                        }

                    case AssetType.Audio: {
                            resource = s_AudioClips[id];
                            
                            s_AudioClips.Remove(id);
                            break;
                        }
                }

                s_ReverseLookup.Remove(resource.GetInstanceID());
                UnityEngine.Object.DestroyImmediate(resource);
                DebugService.Log(LogMask.Loading, "[Streaming] Unloaded streamed asset '{0}'", id);

                resource = null;
            }
        }

        #endif // UNITY_EDITOR

        static internal AsyncHandle UnloadUnusedAsync() {
            return Async.Schedule(UnloadUnusedAsyncJob());
        }

        static private IEnumerator UnloadUnusedAsyncJob() {
            StringHash32 id;
            AssetMeta meta;
            UnityEngine.Object resource = null;
            while(s_UnloadQueue.TryPopFront(out id)) {
                meta = s_Metas[id];
                if (meta.RefCount > 0) {
                    break;
                }

                s_Metas.Remove(id);
                if (meta.Loader != null) {
                    if (!meta.Loader.IsDone()) {
                        s_LoadCount--;
                    }

                    meta.Loader.Dispose();
                    meta.Loader = null;
                }

                #if UNITY_EDITOR
                if (meta.Proxy != null) {
                    s_Batcher.Remove(meta.Proxy);
                    meta.Proxy.Dispose();
                }
                #endif // UNITY_EDITOR

                switch(meta.Type) {
                    case AssetType.Texture: {
                            resource = s_Textures[id];
                            s_Textures.Remove(id);
                            break;
                        }

                    case AssetType.Audio: {
                            resource = s_AudioClips[id];
                            
                            s_AudioClips.Remove(id);
                            break;
                        }
                }

                s_ReverseLookup.Remove(resource.GetInstanceID());
                UnityEngine.Object.Destroy(resource);
                DebugService.Log(LogMask.Loading, "[Streaming] Unloaded streamed asset '{0}'", id);

                resource = null;
                yield return null;
            }
        }

        static internal void UnloadAll() {
            #if UNITY_EDITOR
            foreach(var texture in s_Textures.Values) {
                if (Application.isPlaying) {
                    Texture2D.Destroy(texture);
                } else {
                    Texture2D.DestroyImmediate(texture);
                }
            }
            foreach(var audio in s_AudioClips.Values) {
                if (Application.isPlaying) {
                    AudioClip.Destroy(audio);
                } else {
                    AudioClip.DestroyImmediate(audio);
                }
            }
            #else
            foreach(var texture in s_Textures.Values) {
                Texture2D.Destroy(texture);
            }
            foreach(var audio in s_AudioClips.Values) {
                AudioClip.Destroy(audio);
            }
            #endif // UNITY_EDITOR

            s_ReverseLookup.Clear();
            s_Textures.Clear();
            s_AudioClips.Clear();
            s_Metas.Clear();
            s_UnloadQueue.Clear();

            #if UNITY_EDITOR
            s_Batcher.Dispose();
            #endif // UNITY_EDITOR
        }
    
        #endregion // Management
    }
}