#if !UNITY_EDITOR && UNITY_WEBGL
#define USE_JSLIB
#endif // !UNITY_EDITOR && UNITY_WEBGL

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

using AOT;
using UnityEngine.Scripting;

namespace NativeUtils
{
    static public class NativeInput {
        public delegate void PositionCallback(float normalizedX, float normalizedY);
        public delegate void KeyCallback(KeyCode keyCode);

        private delegate void NativeKeyCallback(int keyCode);

        #if USE_JSLIB

        [DllImport("__Internal")]
        static private extern void NativeWebInput_RegisterClick(PositionCallback callbackDown, PositionCallback callbackUp);

        [DllImport("__Internal")]
        static private extern void NativeWebInput_DeregisterClick();

        #endif // USE_JSLIB

        [MonoPInvokeCallback(typeof(PositionCallback)), Preserve]
        static private void BridgeClickDownCallback(float x, float y) {
            if (OnMouseDown != null) {
                OnMouseDown(x, y);
            }
        }

        [MonoPInvokeCallback(typeof(PositionCallback)), Preserve]
        static private void BridgeClickUpCallback(float x, float y) {
            if (OnMouseUp != null) {
                OnMouseUp(x, y);
            }
        }

        [MonoPInvokeCallback(typeof(NativeKeyCallback)), Preserve]
        static private void BridgeKeyCallback(int keyCode) {
            if (OnKeyDown != null) {
                OnKeyDown((KeyCode) keyCode);
            }
        }

        static public event PositionCallback OnMouseDown;
        static public event PositionCallback OnMouseUp;
        static public event KeyCallback OnKeyDown;

        static public void Initialize() {
            #if USE_JSLIB
            NativeWebInput_RegisterClick(BridgeClickDownCallback, BridgeClickUpCallback);
            #else
            if (s_InstantiatedCallback == null) {
                GameObject go = new GameObject("[NativeWebInputMock]");
                go.hideFlags = HideFlags.DontSave;
                GameObject.DontDestroyOnLoad(go);
                s_InstantiatedCallback = go.AddComponent<NativeInputMockCallback>();
            }
            #endif // USE_JSLIB
        }

        static public void Shutdown() {
            #if USE_JSLIB
            NativeWebInput_DeregisterClick();
            #else
            if (s_InstantiatedCallback != null) {
                GameObject.Destroy(s_InstantiatedCallback.gameObject);
                s_InstantiatedCallback = null;
            }
            #endif // USE_JSLIB
        }

        #if !USE_JSLIB

        static private NativeInputMockCallback s_InstantiatedCallback;

        private sealed class NativeInputMockCallback : MonoBehaviour {
            private void Awake() {
                useGUILayout = false;
            }

            private void LateUpdate() {
                if (Input.GetMouseButtonDown(0)) {
                    Vector2 mousePos = Input.mousePosition;
                    BridgeClickDownCallback(mousePos.x / Screen.width, mousePos.y / Screen.height);
                } else if (Input.GetMouseButtonUp(0)) {
                    Vector2 mousePos = Input.mousePosition;
                    BridgeClickUpCallback(mousePos.x / Screen.width, mousePos.y / Screen.height);
                }
            }

            private void OnGUI() {
                Event evt = Event.current;
                if (evt.type == EventType.KeyDown) {
                    if (OnKeyDown != null) {
                        OnKeyDown(evt.keyCode);
                    }
                }
            }
        }

        #endif // !USE_JSLIB
    }
}