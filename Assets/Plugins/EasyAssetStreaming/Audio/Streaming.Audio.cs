#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyAssetStreaming {

    /// <summary>
    /// Asset streaming.
    /// </summary>
    #if UNITY_EDITOR
    public partial class Streaming : UnityEditor.AssetPostprocessor {
    #else
    static public partial class Streaming {
    #endif // UNITY_EDITOR

        #region Public API

        /// <summary>
        /// Loads an audio clip from a given url.
        /// Returns if the "assetId" parameter has changed.
        /// </summary>
        static public bool Audio(string address, ref StreamingAssetHandle assetId, AssetCallback callback = null) {
            if (string.IsNullOrEmpty(address)) {
                return Unload(ref assetId, callback);
            }

            EnsureInitialized();

            AudioClip loadedClip;
            StreamingAssetHandle id = AudioClips.GetHandle(address, out loadedClip);

            if (assetId != id) {
                Dereference(assetId, callback);
                assetId = id;
                
                ref AssetStateInfo state = ref id.StateInfo;
                state.RefCount++;
                state.LastAccessedTS = CurrentTimestamp();
                state.Status &= ~AssetStatus.PendingUnload;
                AddCallback(id, loadedClip, callback);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Loads an audio clip from a given url.
        /// </summary>
        static public StreamingAssetHandle Audio(string address, AssetCallback callback = null) {
            if (string.IsNullOrEmpty(address)) {
                return default;
            }

            EnsureInitialized();

            AudioClip loadedClip;
            StreamingAssetHandle id = AudioClips.GetHandle(address, out loadedClip);

            ref AssetStateInfo state = ref id.StateInfo;
            state.RefCount++;
            state.LastAccessedTS = CurrentTimestamp();
            state.Status &= ~AssetStatus.PendingUnload;
            AddCallback(id, loadedClip, callback);

            return id;
        }

        /// <summary>
        /// Dereferences the given audio clip.
        /// </summary>
        static public bool Unload(ref AudioClip audioClip, AssetCallback callback = null) {
            if (!object.ReferenceEquals(audioClip, null)) {
                Dereference(audioClip, callback);
                audioClip = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the total number of streamed audio bytes.
        /// </summary>
        static public MemoryStat AudioMemoryUsage() {
            return AudioClips.MemoryUsage;
        }

        /// <summary>
        /// Returns the budgeted number of streamed audio bytes tolerated.
        /// If not 0, this will attempt to unload unreferenced audio clips when this is exceeded.
        /// </summary>
        static public long AudioMemoryBudget {
            get { return AudioClips.MemoryBudget; }
            set { AudioClips.MemoryBudget = value; }
        }

        #endregion // Public API

        /// <summary>
        /// Internal AudioClip api
        /// </summary>
        static internal class AudioClips {

            #region Types

            #endregion // Types

            #region State

            static public readonly Dictionary<StreamingAssetHandle, AudioClip> ClipMap = new Dictionary<StreamingAssetHandle, AudioClip>();
            static public MemoryStat MemoryUsage = default;
            static public long MemoryBudget;

            #endregion // State

            static public StreamingAssetHandle GetHandle(string address, out AudioClip clip) {
                StreamingAssetHandle handle;
                uint hash = AddressKey(address);
                if (!s_Cache.ByAddressHash.TryGetValue(hash, out handle)) {
                    handle = s_Cache.AllocSlot(address, StreamingAssetTypeId.Audio);
                    clip = LoadAudioAsync(handle);
                } else {
                    clip = ClipMap[handle];
                }
                
                return handle;
            }

            static public void StartLoading(StreamingAssetHandle id) {
                string url = id.MetaInfo.ResolvedAddress;
                var request = id.LoadInfo.Loader = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
                request.downloadHandler = new DownloadHandlerAudioClip(url, GetAudioTypeForURL(url));
                InvokeLoadBegin(id, request, id.LoadInfo.RetryCount);
                var sent = request.SendWebRequest();
                sent.completed += (_) => {
                    HandleAudioUWRFinished(id);
                };
            }

            static public void DestroyClip(StreamingAssetHandle id) {
                AudioClip clip = ClipMap[id];
                ClipMap.Remove(id);
                MemoryUsage.Current -= id.StateInfo.Size;
                StreamingHelper.DestroyResource(clip);
            }

            static public void DestroyAllClips() {
                foreach(var clip in ClipMap.Values) {
                    StreamingHelper.DestroyResource(clip);
                }

                ClipMap.Clear();
                MemoryUsage.Current = 0;
            }

            #region Placeholder

            static private AudioClip CreatePlaceholder(string name, bool final) {
                return null;
            }

            #endregion // Placeholder
        
            #region Load

            static private AudioClip LoadAudioAsync(StreamingAssetHandle id) {
                AudioClip clip = CreatePlaceholder(id.MetaInfo.Address, false);
                s_Cache.BindAsset(id, clip);
                QueueLoad(id);
                RecomputeMemorySize(ref MemoryUsage, id, clip);
                return clip;
            }

            static private void HandleAudioUWRFinished(StreamingAssetHandle id) {
                DecrementLoadCounter();

                ref AssetStateInfo stateInfo = ref id.StateInfo;
                ref AssetLoadInfo loadInfo = ref id.LoadInfo;

                if (stateInfo.Status == AssetStatus.Unloaded) {
                    return;
                }

                if ((stateInfo.Status & AssetStatus.PendingUnload) != 0) {
                    UnloadSingle(id, 0, 0);
                    return;
                }

                UnityWebRequest request = loadInfo.Loader;
                bool failed = DownloadFailed(request);

                InvokeLoadResult(id, request, StreamingHelper.ResultType(request, failed));

                if (failed) {
                    if (loadInfo.RetryCount < RetryLimit && StreamingHelper.ShouldRetry(request)) {
                        UnityEngine.Debug.LogWarningFormat("[Streaming] Retrying audio load '{0}' from '{1}': {2}", id.MetaInfo.Address, id.MetaInfo.ResolvedAddress, loadInfo.Loader.error);
                        loadInfo.RetryCount++;
                        QueueDelayedLoad(id, RetryDelayBase + (loadInfo.RetryCount - 1) * RetryDelayExtra);
                        return;
                    }
                    OnAudioDownloadFail(id, request.error);
                } else {
                    OnAudioDownloadCompleted(id, ((DownloadHandlerAudioClip) request.downloadHandler).audioClip);
                }

                request.Dispose();
                loadInfo.Loader = null;
            }

            static private void OnAudioDownloadCompleted(StreamingAssetHandle id, AudioClip clip) {
                ClipMap[id] = clip;
                s_Cache.BindAsset(id, clip);

                id.StateInfo.Status = AssetStatus.Loaded;
                RecomputeMemorySize(ref MemoryUsage, id, clip);
                UnityEngine.Debug.LogFormat("[Streaming] ...finished loading audio (async) '{0}'", id.MetaInfo.Address);
                InvokeCallbacks(id, clip);
            }

            static private void OnAudioDownloadFail(StreamingAssetHandle id, string error) {
                UnityEngine.Debug.LogErrorFormat("[Streaming] Failed to load audio '{0}' from '{1}': {2}", id.MetaInfo.Address, id.MetaInfo.ResolvedAddress, error);
                id.StateInfo.Status = AssetStatus.Error;
                InvokeCallbacks(id, ClipMap[id]);
            }

            #endregion // Load

            #region Budget

            static private bool s_OverBudgetFlag;

            static public bool CheckBudget(long now) {
                if (MemoryBudget <= 0) {
                    return false;
                }

                long over = MemoryUsage.Current - MemoryBudget;
                if (over > 0) {
                    if (!s_OverBudgetFlag) {
                        UnityEngine.Debug.LogFormat("[Streaming] Audio memory is over budget by {0:0.00} Kb", over / 1024f);
                        s_OverBudgetFlag = true;
                    }
                    StreamingAssetHandle asset = IdentifyOverBudgetToDelete(StreamingAssetTypeId.Audio, now, over);
                    if (asset) {
                        UnloadSingle(asset, now);
                        s_OverBudgetFlag = false;
                        return true;
                    }
                } else {
                    s_OverBudgetFlag = false;
                }

                return false;
            }

            #endregion // Budget
        
            #region Manifest

            #endregion // Manifest

            static internal AudioType GetAudioTypeForURL(string inURL)
            {
                if (inURL.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                    return AudioType.MPEG;
                if (inURL.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                    return AudioType.OGGVORBIS;
                if (inURL.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    return AudioType.WAV;

                if (inURL.EndsWith(".acc", StringComparison.OrdinalIgnoreCase))
                    return AudioType.ACC;
                if (inURL.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase))
                    return AudioType.AIFF;
                if (inURL.EndsWith(".it", StringComparison.OrdinalIgnoreCase))
                    return AudioType.IT;
                if (inURL.EndsWith(".mod", StringComparison.OrdinalIgnoreCase))
                    return AudioType.MOD;
                if (inURL.EndsWith(".mp2", StringComparison.OrdinalIgnoreCase))
                    return AudioType.MPEG;
                if (inURL.EndsWith(".s3m", StringComparison.OrdinalIgnoreCase))
                    return AudioType.S3M;
                if (inURL.EndsWith(".xm", StringComparison.OrdinalIgnoreCase))
                    return AudioType.XM;
                if (inURL.EndsWith(".xma", StringComparison.OrdinalIgnoreCase))
                    return AudioType.XMA;
                if (inURL.EndsWith(".vag", StringComparison.OrdinalIgnoreCase))
                    return AudioType.VAG;

                #if UNITY_IOS && !UNITY_EDITOR
                return AudioType.AUDIOQUEUE;
                #else
                return AudioType.UNKNOWN;
                #endif // UNITY_IOS && !UNITY_EDITOR
            }
        }
    }
}