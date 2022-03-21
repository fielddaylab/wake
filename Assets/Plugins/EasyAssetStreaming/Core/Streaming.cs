#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

[assembly: InternalsVisibleTo("EasyAssetStreaming.Editor")]

namespace EasyAssetStreaming {

    /// <summary>
    /// Asset streaming.
    /// </summary>
#if UNITY_EDITOR
    public partial class Streaming : UnityEditor.AssetPostprocessor {
    #else
    static public partial class Streaming {
    #endif // UNITY_EDITOR

        #region Consts

        #endregion // Consts

        #region Editor Hooks

        #if UNITY_EDITOR

        static private bool s_EditorQuitting;

        [UnityEditor.InitializeOnLoadMethod]
        static private void EditorInitialize() {
            UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChange;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            UnityEditor.EditorApplication.quitting += () => s_EditorQuitting = true;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        }

        static private void PlayModeStateChange(UnityEditor.PlayModeStateChange stateChange) {
            if (stateChange != UnityEditor.PlayModeStateChange.ExitingEditMode && stateChange != UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                return;
            }

            UnloadAll();
            DeregisterTick();
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

        static private void OnDomainUnload(object sender, EventArgs args) {
            if (s_EditorQuitting) {
                return;
            }

            UnloadAll();
        }

        static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (Application.isPlaying)
                return;

            bool manifestUpdated = Manifest.ReloadEditor();
            
            foreach(var meta in s_Metas) {
                TryReloadAsset(meta.Key, meta.Value, manifestUpdated);
            }
        }

        static private void TryReloadAsset(StreamingAssetId id, AssetMeta meta, bool manifestUpdated) {
            if (string.IsNullOrEmpty(meta.EditorPath)) {
                return;
            }

            bool bDeleted = !File.Exists(meta.EditorPath);
            bool bModified = false;
            if (!bDeleted) {
                try {
                    bModified = File.GetLastWriteTimeUtc(meta.EditorPath).ToFileTimeUtc() != meta.EditorEditTime;
                } catch {
                    bModified = false;
                }
            }

            switch(meta.Type) {
                case AssetType.Texture: {
                    if (bDeleted) {
                        Textures.HandleTextureDeleted(id, meta);
                    } else if (bModified || manifestUpdated) {
                        Textures.HandleTextureModified(id, meta);
                    }
                    break;
                }
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Editor Hooks

        #region Types

        public delegate void AssetCallback(StreamingAssetId id, AssetStatus status, object asset);

        internal enum AssetType : ushort {
            Unknown = 0,

            Texture,
            Audio,
            Video
        }

        public enum AssetStatus : byte {
            Unloaded = 0,
            Invalid = 0x01,

            PendingUnload = 0x02,
            PendingLoad = 0x04,
            Loaded = 0x08,
            Error = 0x10,
        }

        internal class AssetMeta {
            public AssetType Type;
            public AssetStatus Status;
            public ushort RefCount;
            public long Size;
            public long LastModifiedTS;
            public string Path;

            public UnityWebRequest Loader;
            
            #if UNITY_EDITOR
            public string EditorPath;
            public long EditorEditTime;
            #endif // UNITY_EDITOR

            public List<AssetCallback> OnUpdate;
        }

        public struct MemoryStat {
            public long Current;
            public long Max;
        }

        private struct LoadState {
            public Queue<StreamingAssetId> Queue;
            public uint Count;
            public int MaxPerFrame;
        }

        private struct UnloadState {
            public Queue<StreamingAssetId> Queue;
            public bool Unloading;
            public long StartTS;
            public long MinAge;
            public int MaxPerFrame;
        }

        #endregion // Types

        #region State

        // Lookups

        static private readonly Dictionary<StreamingAssetId, AssetMeta> s_Metas = new Dictionary<StreamingAssetId, AssetMeta>();
        static private readonly Dictionary<int, StreamingAssetId> s_ReverseLookup = new Dictionary<int, StreamingAssetId>();

        static private string s_LocalPathBase = Application.streamingAssetsPath;

        // Load/Unload
        
        static private LoadState s_LoadState = new LoadState() {
            Queue = new Queue<StreamingAssetId>(16),
            MaxPerFrame = 4
        };

        static private UnloadState s_UnloadState = new UnloadState() {
            Queue = new Queue<StreamingAssetId>(16),
            MaxPerFrame = 4
        };

        // Tick

        static private bool s_UpdateEnabled = true;
        static private bool s_TickInitialized;
        static private GameObject s_UpdateHookGO;

        #endregion // State

        #region Management

        /// <summary>
        /// Returns the streaming id associated with the given asset.
        /// </summary>
        static public StreamingAssetId Id(UnityEngine.Object instance) {
            if (!instance) {
                return default;
            }

            StreamingAssetId id;
            int instanceId = instance.GetInstanceID();
            if (!s_ReverseLookup.TryGetValue(instanceId, out id)) {
                UnityEngine.Debug.LogWarningFormat("[Streaming] No asset metadata found for {0}'", instance);
            }

            return id;
        }

        /// <summary>
        /// Returns if any loads are currently executing.
        /// </summary>
        static public bool IsLoading() {
            EnsureTick();
            return s_LoadState.Count > 0;
        }

        /// <summary>
        /// Returns the number of loads occurring.
        /// </summary>
        static public uint LoadCount() {
            return s_LoadState.Count;
        }

        /// <summary>
        /// Returns if the asset with the given streaming id is loading.
        /// </summary>
        static public bool IsLoading(StreamingAssetId id) {
            return !id.IsEmpty && (Status(id) & AssetStatus.PendingLoad) != 0;
        }

        /// <summary>
        /// Returns if the asset with the given streaming id is loaded.
        /// </summary>
        static public bool IsLoaded(StreamingAssetId id) {
            return !id.IsEmpty && Status(id) == AssetStatus.Loaded;
        }

        /// <summary>
        /// Returns if the given streaming asset is loaded.
        /// </summary>
        static public bool IsLoaded(UnityEngine.Object instance) {
            return instance && Status(instance) == AssetStatus.Loaded;
        }

        /// <summary>
        /// Returns the status of the asset with the given streaming id.
        /// </summary>
        static public AssetStatus Status(StreamingAssetId id) {
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
            StreamingAssetId id = Id(instance);
            if (id.IsEmpty) {
                return AssetStatus.Invalid;
            }

            AssetMeta meta;
            if (s_Metas.TryGetValue(id, out meta)) {
                return meta.Status;
            }

            return AssetStatus.Unloaded;
        }

        /// <summary>
        /// Resolves the asset id into its associated resource.
        /// </summary>
        static public object Resolve(StreamingAssetId id) {
            if (id.IsEmpty) {
                return null;
            }

            switch(id.Type) {
                case AssetType.Texture: {
                    return Textures.TextureMap[id];
                }
                case AssetType.Audio: {
                    return AudioClips.ClipMap[id];
                }
                default: {
                    return null;
                }
            }
        }

        /// <summary>
        /// Resolves the asset id into its associated resource.
        /// </summary>
        static public T Resolve<T>(StreamingAssetId id) where T : class {
            return (T) Resolve(id);
        }

        static private long CurrentTimestamp() {
            return Stopwatch.GetTimestamp();
        }

        #endregion // Management

        #region Unloading

        #if UNITY_EDITOR

        static private void UnloadUnusedSync() {
            IdentifyUnusedSync();

            while(s_UnloadState.Queue.Count > 0) {
                UnloadSingle(s_UnloadState.Queue.Dequeue(), 0, 0);
            }
        }

        #endif // UNITY_EDITOR

        /// <summary>
        /// Dereferences the given asset.
        /// </summary>
        static public bool Unload(StreamingAssetId id, AssetCallback callback = null) {
            if (!id.IsEmpty) {
                Dereference(id, callback);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Dereferences the given asset.
        /// </summary>
        static public bool Unload(ref StreamingAssetId id, AssetCallback callback = null) {
            if (!id.IsEmpty) {
                Dereference(id, callback);
                id = default;
                return true;
            }

            return false;
        }

        static private bool Dereference(UnityEngine.Object instance, AssetCallback callback) {
            StreamingAssetId id = Id(instance);
            if (id.IsEmpty) {
                return false;
            }

            AssetMeta meta;
            if (s_Metas.TryGetValue(id, out meta)) {
                if (meta.RefCount > 0) {
                    meta.RefCount--;
                    meta.LastModifiedTS = CurrentTimestamp();
                    RemoveCallback(meta, callback);
                    if (meta.RefCount == 0) {
                        meta.Status |= AssetStatus.PendingUnload;
                        s_UnloadState.Queue.Enqueue(id);
                    }
                    return true;
                }
            }

            return false;
        }

        static private bool Dereference(StreamingAssetId id, AssetCallback callback) {
            if (id.IsEmpty) {
                return false;
            }

            AssetMeta meta;
            if (s_Metas.TryGetValue(id, out meta)) {
                if (meta.RefCount > 0) {
                    meta.RefCount--;
                    meta.LastModifiedTS = CurrentTimestamp();
                    RemoveCallback(meta, callback);
                    if (meta.RefCount == 0) {
                        meta.Status |= AssetStatus.PendingUnload;
                        s_UnloadState.Queue.Enqueue(id);
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if streaming assets are currently unloading.
        /// </summary>
        static public bool IsUnloading() {
            return s_UnloadState.Unloading;
        }

        /// <summary>
        /// Unloads all unused streaming assets asynchronously.
        /// </summary>
        static public void UnloadUnusedAsync() {
            if (!s_UnloadState.Unloading) {
                s_UnloadState.Unloading = true;
                s_UnloadState.MinAge = 0;
                s_UnloadState.StartTS = CurrentTimestamp();
                IdentifyUnusedSync();
                EnsureTick();
            }
        }

        /// <summary>
        /// Unloads old unused streaming assets asynchronously.
        /// </summary>
        static public void UnloadUnusedAsync(float minAge) {
            if (!s_UnloadState.Unloading || s_UnloadState.MinAge == 0) {
                s_UnloadState.Unloading = true;
                s_UnloadState.MinAge = (long) (minAge * TimeSpan.TicksPerSecond);
                s_UnloadState.StartTS = CurrentTimestamp();
                IdentifyUnusedSync();
                EnsureTick();
            }
        }

        /// <summary>
        /// Unloads all assets synchronously.
        /// </summary>
        static public void UnloadAll() {
            Textures.DestroyAllTextures();
            AudioClips.DestroyAllClips();

            foreach(var meta in s_Metas.Values) {
                meta.Status = AssetStatus.Unloaded;
                if (meta.Loader != null) {
                    meta.Loader.Dispose();
                    meta.Loader = null;
                }
            }

            s_ReverseLookup.Clear();
            s_Metas.Clear();
            s_LoadState.Queue.Clear();

            s_UnloadState.Queue.Clear();
            s_UnloadState.Unloading = false;

            UnityEngine.Debug.LogFormat("[Streaming] Unloaded all streamed assets");
        }
    
        static private void IdentifyUnusedSync() {
            AssetMeta meta;
            foreach(var metaKV in s_Metas) {
                meta = metaKV.Value;
                if (meta.RefCount == 0) {
                    meta.Status |= AssetStatus.PendingUnload;
                    if (!s_UnloadState.Queue.Contains(metaKV.Key)) {
                        s_UnloadState.Queue.Enqueue(metaKV.Key);
                    }
                }
            }
        }

        static private StreamingAssetId IdentifyOverBudgetToDelete(AssetType type, long now, long over) {
            AssetMeta meta;
            StreamingAssetId best = default;
            long bestScore = 0;

            long score;
            foreach(var metaKv in s_Metas) {
                meta = metaKv.Value;
                if (meta.RefCount > 0 || (meta.Status & AssetStatus.PendingUnload) == 0 || meta.Type != type) {
                    continue;
                }

                // closest to over, largest, oldest
                score = (1 + Math.Abs(meta.Size - over)) * meta.Size * (now - meta.LastModifiedTS) / 8;
                if (score > bestScore) {
                    bestScore = score;
                    best = metaKv.Key;
                }
            }

            return best;
        }

        static private bool UnloadSingle(StreamingAssetId id, long now, long deleteThreshold = 0) {
            AssetMeta meta;
            if (!s_Metas.TryGetValue(id, out meta)) {
                return false;
            }

            if (meta.RefCount > 0 || (meta.Status & AssetStatus.PendingUnload) == 0) {
                return false;
            }

            if (deleteThreshold > 0 && (now - meta.LastModifiedTS) < deleteThreshold) {
                return false;
            }

            s_Metas.Remove(id);
            if (meta.Loader != null) {
                meta.Loader.Dispose();
                meta.Loader = null;
            }

            #if UNITY_EDITOR
            meta.EditorPath = null;
            meta.EditorEditTime = 0;
            #endif // UNITY_EDITOR

            if (meta.OnUpdate != null) {
                meta.OnUpdate.Clear();
                meta.OnUpdate = null;
            }

            if ((meta.Status & (AssetStatus.PendingLoad | AssetStatus.Loaded | AssetStatus.Error)) != 0) {
                UnityEngine.Object resource = null;
                
                switch(meta.Type) {
                    case AssetType.Texture: {
                        resource = Textures.DestroyTexture(id, meta);
                        break;
                    }

                    case AssetType.Audio: {
                        resource = AudioClips.DestroyClip(id, meta);
                        break;
                    }
                }
                s_ReverseLookup.Remove(resource.GetInstanceID());
                resource = null;
            }

            UnityEngine.Debug.LogFormat("[Streaming] Unloaded streamed asset '{0}'", id);
            
            meta.Size = 0;
            meta.Status = AssetStatus.Unloaded;

            return true;
        }
        
        #endregion // Unloading
    
        #region Paths

        static private bool IsURL(string pathOrUrl) {
            return pathOrUrl.Contains("://");
        }

        static private string StreamingPath(string relativePath) {
            return Path.Combine(s_LocalPathBase, relativePath).Replace("\\", "/");
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

        /// <summary>
        /// Converts a path relative to StreamingAssets into a URL.
        /// If the input is already a URL, it is preserved.
        /// </summary>
        static public string ResolvePathToURL(string relativePath) {
            if (IsURL(relativePath)) {
                return relativePath;
            }
            return PathToURL(StreamingPath(relativePath));
        }

        #endregion // Paths

        #region Update Hook

        static private void EnsureTick() {
            if (s_TickInitialized) {
                return;
            }

            s_TickInitialized = true;

            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                UnityEditor.EditorApplication.update += Tick;
                return;
            }
            #endif // UNITY_EDITOR

            if (s_UpdateHookGO == null) {
                s_UpdateHookGO = new GameObject("[StreamingTick]");
                s_UpdateHookGO.hideFlags = HideFlags.DontSave;
                GameObject.DontDestroyOnLoad(s_UpdateHookGO);
                s_UpdateHookGO.AddComponent<UpdateHook>();
            }
        }

        static private void DeregisterTick() {
            if (!s_TickInitialized) {
                return;
            }
            
            s_TickInitialized = false;

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= Tick;
            #endif // UNITY_EDITOR

            StreamingHelper.DestroyResource(ref s_UpdateHookGO);
        }

        private sealed class UpdateHook : MonoBehaviour {
            private void LateUpdate() {
                Tick();
            }
        }

        static private void Tick() {
            if (!s_UpdateEnabled) {
                if (!Streaming.Manifest.Loaded) {
                    Streaming.Manifest.EnsureLoaded();
                }
                return;
            }

            long now = CurrentTimestamp();
            Textures.CheckBudget(now);
            AudioClips.CheckBudget(now);

            int loadFrame = s_LoadState.MaxPerFrame;
            while(s_LoadState.Queue.Count > 0 && loadFrame > 0) {
                loadFrame--;
                StreamingAssetId id = s_LoadState.Queue.Dequeue();
                AssetMeta meta;
                
                // if this doesn't exist, then just cancel out.
                if (!s_Metas.TryGetValue(id, out meta)) {
                    s_LoadState.Count--;
                    continue;
                }

                if ((meta.Status & (AssetStatus.PendingUnload | AssetStatus.Unloaded)) != 0) {
                    UnloadSingle(id, now, 0);
                    s_LoadState.Count--;
                    continue;
                }

                UnityEngine.Debug.LogFormat("[Streaming] Beginning download of '{0}'", meta.Path);

                switch(meta.Type) {
                    case AssetType.Texture: {
                        Textures.StartLoading(id, meta);
                        break;
                    }
                    case AssetType.Audio: {
                        AudioClips.StartLoading(id, meta);
                        break;
                    }
                }
            }

            if (s_UnloadState.Unloading) {
                int unloadFrame = s_UnloadState.MaxPerFrame;
                while(s_UnloadState.Queue.Count > 0 && unloadFrame > 0) {
                    unloadFrame--;
                    StreamingAssetId id = s_UnloadState.Queue.Dequeue();
                    UnloadSingle(id, s_UnloadState.StartTS, s_UnloadState.MinAge);
                }

                if (s_UnloadState.Queue.Count == 0) {
                    s_UnloadState.Unloading = false;
                }
            }
        }

        #endregion // Update Hook

        #region Utilities

        static private void AddCallback(AssetMeta meta, StreamingAssetId id, object asset, AssetCallback callback) {
            if (callback != null) {
                if (meta.OnUpdate == null) {
                    meta.OnUpdate = new List<AssetCallback>();
                }
                meta.OnUpdate.Add(callback);
                if (meta.Status != AssetStatus.PendingLoad) {
                    callback(id, meta.Status, asset);
                }
            }
        }

        static private void InvokeCallbacks(AssetMeta meta, StreamingAssetId id, object asset) {
            var onUpdate = meta.OnUpdate;
            AssetStatus status = meta.Status;
            if (onUpdate != null) {
                for(int i = 0, len = onUpdate.Count; i < len; i++) {
                    onUpdate[i](id, status, asset);
                }
            }
        }

        static private void RemoveCallback(AssetMeta meta, AssetCallback callback) {
            if (callback != null && meta.OnUpdate != null) {
                meta.OnUpdate.FastRemove(callback);
            }
        }
    
        static private void RecomputeMemorySize(ref MemoryStat memUsage, AssetMeta meta, UnityEngine.Object asset) {
            memUsage.Current -= meta.Size;
            meta.Size = StreamingHelper.CalculateMemoryUsage(asset);
            memUsage.Current += meta.Size;
            if (memUsage.Max < memUsage.Current) {
                memUsage.Max = memUsage.Current;
            }
        }

        #endregion // Utilities
    }
}