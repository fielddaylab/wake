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
        /// Loads a texture from a given url.
        /// Returns if the "texture" parameter has changed.
        /// </summary>
        static public bool Texture(string pathOrUrl, ref Texture2D texture, AssetCallback callback = null) {
            if (string.IsNullOrEmpty(pathOrUrl)) {
                return Unload(ref texture, callback);
            }

            Manifest.EnsureLoaded();

            StreamingAssetId id = new StreamingAssetId(pathOrUrl, AssetType.Texture);
            Texture2D loadedTexture;
            AssetMeta meta = Textures.GetMeta(id, pathOrUrl, out loadedTexture);

            if (texture != loadedTexture) {
                Dereference(texture, callback);
                texture = loadedTexture;
                meta.RefCount++;
                meta.LastModifiedTS = CurrentTimestamp();
                meta.Status &= ~AssetStatus.PendingUnload;
                AddCallback(meta, id, texture, callback);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Loads a texture from a given url.
        /// </summary>
        static public StreamingAssetId Texture(string pathOrUrl, AssetCallback callback = null) {
            if (string.IsNullOrEmpty(pathOrUrl)) {
                return default;
            }

            Manifest.EnsureLoaded();

            StreamingAssetId id = new StreamingAssetId(pathOrUrl, AssetType.Texture);
            Texture2D loadedTexture;
            AssetMeta meta = Textures.GetMeta(id, pathOrUrl, out loadedTexture);

            meta.RefCount++;
            meta.LastModifiedTS = CurrentTimestamp();
            meta.Status &= ~AssetStatus.PendingUnload;
            AddCallback(meta, id, loadedTexture, callback);

            return id;
        }

        /// <summary>
        /// Dereferences the given texture.
        /// </summary>
        static public bool Unload(ref Texture2D texture, AssetCallback callback = null) {
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

            static public readonly Dictionary<StreamingAssetId, Texture2D> TextureMap = new Dictionary<StreamingAssetId, Texture2D>();
            static public MemoryStat MemoryUsage = default;

            #endregion // State

            static public AssetMeta GetMeta(StreamingAssetId id, string pathOrUrl, out Texture2D texture) {
                Texture2D loadedTexture;
                AssetMeta meta;
                if (!s_Metas.TryGetValue(id, out meta)) {
                    meta = new AssetMeta();

                    UnityEngine.Debug.LogFormat( "[Streaming] Loading streamed texture '{0}'...", id);
                    
                    meta.Type = AssetType.Texture;
                    meta.Status = AssetStatus.PendingLoad;
                    meta.Path = pathOrUrl;
                    #if UNITY_EDITOR
                    if (!UnityEditor.EditorApplication.isPlaying) {
                        loadedTexture = LoadTexture_Editor(id, pathOrUrl, meta);
                    } else
                    #endif // UNITY_EDITOR
                    {
                        loadedTexture = LoadTextureAsync(id, pathOrUrl, meta);
                    }

                    s_Metas[id] = meta;
                    TextureMap[id] = loadedTexture;
                    s_ReverseLookup[loadedTexture.GetInstanceID()] = id;
                } else {
                    loadedTexture = TextureMap[id];
                }

                texture = loadedTexture;
                return meta;
            }

            static public void StartLoading(StreamingAssetId id, AssetMeta meta) {
                Texture2D texture = TextureMap[id];
                var sent = meta.Loader.SendWebRequest();
                sent.completed += (_) => {
                    HandleTextureUWRFinished(id, meta.Path, meta, meta.Loader);
                };
            }

            static public UnityEngine.Object DestroyTexture(StreamingAssetId id, AssetMeta meta) {
                Texture2D texture = TextureMap[id];
                TextureMap.Remove(id);
                MemoryUsage.Current -= meta.Size;

                StreamingHelper.DestroyResource(texture);
                return texture;
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

            #region Editor

            #if UNITY_EDITOR

            static private Texture2D LoadTexture_Editor(StreamingAssetId id, string pathOrUrl, AssetMeta meta) {
                if (IsURL(pathOrUrl)) {
                    UnityEngine.Debug.LogErrorFormat("[Streaming] Cannot load texture from URL when not in playmode '{0}'", pathOrUrl);
                    UnityEngine.Debug.LogErrorFormat("[Streaming] Failed to load texture from '{0}'", pathOrUrl);
                    Texture2D texture = CreatePlaceholder(pathOrUrl, true);
                    meta.Status = AssetStatus.Error;
                    RecomputeMemorySize(ref MemoryUsage, meta, texture);
                    return texture;
                }

                string correctedPath = StreamingPath(pathOrUrl);
                if (File.Exists(correctedPath)) {
                    byte[] bytes = File.ReadAllBytes(correctedPath);
                    Texture2D texture = new Texture2D(1, 1);
                    texture.name = pathOrUrl;
                    texture.hideFlags = HideFlags.DontSave;
                    var settings = ApplySettings(id, texture);
                    TextureCompression compression = ResolveCompression(settings.CompressionLevel);
                    texture.LoadImage(bytes, false);
                    PostApplySettings(texture, settings, compression, false);
                    RecomputeMemorySize(ref MemoryUsage, meta, texture);
                    UnityEngine.Debug.LogFormat("[Streaming] ...finished loading (sync) '{0}'", id);
                    meta.EditorPath = correctedPath;
                    try {
                        meta.EditorEditTime = File.GetLastWriteTimeUtc(correctedPath).ToFileTimeUtc();
                    } catch {
                        meta.EditorEditTime = 0;
                    }
                    meta.Status = AssetStatus.Loaded;
                    return texture;
                } else {
                    UnityEngine.Debug.LogErrorFormat("[Streaming] Failed to load texture from '{0}' - file does not exist", pathOrUrl);
                    Texture2D texture = CreatePlaceholder(pathOrUrl, true);
                    meta.Status = AssetStatus.Error;
                    RecomputeMemorySize(ref MemoryUsage, meta, texture);
                    return texture;
                }
            }

            static public void HandleTextureDeleted(StreamingAssetId id, AssetMeta meta) {
                Texture2D texture = TextureMap[id];
                ApplyPlaceholderData(texture, true);
                RecomputeMemorySize(ref MemoryUsage, meta, texture);
                meta.EditorEditTime = 0;

                meta.Status = AssetStatus.Error;
                UnityEngine.Debug.LogFormat("[Streaming] Texture '{0}' was deleted", id);
                InvokeCallbacks(meta, id, texture);
            }

            static public void HandleTextureModified(StreamingAssetId id, AssetMeta meta) {
                Texture2D texture = TextureMap[id];
                var settings = ApplySettings(id, texture);
                TextureCompression compression = ResolveCompression(settings.CompressionLevel);
                texture.LoadImage(File.ReadAllBytes(meta.EditorPath), false);
                PostApplySettings(texture, settings, compression, false);
                RecomputeMemorySize(ref MemoryUsage, meta, texture);
                meta.EditorEditTime = File.GetLastWriteTimeUtc(meta.EditorPath).ToFileTimeUtc();

                UnityEngine.Debug.LogFormat("[Streaming] Texture '{0}' reloaded", id);
                InvokeCallbacks(meta, id, texture);
            }

            #endif // UNITY_EDITOR

            #endregion // Editor
        
            #region Load

            static private Texture2D LoadTextureAsync(StreamingAssetId id, string pathOrUrl, AssetMeta meta) {
                Texture2D texture = CreatePlaceholder(pathOrUrl, false);
                string url = ResolvePathToURL(pathOrUrl);
                var request = meta.Loader = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
                request.downloadHandler = new DownloadHandlerBuffer();
                s_LoadState.Queue.Enqueue(id);
                EnsureTick();
                RecomputeMemorySize(ref MemoryUsage, meta, texture);
                s_LoadState.Count++;
                return texture;
            }

            static private void HandleTextureUWRFinished(StreamingAssetId id, string pathOrUrl, AssetMeta meta, UnityWebRequest request) {
                s_LoadState.Count--;

                if (meta.Status == AssetStatus.Unloaded) {
                    return;
                }

                if ((meta.Status & AssetStatus.PendingUnload) != 0) {
                    UnloadSingle(id, 0, 0);
                    return;
                }

                if (request.isNetworkError || request.isHttpError) {
                    OnTextureDownloadFail(id, pathOrUrl, meta, request.error);
                } else {
                    OnTextureDownloadCompleted(id, request.downloadHandler.data, pathOrUrl, meta);
                }

                request.Dispose();
                meta.Loader = null;
            }

            static private void OnTextureDownloadCompleted(StreamingAssetId id, byte[] source, string pathOrUrl, AssetMeta meta) {
                Texture2D texture = TextureMap[id]; 
                try {
                    var settings = ApplySettings(id, texture);
                    TextureCompression compression = ResolveCompression(settings.CompressionLevel);
                    texture.LoadImage(source, compression == 0);
                    PostApplySettings(texture, settings, compression, true);
                } catch(Exception e) {
                    UnityEngine.Debug.LogException(e);
                    OnTextureDownloadFail(id, pathOrUrl, meta, e.ToString());
                    return;
                }

                meta.Status = AssetStatus.Loaded;
                RecomputeMemorySize(ref MemoryUsage, meta, texture);
                UnityEngine.Debug.LogFormat("[Streaming] ...finished loading (async) '{0}'", id);
                InvokeCallbacks(meta, id, texture);
            }

            static private void OnTextureDownloadFail(StreamingAssetId id, string pathOrUrl, AssetMeta meta, string error) {
                UnityEngine.Debug.LogErrorFormat("[Streaming] Failed to load texture '{0}' from '{1}': {2}", id, pathOrUrl, error);
                meta.Loader = null;
                meta.Status = AssetStatus.Error;
                InvokeCallbacks(meta, id, TextureMap[id]);
            }

            static private TextureSettings ApplySettings(StreamingAssetId id, Texture2D texture) {
                TextureSettings settings = Manifest.Entry(id, AssetType.Texture).Texture;

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

            #endregion // Load

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

            static public JSON SerializeTextureSettings(TextureSettings settings) {
                JSON json = JSON.CreateObject();
                if (settings.Width != 0) {
                    json["Width"].AsInt = settings.Width;
                }
                if (settings.Height != 0) {
                    json["Height"].AsInt = settings.Height;
                }
                if (!settings.Alpha) {
                    json["Alpha"].AsBool = false;
                }
                WriteEnum(json, "Compression", settings.CompressionLevel, LoadedTextureCompression.Inherit);
                WriteEnum(json, "Filter", settings.Filter, LoadedFilterMode.Inherit);
                WriteEnum(json, "Wrap", settings.Wrap, LoadedTextureWrapMode.Inherit);
                WriteEnum(json, "WrapU", settings.WrapU, LoadedTextureWrapMode.Inherit);
                WriteEnum(json, "WrapV", settings.WrapV, LoadedTextureWrapMode.Inherit);
                WriteEnum(json, "WrapW", settings.WrapW, LoadedTextureWrapMode.Inherit);
                return json;
            }

            #endif // UNITY_EDITOR

            #endregion // Manifest
        }
    }
}