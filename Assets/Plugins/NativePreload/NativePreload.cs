#if !UNITY_EDITOR && UNITY_WEBGL
#define USE_JSLIB
#endif // !UNITY_EDITOR && UNITY_WEBGL

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NativeWebUtils {
    static public class NativePreload {
        #if USE_JSLIB

        [DllImport("__Internal")]
        static private extern void NativePreload_Start(string url, int resourceType);

        [DllImport("__Internal")]
        static private extern bool NativePreload_IsLoaded(string url);

        [DllImport("__Internal")]
        static private extern void NativePreload_Cancel(string url);

        #else

        static private readonly HashSet<string> s_DebugPreloadedURLS = new HashSet<string>();

        #endif // USE_JSLIB

        /// <summary>
        /// Returns the url for the given streaming assets path.
        /// </summary>
        static public string StreamingAssetsURL(string path) {
            if (path == null) {
                return null;
            } else if (path.Contains("://")) {
                return path;
            } else {
                string streamingPath = Path.Combine(Application.streamingAssetsPath, path).Replace("\\", "/");
                switch (Application.platform) {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.WebGLPlayer:
                        return streamingPath;

                    case RuntimePlatform.WSAPlayerARM:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                        return "file:///" + streamingPath;

                    default:
                        return "file://" + streamingPath;
                }
            }
        }

        /// <summary>
        /// Type of resource
        /// </summary>
        public enum ResourceType {
            Unknown,
            Audio,
            Image,
            Video
        }

        /// <summary>
        /// Preloads the resource with the given url.
        /// </summary>
        static public void Preload(string url, ResourceType resourceType) {
            if (url == null || !url.Contains("://")) {
                UnityEngine.Debug.LogWarningFormat("[NativePreload] Cannot preload invalid url '{0}'", url);
                return;
            }

            #if USE_JSLIB
            NativePreload_Start(url, (int) resourceType);
            #else
            UnityEngine.Debug.LogFormat("[NativePreload] Requested preload of '{0}' of type {1}", url, resourceType);
            s_DebugPreloadedURLS.Add(url);
            #endif // USE_JSLIB
        }

        /// <summary>
        /// Returns if the resource with the given url has been preloaded.
        /// </summary>
        static public bool IsLoaded(string url) {
            if (url == null || !url.Contains("://")) {
                UnityEngine.Debug.LogWarningFormat("[NativePreload] Cannot preload invalid url '{0}'", url);
                return false;
            }

            #if USE_JSLIB
            return NativePreload_IsLoaded(url);
            #else
            return s_DebugPreloadedURLS.Contains(url);
            #endif // USE_JSLIB
        }

        /// <summary>
        /// Cancels any preload of the resource with the given url.
        /// </summary>
        static public void Cancel(string url) {
            if (url == null || !url.Contains("://")) {
                UnityEngine.Debug.LogWarningFormat("[NativePreload] Cannot preload invalid url '{0}'", url);
                return;
            }

            #if USE_JSLIB
            NativePreload_Cancel(url);
            #else
            UnityEngine.Debug.LogFormat("[NativePreload] Requested cancel preload of '{0}'", url);
            s_DebugPreloadedURLS.Remove(url);
            #endif // USE_JSLIB
        }
    }
}