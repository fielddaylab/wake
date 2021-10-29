#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

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
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            AppDomain.CurrentDomain.DomainUnload += (e, o) => UnloadAll();
        }

        static private void PlayModeStateChange(UnityEditor.PlayModeStateChange stateChange) {
            if (stateChange != UnityEditor.PlayModeStateChange.ExitingEditMode && stateChange != UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                return;
            }

            UnloadAll();
        }

        static private void OnPrefabStageClosing(UnityEditor.Experimental.SceneManagement.PrefabStage _) {
            UnityEditor.EditorApplication.delayCall += UnloadUnusedSync;
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

        public enum AssetStatus : byte {
            Unloaded = 0,
            Invalid = 0x01,

            PendingUnload = 0x02,
            PendingLoad = 0x04,
            Loaded = 0x08,
            Error = 0x10,
        }

        private class AssetMeta {
            public AssetType Type;
            public AssetStatus Status;
            public ushort RefCount;
            public long Size;
            public long LastModifiedTS;

            public UnityWebRequest Loader;
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
        static private long s_TextureMemoryUsage = 0;
        static private AsyncHandle s_UnloadHandle;
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
                meta.LastModifiedTS = CurrentTimestamp();
                meta.Status &= ~AssetStatus.PendingUnload;
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

                Log.Msg( "[Streaming] Loading streamed texture '{0}'...", id);
                
                meta.Type = AssetType.Texture;
                meta.Status = AssetStatus.PendingLoad;
                #if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying) {
                    loadedTexture = LoadTexture_Editor(id, url, meta);
                    s_Batcher.Add(meta.Proxy);
                } else
                #endif // UNITY_EDITOR
                {
                    loadedTexture = LoadTextureAsync(id, url, meta);
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

        static private Texture2D LoadTexture_Editor(StringHash32 id, string url, AssetMeta meta) {
            string correctedPath = StreamingPath(url);
            if (File.Exists(correctedPath)) {
                byte[] bytes = File.ReadAllBytes(correctedPath);
                Texture2D texture = new Texture2D(1, 1);
                texture.name = url;
                texture.hideFlags = HideFlags.DontSave;
                texture.filterMode = GetTextureFilterMode(url);
                texture.wrapMode = GetTextureWrapMode(url);
                texture.LoadImage(bytes, false);
                Log.Msg("[Streaming] ...finished loading (sync) '{0}'", id);
                meta.Proxy = new HotReloadableFileProxy(correctedPath, (p, s) => {
                    if (s == HotReloadOperation.Modified) {
                        texture.LoadImage(File.ReadAllBytes(p), false);

                        Log.Msg("[Streaming] Texture '{0}' reloaded", id);
                    } else {
                        texture.filterMode = FilterMode.Point;
                        texture.Resize(2, 2);
                        texture.SetPixels32(TextureLoadingBytes);
                        texture.Apply(false, true);

                        meta.Status = AssetStatus.Error;
                        Log.Msg("[Streaming] Texture '{0}' was deleted", id);
                    }
                });
                meta.Status = AssetStatus.Loaded;
                return texture;
            } else {
                Log.Error("[Streaming] Failed to load texture from '{0}'", url);
                Texture2D texture = CreatePlaceholderTexture(url, true);
                meta.Proxy = null;
                meta.Status = AssetStatus.Error;
                return texture;
            }
        }

        #endif // UNITY_EDITOR

        static private Texture2D LoadTextureAsync(StringHash32 id, string url, AssetMeta meta) {
            Texture2D texture = CreatePlaceholderTexture(url, false);

            string correctedUrl = PathToURL(StreamingPath(url));
            var request = meta.Loader = new UnityWebRequest(correctedUrl, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerBuffer();
            var sent = request.SendWebRequest();
            sent.completed += (r) => HandleTextureUWRFinished(id, url, meta, request);
            s_LoadCount++;
            return texture;
        }

        static private void HandleTextureUWRFinished(StringHash32 id, string url, AssetMeta meta, UnityWebRequest request) {
            if (request.isNetworkError || request.isHttpError) {
                OnTextureDownloadFail(id, url, meta);
                request.Dispose();
            } else {
                OnTextureDownloadCompleted(id, request.downloadHandler.data, url, meta);
            }

            request.Dispose();
            meta.Loader = null;
        }

        static private void OnTextureDownloadCompleted(StringHash32 id, byte[] source, string url, AssetMeta meta) {
            if (meta.Status == AssetStatus.Unloaded || (meta.Status & AssetStatus.PendingUnload) != 0) {
                s_Metas.Remove(id);
                return;
            }

            Texture2D dest = s_Textures[id];
            dest.LoadImage(source, true);
            dest.filterMode = GetTextureFilterMode(url);
            dest.wrapMode = GetTextureWrapMode(url);
            s_LoadCount --;
            meta.Status = AssetStatus.Loaded;
            meta.Loader = null;
            Log.Msg("[Streaming] ...finished loading (async) '{0}'", id);
        }

        static private void OnTextureDownloadFail(StringHash32 id, string url, AssetMeta meta) {
            if (meta.Status == AssetStatus.Unloaded || (meta.Status & AssetStatus.PendingUnload) != 0) {
                return;
            }

            Log.Error("[Streaming] Failed to load texture '{0}' from '{1}", id, url);
            meta.Loader = null;
            meta.Status = AssetStatus.Error;
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

        #region Paths

        static private string StreamingPath(string relative) {
            return Path.Combine(Application.streamingAssetsPath, relative);
        }

        static private string PathToURL(string path) {
            switch (Application.platform) {
                case RuntimePlatform.Android:
                case RuntimePlatform.WebGLPlayer:
                    return path;

                case RuntimePlatform.WSAPlayerARM:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerX86:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "file:///" + path;

                default:
                    return "file://" + path;
            }
        }

        static public string RelativeStreamingPathToURL(string relative) {
            return PathToURL(StreamingPath(relative));
        }

        #endregion // Paths

        #region Management

        /// <summary>
        /// Attempts to return the streaming id associated with the given asset.
        /// </summary>
        static public bool TryGetId(UnityEngine.Object instance, out StringHash32 id) {
            if (!instance) {
                id = default;
                return false;
            }

            int instanceId = instance.GetInstanceID();
            if (!s_ReverseLookup.TryGetValue(instanceId, out id)) {
                Log.Warn("[Streaming] No asset metadata found for {0}'", instance);
                return false;
            }

            return true;
        }

        static private bool Dereference(UnityEngine.Object instance) {
            StringHash32 id;
            if (!TryGetId(instance, out id)) {
                return false;
            }

            AssetMeta meta;
            if (s_Metas.TryGetValue(id, out meta)) {
                if (meta.RefCount > 0) {
                    meta.RefCount--;
                    meta.LastModifiedTS = CurrentTimestamp();
                    if (meta.RefCount == 0) {
                        meta.Status |= AssetStatus.PendingUnload;
                        s_UnloadQueue.PushBack(id);
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if any loads are currently executing.
        /// </summary>
        static public bool IsLoading() {
            return s_LoadCount > 0;
        }

        /// <summary>
        /// Returns if the asset with the given streaming id is loaded.
        /// </summary>
        static public bool IsLoaded(StringHash32 id) {
            return Status(id) == AssetStatus.Loaded;
        }

        /// <summary>
        /// Returns if the given streaming asset is loaded.
        /// </summary>
        static public bool IsLoaded(UnityEngine.Object instance) {
            return Status(instance) == AssetStatus.Loaded;
        }

        /// <summary>
        /// Returns the status of the asset with the given streaming id.
        /// </summary>
        static public AssetStatus Status(StringHash32 id) {
            AssetMeta meta;
            if (s_Metas.TryGetValue(id, out meta)) {
                return meta.Status;
            }

            return AssetStatus.Unloaded;
        }

        /// <summary>
        /// Returns the status of the given streaming asset.
        /// </summary>
        static public AssetStatus Status(UnityEngine.Object instance) {
            StringHash32 id;
            if (!TryGetId(instance, out id)) {
                return AssetStatus.Invalid;
            }

            AssetMeta meta;
            if (s_Metas.TryGetValue(id, out meta)) {
                return meta.Status;
            }

            return AssetStatus.Unloaded;
        }

        static private long CurrentTimestamp() {
            return Stopwatch.GetTimestamp();
        }

        #if UNITY_EDITOR

        static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (Application.isPlaying)
                return;
            
            s_Batcher.TryReloadAll();
        }

        #endif // UNITY_EDITOR

        #if UNITY_EDITOR

        static private void UnloadUnusedSync() {
            IdentifyUnusedPrefetchSync();

            StringHash32 id;
            while(s_UnloadQueue.TryPopFront(out id)) {
                UnloadSingle(id, 0, 0);
            }
        }

        static private void IdentifyUnusedPrefetchSync() {
            AssetMeta meta;
            foreach(var metaKV in s_Metas) {
                meta = metaKV.Value;
                if (meta.RefCount == 0 && (meta.Status & AssetStatus.PendingUnload) == 0) {
                    meta.Status |= AssetStatus.PendingUnload;
                    s_UnloadQueue.PushBack(metaKV.Key);
                }
            }
        }

        #endif // UNITY_EDITOR

        /// <summary>
        /// Returns if streaming assets are currently unloading.
        /// </summary>
        static public bool IsUnloading() {
            return s_UnloadHandle.IsRunning();
        }

        /// <summary>
        /// Unloads all unused streaming assets asynchronously.
        /// </summary>
        static public AsyncHandle UnloadUnusedAsync() {
            if (s_UnloadHandle.IsRunning()) {
                return s_UnloadHandle;
            }
            return s_UnloadHandle = Async.Schedule(UnloadUnusedAsyncJob(0), AsyncFlags.MainThreadOnly);
        }

        /// <summary>
        /// Unloads old unused streaming assets asynchronously.
        /// </summary>
        static public AsyncHandle UnloadUnusedAsync(float minAge) {
            if (s_UnloadHandle.IsRunning()) {
                return s_UnloadHandle;
            }
            long minAgeInTicks = (long) (minAge * TimeSpan.TicksPerSecond);
            return s_UnloadHandle = Async.Schedule(UnloadUnusedAsyncJob(minAgeInTicks), AsyncFlags.MainThreadOnly);
        }

        static private IEnumerator UnloadUnusedAsyncJob(long deleteThreshold) {
            StringHash32 id;
            long current = CurrentTimestamp();
            while(s_UnloadQueue.TryPopFront(out id)) {
                UnloadSingle(id, current, deleteThreshold);
                yield return null;
            }

            s_UnloadHandle = default;
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

            Log.Msg("[Streaming] Unloaded all streamed assets");
        }
    
        static internal bool UnloadSingle(StringHash32 id, long now, long deleteThreshold = 0) {
            AssetMeta meta = s_Metas[id];
            UnityEngine.Object resource = null;
            if (meta.RefCount > 0) {
                return false;
            }

            if (deleteThreshold > 0 && (now - meta.LastModifiedTS) < deleteThreshold) {
                return false;
            }

            s_Metas.Remove(id);
            if (meta.Loader != null) {
                if (!meta.Loader.isDone) {
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
            
            #if UNITY_EDITOR
            if (Application.isPlaying) {
                UnityEngine.Object.Destroy(resource);
            } else {
                UnityEngine.Object.DestroyImmediate(resource);
            }
            #else
            UnityEngine.Object.Destroy(resource);
            #endif // UNITY_EDITOR

            Log.Msg("[Streaming] Unloaded streamed asset '{0}'", id);

            resource = null;

            return true;
        }

        #endregion // Management
    }
}