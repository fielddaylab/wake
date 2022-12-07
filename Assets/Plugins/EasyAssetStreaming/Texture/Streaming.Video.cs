#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using UnityEngine;
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

        static internal bool IsVideo(string address) {
            return address.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                || address.EndsWith(".webm", StringComparison.OrdinalIgnoreCase);
        }
        
        static internal class Videos {

            #region State

            static public readonly Dictionary<StreamingAssetHandle, VideoPlayer> PlayerMap = new Dictionary<StreamingAssetHandle, VideoPlayer>(MaxVideos);
            static public readonly Dictionary<int, StreamingAssetHandle> PlayerReverseMap = new Dictionary<int, StreamingAssetHandle>(MaxVideos);
            
            static private readonly VideoPlayer[] s_VideoPlayerStack = new VideoPlayer[MaxVideos];
            static private int s_VideoPlayersStackPos = 0;
            static private int s_VideoPlayersInstantiationCount = 0;

            #endregion // State

            #region Load

            static public void StartLoading(StreamingAssetHandle id) {
                VideoPlayer player = PlayerMap[id];
                InvokeLoadBegin(id, null, id.LoadInfo.RetryCount);
                player.Prepare();
            }

            static public VideoPlayer LoadVideoAsync(StreamingAssetHandle id) {
                VideoPlayer player = AllocPlayer();
                PlayerMap[id] = player;
                PlayerReverseMap[player.GetInstanceID()] = id;
                player.source = VideoSource.Url;
                player.url = s_Cache.MetaInfo[id.Index].ResolvedAddress;
                player.enabled = true;
                QueueLoad(id);
                EnsureTick();
                return player;
            }

            static private void HandleVideoPrepareFinished(VideoPlayer player) {
                DecrementLoadCounter();

                StreamingAssetHandle id;
                if (!PlayerReverseMap.TryGetValue(player.GetInstanceID(), out id)) {
                    return;
                }

                if (!s_Cache.IsValid(id)) {
                    return;
                }

                ref AssetStateInfo stateInfo = ref id.StateInfo;

                if (stateInfo.Status == AssetStatus.Unloaded) {
                    return;
                }

                if ((stateInfo.Status & AssetStatus.PendingUnload) != 0) {
                    UnloadSingle(id, 0, 0);
                    return;
                }

                if (player.isPrepared) {
                    OnVideoPrepareCompleted(id, player);
                } else {
                    OnVideoPrepareError(id, player, "unknown error");
                }
            }

            static private void HandleVideoError(VideoPlayer player, string error) {
                UnityEngine.Debug.LogErrorFormat("[Streaming] Video playback error for video '{0}': {1}", player.url, error);
            }

            static private void OnVideoPrepareCompleted(StreamingAssetHandle id, VideoPlayer player) {
                var texture = player.texture;
                id.StateInfo.Status = AssetStatus.Loaded;
                RecomputeMemorySize(ref Textures.MemoryUsage, id, texture);

                UnityEngine.Debug.LogFormat("[Streaming] ...finished loading (async) '{0}'", id.MetaInfo.Address);
                InvokeLoadResult(id, null, LoadResult.Success_Download);
                
                player.Play();
                s_Cache.BindAsset(id, texture);
                InvokeCallbacks(id, texture);
            }

            static private void OnVideoPrepareError(StreamingAssetHandle id, VideoPlayer player, string error) {
                UnityEngine.Debug.LogErrorFormat("[Streaming] Failed to load video '{0}' from '{1}': {2}", id.MetaInfo.Address, id.MetaInfo.ResolvedAddress, error);
                id.StateInfo.Status = AssetStatus.Error;
                InvokeLoadResult(id, null, LoadResult.Error_Unknown); // TODO: Parse out "error"? 
                InvokeCallbacks(id, null);
            }

            #endregion // Load

            #region Unload

            static public void DestroyVideo(StreamingAssetHandle id) {
                VideoPlayer player = PlayerMap[id];
                PlayerMap.Remove(id);
                PlayerReverseMap.Remove(player.GetInstanceID());
                Textures.MemoryUsage.Current -= id.StateInfo.Size;
                FreePlayer(player);
            }

            static public void DestroyAllVideos() {
                foreach(var player in PlayerMap.Values) {
                    FreePlayer(player);
                }

                PlayerMap.Clear();
                PlayerReverseMap.Clear();
            }

            #endregion // Unload

            #region Players

            static private VideoPlayer AllocPlayer() {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    return null;
                }
                #endif // UNITY_EDITOR

                if (s_VideoPlayersStackPos <= 0) {
                    EnsureInitialized();

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
                player.enabled = false;

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