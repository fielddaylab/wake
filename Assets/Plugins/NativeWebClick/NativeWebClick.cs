#if !UNITY_EDITOR && UNITY_WEBGL
#define USE_JSLIB
#endif // !UNITY_EDITOR && UNITY_WEBGL

using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace NativeWebClick
{
    static public class NativeClick {
        public delegate void NativePositionCallback(float normalizedX, float normalizedY);

        #if USE_JSLIB

        [DllImport("__Internal")]
        static private extern void NativeWebClick_Register(NativePositionCallback callback);

        [DllImport("__Internal")]
        static private extern void NativeWebClick_Deregister();

        #endif // USE_JSLIB

        [MonoPInvokeCallback(typeof(NativePositionCallback))]
        static private void BridgeCallback(float x, float y) {
            if (OnMouseDown != null) {
                OnMouseDown(x, y);
            }
        }

        static public event NativePositionCallback OnMouseDown;

        static public void Initialize() {
            #if USE_JSLIB
            NativeWebClick_Register(BridgeCallback);
            #else
            if (s_InstantiatedCallback == null) {
                GameObject go = new GameObject("[NativeWebClickMock]");
                go.hideFlags = HideFlags.DontSave;
                s_InstantiatedCallback = go.AddComponent<MockCallback>();
            }
            #endif // USE_JSLIB
        }

        static public void Shutdown() {
            #if USE_JSLIB
            NativeWebClick_Deregister();
            #else
            if (s_InstantiatedCallback != null) {
                GameObject.Destroy(s_InstantiatedCallback.gameObject);
                s_InstantiatedCallback = null;
            }
            #endif // USE_JSLIB
        }

        #if !USE_JSLIB

        static private MockCallback s_InstantiatedCallback;

        private sealed class MockCallback : MonoBehaviour {
            private void LateUpdate() {
                if (Input.GetMouseButtonDown(0)) {
                    Vector2 mousePos = Input.mousePosition;
                    BridgeCallback(mousePos.x / Screen.width, mousePos.y / Screen.height);
                }
            }
        }

        #endif // !USE_JSLIB
    }
}