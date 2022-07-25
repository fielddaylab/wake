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
using UnityEngine.Video;

namespace EasyAssetStreaming {

    /// <summary>
    /// Asset streaming.
    /// </summary>
    #if UNITY_EDITOR
    public partial class Streaming : UnityEditor.AssetPostprocessor {
    #else
    static public partial class Streaming {
    #endif // UNITY_EDITOR

        internal const int MaxVideos = 16;

        static internal bool IsVideo(string pathOrUrl) {
            return pathOrUrl.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                || pathOrUrl.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
                || pathOrUrl.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
        }
        
        static internal class Videos {

            #region State

            static public readonly Dictionary<StreamingAssetId, VideoPlayer> PlayerMap = new Dictionary<StreamingAssetId, VideoPlayer>(MaxVideos);
            static public readonly Dictionary<int, StreamingAssetId> PlayerReverseMap = new Dictionary<int, StreamingAssetId>(MaxVideos);
            
            static private readonly VideoPlayer[] s_VideoPlayerStack = new VideoPlayer[MaxVideos];
            static private int s_VideoPlayersStackPos = 0;
            static private int s_VideoPlayersInstantiationCount = 0;

            #endregion // State

            static public void StartLoading(StreamingAssetId id, AssetMeta meta) {
                VideoPlayer player = PlayerMap[id];
                player.Prepare();
            }

            static public Texture DestroyVideo(StreamingAssetId id, AssetMeta meta) {
                VideoPlayer player = PlayerMap[id];
                PlayerMap.Remove(id);
                PlayerReverseMap.Remove(player.GetInstanceID());
                Textures.MemoryUsage.Current -= meta.Size;

                Texture tex = player.texture;
                FreePlayer(player);
                return tex;
            }

            static public void DestroyAllVideos() {
                foreach(var player in PlayerMap.Values) {
                    FreePlayer(player);
                }

                PlayerMap.Clear();
                PlayerReverseMap.Clear();
            }

            #region Load

            static public VideoPlayer LoadVideoAsync(StreamingAssetId id, string pathOrUrl, AssetMeta meta) {
                VideoPlayer player = AllocPlayer();
                PlayerReverseMap[player.GetInstanceID()] = id;
                string url = ResolvePathToURL(pathOrUrl);
                player.source = VideoSource.Url;
                player.url = url;
                s_LoadState.Queue.Enqueue(id);
                EnsureTick();
                s_LoadState.Count++;
                return player;
            }

            static private void HandleVideoPrepareFinished(VideoPlayer player) {
                s_LoadState.Count--;

                StreamingAssetId id;
                if (!PlayerReverseMap.TryGetValue(player.GetInstanceID(), out id)) {
                    return;
                }

                AssetMeta meta;
                if (!s_Metas.TryGetValue(id, out meta)) {
                    return;
                }

                string pathOrUrl = meta.Path;

                if (meta.Status == AssetStatus.Unloaded) {
                    return;
                }

                if ((meta.Status & AssetStatus.PendingUnload) != 0) {
                    UnloadSingle(id, 0, 0);
                    return;
                }

                if (player.isPrepared) {
                    OnVideoPrepareCompleted(id, pathOrUrl, meta, player);
                } else {
                    OnVideoPrepareError(id, pathOrUrl, meta, player, "unknown error");
                }
            }

            static private void HandleVideoError(VideoPlayer player, string error) {
                UnityEngine.Debug.LogErrorFormat("[Streaming] Video playback error for video '{0}': {1}", player.url, error);
            }

            static private void OnVideoPrepareCompleted(StreamingAssetId id, string pathOrUrl, AssetMeta meta, VideoPlayer player) {
                var texture = player.texture;
                meta.Status = AssetStatus.Loaded;
                RecomputeMemorySize(ref Textures.MemoryUsage, meta, texture);
                UnityEngine.Debug.LogFormat("[Streaming] ...finished loading (async) '{0}'", id);
                player.Play();
                s_ReverseLookup[texture.GetInstanceID()] = id;
                InvokeCallbacks(meta, id, texture);
            }

            static private void OnVideoPrepareError(StreamingAssetId id, string pathOrUrl, AssetMeta meta, VideoPlayer player, string error) {
                UnityEngine.Debug.LogErrorFormat("[Streaming] Failed to load video '{0}' from '{1}': {2}", id, pathOrUrl, error);
                meta.Status = AssetStatus.Error;
                InvokeCallbacks(meta, id, null);
            }

            #endregion // Load

            #region Players

            static private VideoPlayer AllocPlayer() {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    return null;
                }
                #endif // UNITY_EDITOR

                if (s_VideoPlayersStackPos <= 0) {
                    EnsureTick();
                    s_VideoPlayersInstantiationCount++;
                    if (s_VideoPlayersInstantiationCount > MaxVideos) {
                        UnityEngine.Debug.LogWarningFormat("[Streaming] Too many video players allocated");
                    }

                    var player = s_UpdateHookGO.AddComponent<VideoPlayer>();
                    player.renderMode = VideoRenderMode.APIOnly;
                    player.playOnAwake = false;
                    player.skipOnDrop = false;
                    player.isLooping = true;
                    player.audioOutputMode = VideoAudioOutputMode.None;
                    player.prepareCompleted += HandleVideoPrepareFinished;
                    player.errorReceived += HandleVideoError;
                    return player;
                } else {
                    return s_VideoPlayerStack[--s_VideoPlayersStackPos];
                }
            }

            static private void FreePlayer(VideoPlayer player) {
                player.Stop();

                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    return;
                }
                #endif // UNITY_EDITOR

                if (s_VideoPlayersStackPos >= MaxVideos) {
                    UnityEngine.Debug.LogErrorFormat("[Streaming] Too many video players allocated - discarding");
                    StreamingHelper.DestroyResource(player);
                } else {
                    s_VideoPlayerStack[s_VideoPlayersStackPos++] = player;
                }
            }
        
            #endregion // Players
        }
    }
}