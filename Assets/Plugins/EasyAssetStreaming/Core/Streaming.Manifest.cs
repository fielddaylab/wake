#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.IO;
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

        #region Consts

        public const string ManifestPath = "EasyManifest.json";

        #endregion // Consts

        #region Types

        internal class ManifestData {
            public Dictionary<StreamingAssetId, ManifestEntry> Map = new Dictionary<StreamingAssetId, ManifestEntry>();

            public Textures.DefaultSettings TextureDefaults = Textures.DefaultSettings.Default;
        }

        internal struct ManifestEntry {
            public string Path;
            public AssetType Type;
            [NonSerialized] public StreamingAssetId Id;

            public Textures.TextureSettings Texture;
        }

        #endregion // Types

        static internal class Manifest {
            #region State

            static public readonly ManifestData Current = new ManifestData();
            static public bool Loaded = false;
            static private UnityWebRequest s_Loader;

            #if UNITY_EDITOR
            static private long s_LastModifiedTS;
            static private bool s_Writing;
            #endif // UNITY_EDITOR

            #endregion // State
            
            #region Editor

            #if UNITY_EDITOR

            static public bool ReloadEditor() {
                if (s_Writing) {
                    return true;
                }

                string correctedPath = StreamingPath(ManifestPath);
                long modifiedTime;
                try {
                    modifiedTime = File.GetLastWriteTimeUtc(correctedPath).ToFileTimeUtc();;
                } catch {
                    modifiedTime = 0;
                }

                if (modifiedTime != s_LastModifiedTS) {
                    s_LastModifiedTS = modifiedTime;
                    Load_Editor();
                    return true;
                }

                return false;
            }

            static private void Load_Editor() {
                string correctedPath = StreamingPath(ManifestPath);
                if (File.Exists(correctedPath)) {
                    string json = File.ReadAllText(correctedPath);
                    try {
                        LoadJSON(json, Current);
                        s_LastModifiedTS = File.GetLastWriteTimeUtc(correctedPath).ToFileTimeUtc();
                        if (!Loaded) {
                            Loaded = true;
                            UnityEngine.Debug.LogFormat("[Streaming] Loaded manifest (sync): {0} assets", Current.Map.Count);
                        } else {
                            UnityEngine.Debug.LogFormat("[Streaming] Reloaded manifest (sync): {0} assets", Current.Map.Count);
                        }
                    }
                    catch(Exception e) {
                        UnityEngine.Debug.LogException(e);
                        UnityEngine.Debug.LogErrorFormat("[Streaming] Unable to load streaming manifest (sync)");
                    }
                } else {
                    UnityEngine.Debug.LogWarning("[Streaming] No streaming manifest currently exists");
                    Loaded = true;
                }
            }

            [UnityEditor.MenuItem("Assets/EasyStreamingAssets/Regenerate Manifest File", false, 29)]
            static private void RegenerateManifest() {
                string manifestStreamingPath = StreamingPath(ManifestPath);
                if (File.Exists(manifestStreamingPath)) {
                    EnsureLoaded();
                }

                string fullPathReplace = Path.GetFullPath(Application.streamingAssetsPath).Replace("\\", "/");

                JSON manifestJSON = JSON.CreateObject();

                var assetsJSON = manifestJSON["Assets"] = JSON.CreateObject();

                foreach(var fullPath in Directory.EnumerateFiles(Application.streamingAssetsPath, ".", SearchOption.AllDirectories)) {
                    string ext = Path.GetExtension(fullPath).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || ext == ".meta") {
                        continue;
                    }

                    string relativePath = fullPath.Replace(fullPathReplace, string.Empty).Replace("\\", "/").TrimStart('/');
                    if (relativePath == ManifestPath) {
                        continue;
                    }

                    ManifestEntry entry;
                    StreamingAssetId id = new StreamingAssetId(relativePath, default(AssetType));
                    Current.Map.TryGetValue(id, out entry);

                    JSON entryJSON = assetsJSON[relativePath] = JSON.CreateObject();

                    entry.Path = relativePath;
                    entry.Type = IdentifyAssetType(ext);

                    WriteEnum(entryJSON, "Type", entry.Type);

                    if (entry.Type == AssetType.Texture) {
                        Texture2D texture = null;
                        try {
                            texture = new Texture2D(1, 1);
                            texture.LoadImage(File.ReadAllBytes(fullPath), false);
                            entryJSON["Texture"] = Textures.SerializeTextureSettings(entry.Texture, texture);
                        }
                        finally {
                            StreamingHelper.DestroyResource(texture);
                        }
                    }

                    Current.Map[id] = entry;
                }

                var textureDefaults = manifestJSON["TextureDefaults"] = JSON.CreateObject();
                WriteEnum(textureDefaults, "Filter", Current.TextureDefaults.Filter, Textures.DefaultSettings.Default.Filter);
                WriteEnum(textureDefaults, "Wrap", Current.TextureDefaults.Wrap, Textures.DefaultSettings.Default.Wrap);
                WriteEnum(textureDefaults, "Compression", Current.TextureDefaults.Compression, Textures.DefaultSettings.Default.Compression);

                s_Writing = true;
                try {
                    using(StreamWriter writer = new StreamWriter(File.Open(manifestStreamingPath, FileMode.Create))) {
                        manifestJSON.WriteTo(writer, 4);
                    }
                    UnityEditor.AssetDatabase.ImportAsset("Assets/StreamingAssets/" + ManifestPath, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
                    UnityEditor.EditorUtility.OpenWithDefaultApp(manifestStreamingPath);
                } finally {
                    s_Writing = false;
                }
            }

            #endif // UNITY_EDITOR

            #endregion // Editor

            #region Loading

            static public void EnsureLoaded() {
                if (Loaded) {
                    return;
                }

                #if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying) {
                    Load_Editor();
                } else
                #endif // UNITY_EDITOR
                {
                    LoadAsync();
                }
            }

            static private void LoadAsync() {
                if (s_Loader != null) {
                    return;
                }

                UnityEngine.Debug.LogFormat("[Streaming] Loading manifest...", Current.Map.Count);
                var request = s_Loader = UnityWebRequest.Get(ResolvePathToURL(ManifestPath));
                EnsureTick();
                var op = s_Loader.SendWebRequest();
                op.completed += (_) => {
                    HandleLoaded(request);
                };
            }

            static private void HandleLoaded(UnityWebRequest request) {
                if (s_Loader != request) {
                    request.Dispose();
                    return;
                }

                Loaded = true;
                if (request.isNetworkError || request.isHttpError) {
                    UnityEngine.Debug.LogErrorFormat("[Streaming] Unable to load streaming manifest (async): {0}", request.error);
                } else {
                    try {
                        LoadJSON(request.downloadHandler.text, Current);
                        UnityEngine.Debug.LogFormat("[Streaming] Loaded manifest (async): {0} assets", Current.Map.Count);
                    } catch(Exception e) {
                        UnityEngine.Debug.LogException(e);
                        UnityEngine.Debug.LogErrorFormat("[Streaming] Unable to parse manifest file");
                    }
                }

                request.Dispose();
                s_Loader = null;
            }

            static private void LoadJSON(string jsonString, ManifestData data) {
                JSON json;
                if (string.IsNullOrEmpty(jsonString)) {
                    json = JSON.CreateNull();
                } else {
                    json = JSON.Parse(jsonString);
                }

                var assets = json["Assets"];
                data.Map.Clear();

                foreach(var entryKV in assets.KeyValues) {
                    ManifestEntry entry = new ManifestEntry();
                    entry.Path = entryKV.Key;
                    JSON entryJSON = entryKV.Value;
                    if (entryJSON["Texture"] != null) {
                        entry.Type = AssetType.Texture;
                        entry.Texture = Textures.ParseTextureSettings(entryJSON["Texture"]);
                    }

                    entry.Id = new StreamingAssetId(entry.Path, entry.Type);
                    data.Map.Add(entry.Id, entry);
                }

                var texSettings = json["TextureDefaults"];
                data.TextureDefaults.Filter = ParseEnum(texSettings["Filter"], Textures.DefaultSettings.Default.Filter);
                data.TextureDefaults.Wrap = ParseEnum(texSettings["Wrap"], Textures.DefaultSettings.Default.Wrap);
                data.TextureDefaults.Compression = ParseEnum(texSettings["Compression"], Textures.DefaultSettings.Default.Compression);
            }

            #endregion // Loading

            #region Access

            static public ManifestEntry Entry(StreamingAssetId id, AssetType type) {
                EnsureLoaded();

                ManifestEntry entry = default;
                if (!Current.Map.TryGetValue(id, out entry)) {
                    entry.Id = id;
                    entry.Path = id.ToString();
                    entry.Type = type;
                    Current.Map.Add(id, entry);
                }

                return entry;
            }

            #endregion // Access
        }

        static private T ParseEnum<T>(string data) {
            return (T) Enum.Parse(typeof(T), data);
        }

        static private T ParseEnum<T>(JSON data, T defaultVal) {
            if (data == null) {
                return defaultVal;
            }
            if (data.IsNumber) {
                return (T) Enum.ToObject(typeof(T), data.AsLong);
            }
            return (T) Enum.Parse(typeof(T), data.AsString);
        }

        #if UNITY_EDITOR

        static private AssetType IdentifyAssetType(string extension) {
            switch(extension) {
                case ".png":
                case ".jpg":
                case ".jpeg":
                {
                    return AssetType.Texture;
                }

                case ".webm":
                case ".mp4":
                {
                    return AssetType.Video;
                }

                case ".mp3":
                case ".ogg":
                case ".wav":
                case ".aac":
                {
                    return AssetType.Audio;
                }

                default:
                {
                    return AssetType.Unknown;
                }
            }
        }

        static private void WriteEnum<T>(JSON data, string id, T value) {
            data[id].AsString = value.ToString();
        }

        static private void WriteEnum<T>(JSON data, string id, T value, T defaultVal) {
            if (!EqualityComparer<T>.Default.Equals(value, defaultVal)) {
                data[id].AsString = value.ToString();
            }
        }

        #endif // UNITY_EDITOR
    }
}