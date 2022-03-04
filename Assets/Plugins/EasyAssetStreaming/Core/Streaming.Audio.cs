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
        static public bool Audio(string pathOrUrl, ref StreamingAssetId assetId, AssetCallback callback = null) {
            if (string.IsNullOrEmpty(pathOrUrl)) {
                return Unload(ref assetId, callback);
            }

            Manifest.EnsureLoaded();

            StreamingAssetId id = new StreamingAssetId(pathOrUrl, AssetType.Audio);
            AudioClip loadedClip;
            AssetMeta meta = Audios.GetMeta(id, pathOrUrl, out loadedClip);

            if (assetId != id) {
                Dereference(assetId, callback);
                assetId = id;
                meta.RefCount++;
                meta.LastModifiedTS = CurrentTimestamp();
                meta.Status &= ~AssetStatus.PendingUnload;
                AddCallback(meta, id, loadedClip, callback);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Loads an audio clip from a given url.
        /// </summary>
        static public StreamingAssetId Audio(string pathOrUrl, AssetCallback callback = null) {
            if (string.IsNullOrEmpty(pathOrUrl)) {
                return default;
            }

            Manifest.EnsureLoaded();

            StreamingAssetId id = new StreamingAssetId(pathOrUrl, AssetType.Texture);
            AudioClip loadedClip;
            AssetMeta meta = Audios.GetMeta(id, pathOrUrl, out loadedClip);

            meta.RefCount++;
            meta.LastModifiedTS = CurrentTimestamp();
            meta.Status &= ~AssetStatus.PendingUnload;
            AddCallback(meta, id, loadedClip, callback);

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
            return Audios.MemoryUsage;
        }

        #endregion // Public API

        /// <summary>
        /// Internal Texture api
        /// </summary>
        static internal class Audios {

            #region State

            static public readonly Dictionary<StreamingAssetId, AudioClip> ClipMap = new Dictionary<StreamingAssetId, AudioClip>();
            static public MemoryStat MemoryUsage = default;

            #endregion // State

            static public AssetMeta GetMeta(StreamingAssetId id, string pathOrUrl, out AudioClip clip) {
                AudioClip loadedClip;
                AssetMeta meta;
                if (!s_Metas.TryGetValue(id, out meta)) {
                    meta = new AssetMeta();

                    UnityEngine.Debug.LogFormat("[Streaming] Loading streamed audio '{0}'...", id);
                    
                    meta.Type = AssetType.Audio;
                    meta.Status = AssetStatus.PendingLoad;
                    meta.Path = pathOrUrl;
                    loadedClip = LoadAudioAsync(id, pathOrUrl, meta);

                    s_Metas[id] = meta;
                    ClipMap[id] = loadedClip;
                } else {
                    loadedClip = ClipMap[id];
                }

                clip = loadedClip;
                return meta;
            }

            static public void StartLoading(StreamingAssetId id, AssetMeta meta) {
                var sent = meta.Loader.SendWebRequest();
                sent.completed += (_) => {
                    HandleAudioUWRFinished(id, meta.Path, meta, meta.Loader);
                };
            }

            static public UnityEngine.Object DestroyClip(StreamingAssetId id, AssetMeta meta) {
                AudioClip clip = ClipMap[id];
                ClipMap.Remove(id);
                MemoryUsage.Current -= meta.Size;

                StreamingHelper.DestroyResource(clip);
                return clip;
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

            static private AudioClip LoadAudioAsync(StreamingAssetId id, string pathOrUrl, AssetMeta meta) {
                AudioClip clip = CreatePlaceholder(pathOrUrl, false);
                string url = ResolvePathToURL(pathOrUrl);
                var request = meta.Loader = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
                request.downloadHandler = new DownloadHandlerAudioClip(url, GetAudioTypeForURL(url));
                s_LoadState.Queue.Enqueue(id);
                EnsureTick();
                RecomputeMemorySize(ref MemoryUsage, meta, clip);
                s_LoadState.Count++;
                return clip;
            }

            static private void HandleAudioUWRFinished(StreamingAssetId id, string pathOrUrl, AssetMeta meta, UnityWebRequest request) {
                s_LoadState.Count--;

                if (meta.Status == AssetStatus.Unloaded) {
                    return;
                }

                if ((meta.Status & AssetStatus.PendingUnload) != 0) {
                    UnloadSingle(id, 0, 0);
                    return;
                }

                if (request.isNetworkError || request.isHttpError) {
                    OnAudioDownloadFail(id, pathOrUrl, meta);
                } else {
                    OnAudioDownloadCompleted(id, ((DownloadHandlerAudioClip) request.downloadHandler).audioClip, pathOrUrl, meta);
                }

                request.Dispose();
                meta.Loader = null;
            }

            static private void OnAudioDownloadCompleted(StreamingAssetId id, AudioClip clip, string pathOrUrl, AssetMeta meta) {
                ClipMap[id] = clip;
                s_ReverseLookup[clip.GetInstanceID()] = id;

                meta.Status = AssetStatus.Loaded;
                RecomputeMemorySize(ref MemoryUsage, meta, clip);
                UnityEngine.Debug.LogFormat("[Streaming] ...finished loading (async) '{0}'", id);
                InvokeCallbacks(meta, id, clip);
            }

            static private void OnAudioDownloadFail(StreamingAssetId id, string pathOrUrl, AssetMeta meta) {
                UnityEngine.Debug.LogErrorFormat("[Streaming] Failed to load audio '{0}' from '{1}", id, pathOrUrl);
                meta.Loader = null;
                meta.Status = AssetStatus.Error;
                InvokeCallbacks(meta, id, ClipMap[id]);
            }

            #endregion // Load
        
            #region Manifest

            #endregion // Manifest

            static private AudioType GetAudioTypeForURL(string inURL)
            {
                string extension = System.IO.Path.GetExtension(inURL);

                if (extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                    return AudioType.MPEG;
                if (extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
                    return AudioType.OGGVORBIS;
                if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                    return AudioType.WAV;

                if (extension.Equals(".acc", StringComparison.OrdinalIgnoreCase))
                    return AudioType.ACC;
                if (extension.Equals(".aiff", StringComparison.OrdinalIgnoreCase))
                    return AudioType.AIFF;
                if (extension.Equals(".it", StringComparison.OrdinalIgnoreCase))
                    return AudioType.IT;
                if (extension.Equals(".mod", StringComparison.OrdinalIgnoreCase))
                    return AudioType.MOD;
                if (extension.Equals(".mp2", StringComparison.OrdinalIgnoreCase))
                    return AudioType.MPEG;
                if (extension.Equals(".s3m", StringComparison.OrdinalIgnoreCase))
                    return AudioType.S3M;
                if (extension.Equals(".xm", StringComparison.OrdinalIgnoreCase))
                    return AudioType.XM;
                if (extension.Equals(".xma", StringComparison.OrdinalIgnoreCase))
                    return AudioType.XMA;
                if (extension.Equals(".vag", StringComparison.OrdinalIgnoreCase))
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