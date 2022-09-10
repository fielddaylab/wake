#if !UNITY_EDITOR && UNITY_WEBGL
#define USE_JSLIB
#endif // !UNITY_EDITOR && UNITY_WEBGL

using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NativeWebUtils {
    static public class NativePreload {
        #if USE_JSLIB

        [DllImport("__Internal")]
        static private extern void NativePreload_Start(string url);

        [DllImport("__Internal")]
        static private extern void NativePreload_Cancel(string url);

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
        /// Preloads the resource with the given url.
        /// </summary>
        static public void Preload(string url) {
            if (url == null || !url.Contains("://")) {
                UnityEngine.Debug.LogWarningFormat("[NativePreload] Cannot preload invalid url '{0}'", url);
                return;
            }

            #if USE_JSLIB
            NativePreload_Start(url);
            #else
            UnityEngine.Debug.LogFormat("[NativePreload] Requested preload of '{0}'", url);
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
            #endif // USE_JSLIB
        }
    }
}