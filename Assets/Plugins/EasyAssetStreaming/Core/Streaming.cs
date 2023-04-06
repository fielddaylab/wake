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

        private const int RetryLimit = 10;
        private const float RetryDelayBase = 1f;
        private const float RetryDelayExtra = 0.05f;
        private const int TimeOutSecs = 8;

        #endregion // Consts

        #region Types

        private struct LoadState {
            public Queue<StreamingAssetHandle> Queue;
            public List<TimeStampedAssetHandle> DelayedQueue;
            public uint Count;
            public int MaxPerFrame;
        }

        private struct TimeStampedAssetHandle {
            public readonly long Timestamp;
            public readonly StreamingAssetHandle Handle;

            public TimeStampedAssetHandle(StreamingAssetHandle id, long timestamp) {
                Handle = id;
                Timestamp = timestamp;
            }
        }

        private struct UnloadState {
            public Queue<StreamingAssetHandle> Queue;
            public bool Unloading;
            public long StartTS;
            public long MinAge;
            public int MaxPerFrame;
        }

        /// <summary>
        /// Memory statistic.
        /// </summary>
        public struct MemoryStat {
            public long Current;
            public long Max;
        }

        /// <summary>
        /// What kind of result/error was encountered.
        /// </summary>
        public enum LoadResult {
            Success_Download,
            Success_Cached,
            Error_Network,
            Error_Server,
            Cancelled,
            Error_Unknown,
            Error_Simulated,
        }

        /// <summary>
        /// Delegate for reporting errors.
        /// </summary>
        public delegate void LoadBeginDelegate(StreamingAssetHandle id, long size, UnityWebRequest request, int retryStatus);

        /// <summary>
        /// Delegate for reporting errors.
        /// </summary>
        public delegate void LoadResultDelegate(StreamingAssetHandle id, long size, UnityWebRequest request, LoadResult resultType);

        #endregion // Types

        #region State

        // Lookups
        
        static private string s_LocalPathBase = Application.streamingAssetsPath;

        // Load/Unload
        
        static private LoadState s_LoadState = new LoadState() {
            Queue = new Queue<StreamingAssetHandle>(16),
            DelayedQueue = new List<TimeStampedAssetHandle>(16),
            MaxPerFrame = 4
        };

        static private UnloadState s_UnloadState = new UnloadState() {
            Queue = new Queue<StreamingAssetHandle>(16),
            MaxPerFrame = 4
        };

        // Tick

        static private bool s_UpdateEnabled = true;
        static private bool s_TickInitialized;
        static private GameObject s_UpdateHookGO;

        // Simulated conditions

        #if DEVELOPMENT

        static private float s_SimulatedFailureRate = 0;
        static private float s_SimulatedDelay = 0;

        #endif // DEVELOPMENT

        #endregion // State

        #region Initialization

        static private void EnsureInitialized() {
            EnsureCache();
            EnsureTick();
        }

        /// <summary>
        /// Manually initializes streaming.
        /// </summary>
        static public void Initialize() {
            EnsureInitialized();
        }

        #endregion // Initialization

        #region Status

        /// <summary>
        /// Returns if the asset with the given streaming id is loading.
        /// </summary>
        static public bool IsLoading(StreamingAssetHandle id) {
            return !id.IsEmpty && (Status(id) & AssetStatus.PendingLoad) != 0;
        }

        /// <summary>
        /// Returns if the asset with the given streaming id is loaded.
        /// </summary>
        static public bool IsLoaded(StreamingAssetHandle id) {
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
        static public AssetStatus Status(StreamingAssetHandle id) {
            if (s_Cache == null || !s_Cache.IsValid(id)) {
                return AssetStatus.Invalid;
            }

            return s_Cache.StateInfo[id.Index].Status;
        }

        /// <summary>
        /// Returns the status of the given streaming asset.
        /// </summary>
        static public AssetStatus Status(UnityEngine.Object instance) {
            StreamingAssetHandle id = Handle(instance);
            if (id.IsEmpty) {
                return AssetStatus.Invalid;
            }

            return s_Cache.StateInfo[id.Index].Status;
        }

        #endregion // Status

        #region Resolve

        /// <summary>
        /// Resolves the asset id into its associated resource.
        /// </summary>
        static public object Resolve(StreamingAssetHandle id) {
            if (id.IsEmpty || s_Cache == null || !s_Cache.IsValid(id)) {
                return null;
            }

            StreamingAssetType assetType = id.AssetType;

            switch(assetType.Id) {
                case StreamingAssetTypeId.Texture: {
                    if (assetType.Sub == StreamingAssetSubTypeId.VideoTexture) {
                        return Videos.PlayerMap[id].texture;
                    } else {
                        return Textures.TextureMap[id];
                    }
                }
                case StreamingAssetTypeId.Audio: {
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
        static public T Resolve<T>(StreamingAssetHandle id) where T : class {
            return (T) Resolve(id);
        }

        #endregion // Resolve

        #region Errors

        /// <summary>
        /// Event dispatched when a load has completed/failed.
        /// </summary>
        static public event LoadBeginDelegate OnLoadBegin;

        /// <summary>
        /// Event dispatched when a load has completed/failed.
        /// </summary>
        static public event LoadResultDelegate OnLoadResult;

        /// <summary>
        /// Retries loading previously-errored assets.
        /// </summary>
        static public int RetryErrored() {
            int errorCount = 0;
            foreach(var id in s_Cache.ByAddressHash.Values) {
                ref AssetStatus status = ref s_Cache.StateInfo[id.Index].Status;
                if (status == AssetStatus.Error) {
                    status = AssetStatus.PendingLoad;
                    s_Cache.LoadInfo[id.Index].RetryCount = 0;
                    QueueLoad(id);
                    errorCount++;
                }
            }
            return errorCount;
        }

        /// <summary>
        /// Returns the number of assets with loading failures.
        /// </summary>
        static public int ErrorCount() {
            int errorCount = 0;
            foreach(var id in s_Cache.ByAddressHash.Values) {
                ref AssetStatus status = ref s_Cache.StateInfo[id.Index].Status;
                if (status == AssetStatus.Error) {
                    errorCount++;
                }
            }
            return errorCount;
        }

        static private void InvokeLoadBegin(StreamingAssetHandle id, UnityWebRequest request, int retryCount) {
            if (OnLoadBegin != null) {
                OnLoadBegin(id, Manifest.Entry(id).Size, request, retryCount);
            }
        }

        static private void InvokeLoadResult(StreamingAssetHandle id, UnityWebRequest request, LoadResult result) {
            if (OnLoadResult != null) {
                OnLoadResult(id, Manifest.Entry(id).Size, request, result);
            }
        }

        #endregion // Errors

        #region Queues

        static private void QueueLoad(StreamingAssetHandle id) {
            EnsureInitialized();
            s_LoadState.Queue.Enqueue(id);
            s_LoadState.Count++;
            
            ref AssetStatus status = ref s_Cache.StateInfo[id.Index].Status;
            status = (status | AssetStatus.PendingLoad) & ~AssetStatus.PendingUnload;
        }

        static private void QueueDelayedLoad(StreamingAssetHandle id, float delaySeconds) {
            EnsureInitialized();
            s_LoadState.DelayedQueue.Add(new TimeStampedAssetHandle(id, CurrentTimestamp() + (long) (delaySeconds * TimeSpan.TicksPerSecond)));
            s_LoadState.Count++;
            
            ref AssetStatus status = ref s_Cache.StateInfo[id.Index].Status;
            status = (status | AssetStatus.PendingLoad) & ~(AssetStatus.PendingUnload | AssetStatus.Loading);
        }

        static private void DecrementLoadCounter() {
            s_LoadState.Count--;
        }

        static private void QueueUnload(StreamingAssetHandle id) {
            s_UnloadState.Queue.Enqueue(id);
            
            ref AssetStatus status = ref s_Cache.StateInfo[id.Index].Status;
            status = (status | AssetStatus.PendingUnload) & ~AssetStatus.PendingLoad;
        }

        #endregion // Queues

        #region Loading

        /// <summary>
        /// Returns if any loads are currently executing.
        /// </summary>
        static public bool IsLoading() {
            EnsureInitialized();
            return s_LoadState.Count > 0;
        }

        /// <summary>
        /// Returns the number of loads occurring.
        /// </summary>
        static public uint LoadCount() {
            return s_LoadState.Count;
        }

        #endregion // Loading

        #region Unloading

        /// <summary>
        /// Returns if streaming assets are currently unloading.
        /// </summary>
        static public bool IsUnloading() {
            return s_UnloadState.Unloading;
        }

        /// <summary>
        /// Dereferences the given asset.
        /// </summary>
        static public bool Unload(StreamingAssetHandle id, AssetCallback callback = null) {
            if (!id.IsEmpty) {
                Dereference(id, callback);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Dereferences the given asset.
        /// </summary>
        static public bool Unload(ref StreamingAssetHandle id, AssetCallback callback = null) {
            if (!id.IsEmpty) {
                Dereference(id, callback);
                id = default;
                return true;
            }

            return false;
        }

        // Dereferences the asset
        [MethodImpl(256)]
        static private bool Dereference(UnityEngine.Object instance, AssetCallback callback) {
            return Dereference(Handle(instance), callback);
        }

        // Dereferences the asset handle
        static private bool Dereference(StreamingAssetHandle id, AssetCallback callback) {
            if (id.IsEmpty || !s_Cache.IsValid(id)) {
                return false;
            }

            ref AssetStateInfo stateInfo = ref s_Cache.StateInfo[id.Index];

            if (stateInfo.RefCount > 0) {
                stateInfo.RefCount--;
                stateInfo.LastAccessedTS = CurrentTimestamp();
                RemoveCallback(id, callback);
                if (stateInfo.RefCount == 0) {
                    QueueUnload(id);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Unloads all unused streaming assets asynchronously.
        /// </summary>
        static public void UnloadUnusedAsync() {
            if (!s_UnloadState.Unloading) {
                s_UnloadState.Unloading = true;
                s_UnloadState.MinAge = 0;
                s_UnloadState.StartTS = CurrentTimestamp();
                EnsureInitialized();
                IdentifyUnusedSync();
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
                EnsureInitialized();
                IdentifyUnusedSync();
            }
        }

        /// <summary>
        /// Unloads all assets synchronously.
        /// </summary>
        static public void UnloadAll() {
            Textures.DestroyAllTextures();
            AudioClips.DestroyAllClips();
            Videos.DestroyAllVideos();

            if (s_Cache != null) {
                foreach(var id in s_Cache.ByAddressHash.Values) {
                    s_Cache.LoadInfo[id.Index].Loader?.Dispose();
                }

                s_Cache.Clear();
            }
            s_LoadState.Queue.Clear();

            s_UnloadState.Queue.Clear();
            s_UnloadState.Unloading = false;

            UnityEngine.Debug.LogFormat("[Streaming] Unloaded all streamed assets");
        }
    
        // Identifies and queues up unused assets
        static private void IdentifyUnusedSync() {
            foreach(var handle in s_Cache.ByAddressHash.Values) {
                ref AssetStateInfo stateInfo = ref s_Cache.StateInfo[handle.Index];
                if (stateInfo.RefCount > 0) {
                    continue;
                }
                
                stateInfo.Status |= AssetStatus.PendingUnload;
                if (!s_UnloadState.Queue.Contains(handle)) {
                    s_UnloadState.Queue.Enqueue(handle);
                }
            }
        }

        // Identifies the best asset to unload to meet memory budgets
        static private StreamingAssetHandle IdentifyOverBudgetToDelete(StreamingAssetTypeId type, long now, long over) {
            StreamingAssetHandle best = default;
            long bestScore = 0;

            long score;
            foreach(var handle in s_Cache.ByAddressHash.Values) {
                if (handle.AssetType != type) {
                    continue;
                }

                ref AssetStateInfo stateInfo = ref handle.StateInfo;
                if (stateInfo.RefCount > 0 || (stateInfo.Status & AssetStatus.PendingUnload) == 0) {
                    continue;
                }

                // closest to over, largest, oldest
                score = (1 + Math.Abs(stateInfo.Size - over)) * stateInfo.Size * (now - stateInfo.LastAccessedTS) / 8;
                if (score > bestScore) {
                    bestScore = score;
                    best = handle;
                }
            }

            return best;
        }

        static private bool UnloadSingle(StreamingAssetHandle id, long now, long deleteThreshold = 0) {
            if (!s_Cache.IsValid(id)) {
                return false;
            }

            ref AssetStateInfo stateInfo = ref s_Cache.StateInfo[id.Index];

            if (stateInfo.RefCount > 0 || (stateInfo.Status & AssetStatus.PendingUnload) == 0) {
                return false;
            }

            if (deleteThreshold > 0 && (now - stateInfo.LastAccessedTS) < deleteThreshold) {
                return false;
            }

            ref AssetLoadInfo loadInfo = ref s_Cache.LoadInfo[id.Index];

            if (loadInfo.Loader != null) {
                loadInfo.Loader.Dispose();
                InvokeLoadResult(id, loadInfo.Loader, LoadResult.Cancelled);
                loadInfo.Loader = null;
            }

            AssetMetaInfo metaInfo = id.MetaInfo;

            if ((stateInfo.Status & (AssetStatus.PendingLoad | AssetStatus.Loaded | AssetStatus.Error)) != 0) {
                switch(metaInfo.Type.Id) {
                    case StreamingAssetTypeId.Texture: {
                        if (metaInfo.Type.Sub == StreamingAssetSubTypeId.VideoTexture) {
                            Videos.DestroyVideo(id);
                        } else {
                            Textures.DestroyTexture(id);
                        }
                        break;
                    }

                    case StreamingAssetTypeId.Audio: {
                        AudioClips.DestroyClip(id);
                        break;
                    }
                }
            }

            UnityEngine.Debug.LogFormat("[Streaming] Unloaded streamed asset '{0}'", metaInfo.Address);
            s_Cache.FreeSlot(id);
            return true;
        }

        #if UNITY_EDITOR

        static private void UnloadUnusedSync() {
            EnsureInitialized();
            IdentifyUnusedSync();

            while(s_UnloadState.Queue.Count > 0) {
                UnloadSingle(s_UnloadState.Queue.Dequeue(), 0, 0);
            }
        }

        #endif // UNITY_EDITOR
        
        #endregion // Unloading
    
        #region Paths

        static private bool IsURL(string address) {
            return address.Contains("://");
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
        static public string ResolveAddressToURL(string relativePath) {
            if (IsURL(relativePath)) {
                return relativePath;
            }
            return PathToURL(StreamingPath(relativePath));
        }

        #endregion // Paths

        #region Tick

        static private void EnsureTick() {
            if (s_TickInitialized) {
                return;
            }

            s_TickInitialized = true;

            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                UnityEditor.EditorApplication.update += Tick;
                Tick();
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
            StreamingHelper.Release();
        }

        private sealed class UpdateHook : MonoBehaviour {
            private void LateUpdate() {
                Tick();
            }
        }

        static private void Tick() {
            if (!Streaming.Manifest.Loaded) {
                Streaming.Manifest.EnsureLoaded();
            }

            if (!s_UpdateEnabled) {
                return;
            }

            bool didWork = false;

            long now = CurrentTimestamp();
            didWork |= Textures.CheckBudget(now);
            didWork |= AudioClips.CheckBudget(now);

            // update the delayed queue
            for(int i = s_LoadState.DelayedQueue.Count - 1; i >= 0; i--) {
                if (s_LoadState.DelayedQueue[i].Timestamp <= now) {
                    s_LoadState.Queue.Enqueue(s_LoadState.DelayedQueue[i].Handle);
                    s_LoadState.DelayedQueue.FastRemoveAt(i);
                }
            }

            // update the load queue
            int loadFrame = s_LoadState.MaxPerFrame;
            while(s_LoadState.Queue.Count > 0 && loadFrame > 0) {
                loadFrame--;
                StreamingAssetHandle id = s_LoadState.Queue.Dequeue();
                if (!s_Cache.IsValid(id)) {
                    s_LoadState.Count--;
                    continue;
                }
                
                ref AssetStateInfo stateInfo = ref s_Cache.StateInfo[id.Index];
                
                // if unloading or unloaded then ignore
                if ((stateInfo.Status & (AssetStatus.PendingUnload | AssetStatus.Unloaded)) != 0) {
                    UnloadSingle(id, now, 0);
                    s_LoadState.Count--;
                    didWork = true;
                    continue;
                }

                // if already loading then ignore
                if ((stateInfo.Status & AssetStatus.Loading) != 0) {
                    s_LoadState.Count--;
                    continue;
                }

                stateInfo.Status |= AssetStatus.Loading;

                AssetMetaInfo metaInfo = s_Cache.MetaInfo[id.Index];
                UnityEngine.Debug.LogFormat("[Streaming] Beginning download of '{0}'", metaInfo.Address);

                switch(metaInfo.Type.Id) {
                    case StreamingAssetTypeId.Texture: {
                        if (metaInfo.Type.Sub == StreamingAssetSubTypeId.VideoTexture) {
                            Videos.StartLoading(id);
                        } else {
                            Textures.StartLoading(id);
                        }
                        break;
                    }
                    case StreamingAssetTypeId.Audio: {
                        AudioClips.StartLoading(id);
                        break;
                    }
                }

                didWork = true;
            }

            // update the unload queue
            if (s_UnloadState.Unloading) {
                int unloadFrame = s_UnloadState.MaxPerFrame;
                while(s_UnloadState.Queue.Count > 0 && unloadFrame > 0) {
                    unloadFrame--;
                    StreamingAssetHandle id = s_UnloadState.Queue.Dequeue();
                    UnloadSingle(id, s_UnloadState.StartTS, s_UnloadState.MinAge);
                }

                didWork = true;

                if (s_UnloadState.Queue.Count == 0) {
                    s_UnloadState.Unloading = false;
                }
            }
        
            if (!didWork) {
                Textures.ProcessCompressionQueue();
            }
        }

        #endregion // Tick

        #region Utilities

        [MethodImpl(256)]
        static private uint AddressKey(string address) {
            return StreamingHelper.HashString(address);
        }

        [MethodImpl(256)]
        static private long CurrentTimestamp() {
            return Stopwatch.GetTimestamp();
        }

        [MethodImpl(256)]
        static private void RecomputeMemorySize(ref MemoryStat memUsage, StreamingAssetHandle id, UnityEngine.Object asset) {
            RecomputeMemorySize(ref memUsage, ref id.StateInfo, asset);
        }

        [MethodImpl(256)]
        static private void RecomputeMemorySize(ref MemoryStat memUsage, ref AssetStateInfo state, UnityEngine.Object asset) {
            memUsage.Current -= state.Size;
            state.Size = StreamingHelper.CalculateMemoryUsage(asset);
            memUsage.Current += state.Size;
            if (memUsage.Max < memUsage.Current) {
                memUsage.Max = memUsage.Current;
            }

            // if (asset != null) {
            //     long reportedSize = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(asset);
            //     if (state.Size != reportedSize) {
            //         UnityEngine.Debug.LogWarningFormat("[Streaming] Asset {0}: computed size of {1} and profiler size {2} do not match (difference of {3})", asset.name, state.Size, reportedSize, reportedSize - state.Size);
            //     }
            // }
        }

        [MethodImpl(256)]
        static private bool DownloadFailed(UnityWebRequest request) {
            #if DEVELOPMENT
            return request.isNetworkError || request.isHttpError || UnityEngine.Random.value < s_SimulatedFailureRate;
            #else
            return request.isNetworkError || request.isHttpError;
            #endif // DEVELOPMENT
        }

        #endregion // Utilities
    }
}