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

        #region Public API

        /// <summary>
        /// Loads a texture from a given url.
        /// Returns if the "texture" parameter has changed.
        /// </summary>
        static public bool Texture(string address, ref Texture texture, AssetCallback callback = null) {
            if (string.IsNullOrEmpty(address)) {
                return Unload(ref texture, callback);
            }

            EnsureInitialized();

            Texture loadedTexture;
            StreamingAssetHandle id = Textures.GetHandle(address, out loadedTexture);

            if (texture != loadedTexture) {
                Dereference(texture, callback);
                texture = loadedTexture;

                ref AssetStateInfo state = ref id.StateInfo;
                state.RefCount++;
                state.LastAccessedTS = CurrentTimestamp();
                state.Status &= ~AssetStatus.PendingUnload;
                AddCallback(id, texture, callback);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Loads a texture from a given url.
        /// Returns if the "assetId" parameter has changed.
        /// </summary>
        static public bool Texture(string address, ref StreamingAssetHandle assetId, AssetCallback callback = null) {
            if (string.IsNullOrEmpty(address)) {
                return Unload(ref assetId, callback);
            }

            EnsureInitialized();

            Texture loadedTexture;
            StreamingAssetHandle id = Textures.GetHandle(address, out loadedTexture);

            if (assetId != id) {
                Dereference(assetId, callback);
                assetId = id;
                
                ref AssetStateInfo state = ref id.StateInfo;
                state.RefCount++;
                state.LastAccessedTS = CurrentTimestamp();
                state.Status &= ~AssetStatus.PendingUnload;
                AddCallback(id, loadedTexture, callback);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Loads a texture from a given url.
        /// </summary>
        static public StreamingAssetHandle Texture(string address, AssetCallback callback = null) {
            if (string.IsNullOrEmpty(address)) {
                return default;
            }

            EnsureInitialized();

            Texture loadedTexture;
            StreamingAssetHandle id = Textures.GetHandle(address, out loadedTexture);

            ref AssetStateInfo state = ref id.StateInfo;
            state.RefCount++;
            state.LastAccessedTS = CurrentTimestamp();
            state.Status &= ~AssetStatus.PendingUnload;
            AddCallback(id, loadedTexture, callback);

            return id;
        }

        /// <summary>
        /// Dereferences the given texture.
        /// </summary>
        static public bool Unload(ref Texture texture, AssetCallback callback = null) {
            if (!object.ReferenceEquals(texture, null)) {
                Dereference(texture, callback);
                texture = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the total number of streamed texture bytes.
        /// </summary>
        static public MemoryStat TextureMemoryUsage() {
            return Textures.MemoryUsage;
        }

        /// <summary>
        /// Returns the budgeted number of streamed texture bytes tolerated.
        /// If not 0, this will attempt to unload unreferenced textures when this is exceeded.
        /// </summary>
        static public long TextureMemoryBudget {
            get { return Textures.MemoryBudget; }
            set { Textures.MemoryBudget = value; }
        }

        #endregion // Public API

        /// <summary>
        /// Internal Texture api
        /// </summary>
        static internal class Textures {
            
            #region Consts

            static private readonly Color32[] PlaceholderTextureBytes = new Color32[] {
                Color.black, Color.white, Color.white, Color.black
            };

            #endregion // Consts

            #region Types

            public struct DefaultSettings {
                public FilterMode Filter;
                public TextureWrapMode Wrap;
                public TextureCompression Compression;

                static public readonly DefaultSettings Default = new DefaultSettings() {
                    Filter = FilterMode.Bilinear,
                    Wrap = TextureWrapMode.Clamp,
                    Compression = TextureCompression.None
                };
            }

            public struct TextureSettings {
                public int Width;
                public int Height;
                public bool Alpha;

                public LoadedFilterMode Filter;
                public LoadedTextureCompression CompressionLevel;

                public LoadedTextureWrapMode Wrap;
                public LoadedTextureWrapMode WrapU;
                public LoadedTextureWrapMode WrapV;
                public LoadedTextureWrapMode WrapW;
            }

            public enum LoadedTextureWrapMode {
                Inherit = 0,

                Repeat = 1,
                Clamp = 2,
                Mirror = 3,
                MirrorOnce = 4
            }

            public enum LoadedFilterMode {
                Inherit = 0,

                Point = 1,
                Bilinear = 2,
                Trilinear = 3
            }

            public enum TextureCompression {
                None,

                Low,
                Normal,
                High,
            }

            public enum LoadedTextureCompression {
                Inherit = 0,
                None,

                Low,
                Normal,
                High,
            }

            #endregion // Types

            #region State

            static public readonly Dictionary<StreamingAssetHandle, Texture> TextureMap = new Dictionary<StreamingAssetHandle, Texture>();
            static public MemoryStat MemoryUsage = default;
            static public long MemoryBudget = 0;

            #endregion // State

            [MethodImpl(256)]
            static private bool IsLoadableVideo(string address) {
                #if UNITY_EDITOR
                return UnityEditor.EditorApplication.isPlaying && IsVideo(address);
                #else
                return IsVideo(address);
                #endif // UNITY_EDITOR
            }

            static public StreamingAssetHandle GetHandle(string address, out Texture texture) {
                StreamingAssetHandle handle;
                uint hash = AddressKey(address);
                if (!s_Cache.ByAddressHash.TryGetValue(hash, out handle)) {
                    UnityEngine.Debug.LogFormat( "[Streaming] Loading streamed texture '{0}'...", address);

                    bool isVideo = IsLoadableVideo(address);
                    if (isVideo) {
                        handle = s_Cache.AllocSlot(address, new StreamingAssetType(StreamingAssetTypeId.Texture, StreamingAssetSubTypeId.VideoTexture));
                        VideoPlayer player = Videos.LoadVideoAsync(handle);
                        texture = player.texture;
                    } else {
                        handle = s_Cache.AllocSlot(address, StreamingAssetTypeId.Texture);
                        #if UNITY_EDITOR
                        if (!UnityEditor.EditorApplication.isPlaying) {
                            texture = LoadTexture_Editor(handle);
                        } else
                        #endif // UNITY_EDITOR
                        {
                            texture = LoadTextureAsync(handle, address);
                        }

                        TextureMap[handle] = texture;
                        s_Cache.BindAsset(handle, texture);
                    }
                } else {
                    if (handle.AssetType.Sub == StreamingAssetSubTypeId.VideoTexture) {
                        texture = Videos.PlayerMap[handle].texture;
                    } else {
                        texture = TextureMap[handle];
                    }
                }

                return handle;
            }

            static public void StartLoading(StreamingAssetHandle id) {
                string url = id.MetaInfo.ResolvedAddress;
                var request = id.LoadInfo.Loader = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
                request.downloadHandler = new DownloadHandlerBuffer();
                var sent = request.SendWebRequest();
                sent.completed += (_) => {
                    HandleTextureUWRFinished(id);
                };
            }

            static public void DestroyTexture(StreamingAssetHandle id) {
                Texture texture = TextureMap[id];
                TextureMap.Remove(id);
                MemoryUsage.Current -= id.StateInfo.Size;

                StreamingHelper.DestroyResource(texture);
            }

            static public void DestroyAllTextures() {
                foreach(var texture in TextureMap.Values) {
                    StreamingHelper.DestroyResource(texture);
                }

                TextureMap.Clear();
                MemoryUsage.Current = 0;
            }

            #region Placeholder

            static private Texture2D CreatePlaceholder(string name, bool final) {
                Texture2D texture;
                texture = new Texture2D(2, 2, TextureFormat.RGB24, false, false);
                texture.name = name;
                texture.hideFlags = HideFlags.DontSave;
                ApplyPlaceholderData(texture, final);
                return texture;
            }

            static private void ApplyPlaceholderData(Texture2D texture, bool final) {
                if (texture.width != 2 || texture.height != 2) {
                    texture.Resize(2, 2);
                }
                texture.SetPixels32(PlaceholderTextureBytes);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.Apply(false, final);
            }

            #endregion // Placeholder

            #region Load

            static private Texture2D LoadTextureAsync(StreamingAssetHandle id, string address) {
                Texture2D texture = CreatePlaceholder(address, false);
                s_Cache.BindAsset(id, texture);
                QueueLoad(id);
                RecomputeMemorySize(ref MemoryUsage, id, texture);
                return texture;
            }

            static private void HandleTextureUWRFinished(StreamingAssetHandle id) {
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

                if (request.isNetworkError || request.isHttpError) {
                    if (loadInfo.RetryCount < RetryLimit && StreamingHelper.ShouldRetry(request)) {
                        UnityEngine.Debug.LogWarningFormat("[Streaming] Retrying texture load '{0}' from '{1}': {2}", id.MetaInfo.Address, id.MetaInfo.ResolvedAddress, loadInfo.Loader.error);
                        loadInfo.RetryCount++;
                        QueueDelayedLoad(id, RetryDelayBase + (loadInfo.RetryCount - 1) * RetryDelayExtra);
                        return;
                    }
                    OnTextureDownloadFail(id, request.error);
                } else {
                    OnTextureDownloadCompleted(id, request.downloadHandler.data);
                }

                request.Dispose();
                loadInfo.Loader = null;
            }

            static private void OnTextureDownloadCompleted(StreamingAssetHandle id, byte[] source) {
                Texture2D texture = (Texture2D) TextureMap[id]; 
                try {
                    var settings = ApplySettings(id, texture);
                    TextureCompression compression = ResolveCompression(settings.CompressionLevel);
                    texture.LoadImage(source, compression == 0);
                    PostApplySettings(texture, settings, compression, true);
                } catch(Exception e) {
                    UnityEngine.Debug.LogException(e);
                    OnTextureDownloadFail(id, e.ToString());
                    return;
                }

                id.StateInfo.Status = AssetStatus.Loaded;
                id.LoadInfo.RetryCount = 0;
                RecomputeMemorySize(ref MemoryUsage, id, texture);
                UnityEngine.Debug.LogFormat("[Streaming] ...finished loading (async) '{0}'", id.MetaInfo.Address);
                InvokeCallbacks(id, texture);
            }

            static private void OnTextureDownloadFail(StreamingAssetHandle id, string error) {
                UnityEngine.Debug.LogErrorFormat("[Streaming] Failed to load texture '{0}' from '{1}': {2}", id.MetaInfo.Address, id.MetaInfo.ResolvedAddress, error);
                id.StateInfo.Status = AssetStatus.Error;
                InvokeCallbacks(id, TextureMap[id]);
            }

            #endregion // Load

            #region Settings

            static private TextureSettings ApplySettings(StreamingAssetHandle id, Texture2D texture) {
                AssetMetaInfo meta = id.MetaInfo;
                TextureSettings settings = Manifest.Entry(meta.AddressHash, meta.Address, StreamingAssetTypeId.Texture).Texture;

                texture.filterMode = ResolveFilterMode(settings.Filter);

                if (settings.Wrap != LoadedTextureWrapMode.Inherit) {
                    texture.wrapMode = ResolveWrapMode(settings.Wrap);
                } else {
                    texture.wrapModeU = ResolveWrapMode(settings.WrapU);
                    texture.wrapModeV = ResolveWrapMode(settings.WrapV);
                    texture.wrapModeW = ResolveWrapMode(settings.WrapW);
                }
 
                return settings;
            }

            static private void PostApplySettings(Texture2D texture, TextureSettings settings, TextureCompression compression, bool final) {
                if (compression > 0 && texture.isReadable) {
                    texture.Compress(compression == TextureCompression.High);
                    if (final) {
                        texture.Apply(false, true);
                    }
                }
            }

            static private FilterMode ResolveFilterMode(LoadedFilterMode mode) {
                if (mode == LoadedFilterMode.Inherit) {
                    return Manifest.Current.TextureDefaults.Filter;
                } else {
                    return (FilterMode) (mode - 1);
                }
            }

            static private TextureCompression ResolveCompression(LoadedTextureCompression mode) {
                if (mode == LoadedTextureCompression.Inherit) {
                    return Manifest.Current.TextureDefaults.Compression;
                } else {
                    return (TextureCompression) (mode - 1);
                }
            }

            static private TextureWrapMode ResolveWrapMode(LoadedTextureWrapMode mode) {
                if (mode == LoadedTextureWrapMode.Inherit) {
                    return Manifest.Current.TextureDefaults.Wrap;
                } else {
                    return (TextureWrapMode) (mode - 1);
                }
            }

            #endregion // Settings

            #region Formats

            static private readonly TextureFormat[] FormatMappingsWebGL = new TextureFormat[] {
                TextureFormat.RGB24, TextureFormat.ARGB32,
                TextureFormat.DXT1, TextureFormat.DXT5,
                TextureFormat.DXT1, TextureFormat.DXT5,
                TextureFormat.DXT1, TextureFormat.DXT5
            };

            static private readonly TextureFormat[] FormatMappingsDesktop = new TextureFormat[] {
                TextureFormat.RGB24, TextureFormat.ARGB32,
                TextureFormat.DXT1, TextureFormat.DXT5,
                TextureFormat.DXT1, TextureFormat.DXT5,
                TextureFormat.BC7, TextureFormat.BC7
            };

            static private readonly TextureFormat[] FormatMappingsAndroid = new TextureFormat[] {
                TextureFormat.RGB24, TextureFormat.ARGB32,
                TextureFormat.ETC_RGB4, TextureFormat.ETC2_RGBA1,
                TextureFormat.ETC_RGB4, TextureFormat.ETC2_RGBA8,
                TextureFormat.BC7, TextureFormat.BC7
            };

            static private readonly TextureFormat[] FormatMappingsIOS = new TextureFormat[] {
                TextureFormat.RGB24, TextureFormat.ARGB32,
                TextureFormat.PVRTC_RGB2, TextureFormat.PVRTC_RGBA2,
                TextureFormat.PVRTC_RGB4, TextureFormat.PVRTC_RGBA4,
                TextureFormat.PVRTC_RGB4, TextureFormat.PVRTC_RGBA4
            };

            static private readonly TextureFormat[] FormatMappingsDefault = new TextureFormat[] {
                TextureFormat.RGB24, TextureFormat.ARGB32,
                TextureFormat.RGBA4444, TextureFormat.RGBA4444,
                TextureFormat.RGBA4444, TextureFormat.RGBA4444,
                TextureFormat.RGBA4444, TextureFormat.RGBA4444
            };

            static private TextureFormat DesiredFormat(TextureCompression compression, bool hasAlpha) {
                int idx = 2 * (int) compression + (hasAlpha ? 1 : 0);
                switch(Application.platform) {
                    case RuntimePlatform.Android: {
                        return FormatMappingsAndroid[idx];
                    }

                    case RuntimePlatform.IPhonePlayer: {
                        return FormatMappingsIOS[idx];
                    }

                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.LinuxPlayer: {
                        return FormatMappingsDesktop[idx];
                    }

                    case RuntimePlatform.WebGLPlayer: {
                        return FormatMappingsIOS[idx];
                    }

                    default: {
                        return FormatMappingsDefault[idx];
                    }
                }
            }

            #endregion // Formats
        
            #region Budget

            static private bool s_OverBudgetFlag;

            static public void CheckBudget(long now) {
                if (MemoryBudget <= 0) {
                    return;
                }

                long over = MemoryUsage.Current - MemoryBudget;
                if (over > 0) {
                    if (!s_OverBudgetFlag) {
                        UnityEngine.Debug.LogFormat("[Streaming] Texture memory is over budget by {0:0.00} Kb", over / 1024f);
                        s_OverBudgetFlag = true;
                    }
                    StreamingAssetHandle asset = IdentifyOverBudgetToDelete(StreamingAssetTypeId.Texture, now, over);
                    if (asset) {
                        UnloadSingle(asset, now);
                        s_OverBudgetFlag = false;
                    }
                } else {
                    s_OverBudgetFlag = false;
                }
            }

            #endregion // Budget

            #region Manifest

            static public TextureSettings ParseTextureSettings(JSON data) {
                TextureSettings settings;
                settings.Width = data["Width"].AsInt;
                settings.Height = data["Height"].AsInt;
                settings.Alpha = data["Alpha"] == null ? true : data["Alpha"].AsBool;
                settings.CompressionLevel = ParseEnum<LoadedTextureCompression>(data["Compression"], LoadedTextureCompression.Inherit);
                settings.Filter = ParseEnum<LoadedFilterMode>(data["Filter"], LoadedFilterMode.Inherit);
                settings.Wrap = ParseEnum<LoadedTextureWrapMode>(data["Wrap"], LoadedTextureWrapMode.Inherit);
                settings.WrapU = ParseEnum<LoadedTextureWrapMode>(data["WrapU"], LoadedTextureWrapMode.Inherit);
                settings.WrapV = ParseEnum<LoadedTextureWrapMode>(data["WrapV"], LoadedTextureWrapMode.Inherit);
                settings.WrapW = ParseEnum<LoadedTextureWrapMode>(data["WrapW"], LoadedTextureWrapMode.Inherit);
                return settings;
            }

            #if UNITY_EDITOR

            static public JSON SerializeTextureSettings(TextureSettings settings, Texture2D texture) {
                JSON json = JSON.CreateObject();
                json["Width"].AsInt = texture.width;
                json["Height"].AsInt = texture.height;
                json["Alpha"].AsBool = HasAlpha(texture);

                WriteEnum(json, "Compression", settings.CompressionLevel, LoadedTextureCompression.Inherit);
                WriteEnum(json, "Filter", settings.Filter, LoadedFilterMode.Inherit);
                WriteEnum(json, "Wrap", settings.Wrap, LoadedTextureWrapMode.Inherit);
                WriteEnum(json, "WrapU", settings.WrapU, LoadedTextureWrapMode.Inherit);
                WriteEnum(json, "WrapV", settings.WrapV, LoadedTextureWrapMode.Inherit);
                WriteEnum(json, "WrapW", settings.WrapW, LoadedTextureWrapMode.Inherit);
                return json;
            }

            static private bool HasAlpha(Texture2D texture) {
                foreach(var pixel in texture.GetPixels32()) {
                    if (pixel.a < 255) {
                        return true;
                    }
                }

                return false;
            }

            #endif // UNITY_EDITOR

            #endregion // Manifest

            #region Editor

            #if UNITY_EDITOR

            static private Texture2D LoadTexture_Editor(StreamingAssetHandle id) {
                string address = id.MetaInfo.Address;
                if (IsURL(address)) {
                    UnityEngine.Debug.LogErrorFormat("[Streaming] Cannot load texture from URL when not in playmode '{0}'", address);
                    UnityEngine.Debug.LogErrorFormat("[Streaming] Failed to load texture from '{0}'", address);
                    Texture2D texture = CreatePlaceholder(address, true);
                    id.StateInfo.Status = AssetStatus.Error;
                    RecomputeMemorySize(ref MemoryUsage, id, texture);
                    return texture;
                }

                string correctedPath = StreamingPath(address);
                if (File.Exists(correctedPath)) {
                    byte[] bytes = File.ReadAllBytes(correctedPath);
                    Texture2D texture = new Texture2D(1, 1);
                    texture.name = address;
                    texture.hideFlags = HideFlags.DontSaveInEditor;
                    var settings = ApplySettings(id, texture);
                    TextureCompression compression = ResolveCompression(settings.CompressionLevel);
                    texture.LoadImage(bytes, false);
                    PostApplySettings(texture, settings, compression, false);
                    RecomputeMemorySize(ref MemoryUsage, id, texture);
                    UnityEngine.Debug.LogFormat("[Streaming] ...finished loading (sync) '{0}'", id.MetaInfo.Address);
                    ref AssetEditorInfo editorInfo = ref id.EditorInfo;
                    editorInfo.Path = correctedPath;
                    try {
                        editorInfo.EditTime = File.GetLastWriteTimeUtc(correctedPath).ToFileTimeUtc();
                    } catch {
                        editorInfo.EditTime = 0;
                    }
                    id.StateInfo.Status = AssetStatus.Loaded;
                    return texture;
                } else {
                    UnityEngine.Debug.LogErrorFormat("[Streaming] Failed to load texture from '{0}' - file does not exist", address);
                    Texture2D texture = CreatePlaceholder(address, true);
                    id.StateInfo.Status = AssetStatus.Error;
                    RecomputeMemorySize(ref MemoryUsage, id, texture);
                    return texture;
                }
            }

            static public void HandleTextureDeleted(StreamingAssetHandle id) {
                Texture2D texture = (Texture2D) TextureMap[id];
                ApplyPlaceholderData(texture, true);
                RecomputeMemorySize(ref MemoryUsage, id, texture);
                id.EditorInfo.EditTime = 0;

                id.StateInfo.Status = AssetStatus.Error;
                UnityEngine.Debug.LogFormat("[Streaming] Texture '{0}' was deleted", id.MetaInfo.Address);
                InvokeCallbacks(id, texture);
            }

            static public void HandleTextureModified(StreamingAssetHandle id) {
                Texture2D texture = (Texture2D) TextureMap[id];
                var settings = ApplySettings(id, texture);
                TextureCompression compression = ResolveCompression(settings.CompressionLevel);
                
                ref AssetEditorInfo editorInfo = ref id.EditorInfo;

                texture.LoadImage(File.ReadAllBytes(editorInfo.Path), false);
                PostApplySettings(texture, settings, compression, false);
                RecomputeMemorySize(ref MemoryUsage, id, texture);
                editorInfo.EditTime = File.GetLastWriteTimeUtc(editorInfo.Path).ToFileTimeUtc();

                UnityEngine.Debug.LogFormat("[Streaming] Texture '{0}' reloaded", id.MetaInfo.Address);
                InvokeCallbacks(id, texture);
            }

            #endif // UNITY_EDITOR

            #endregion // Editor
        }
    }
}