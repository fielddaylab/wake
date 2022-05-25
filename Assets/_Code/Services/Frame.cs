#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using BeauUtil;
using UnityEngine;
using System;
using BeauUtil.Debugger;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua {
    static public unsafe class Frame {
        public const ushort InvalidIndex = ushort.MaxValue;
        private const int HeapSize = 64 * 1024; // 64KB frame heap

        static public ushort Index;
        static internal Unsafe.ArenaHandle FrameHeap;
        static private bool s_HeapInitialized = false;

        static internal void IncrementFrame() {
            Index = (ushort) ((Index + 1) % InvalidIndex);
        }

        #region Buffer

        // TODO: this is not thread safe :/

        static internal void CreateBuffer() {
            DestroyBuffer();
            FrameHeap = Unsafe.CreateArena(HeapSize, "Frame");
            Log.Msg("[Frame] Initialized per-frame heap; size={0}", Unsafe.ArenaSize(FrameHeap));
            s_HeapInitialized = true;
        }

        static internal void ResetBuffer() {
            #if UNITY_EDITOR
            int allocSize = HeapSize - Unsafe.ArenaFreeBytes(FrameHeap);
            if (allocSize > 0) {
                Log.Msg("[Frame] {0} allocated this frame", allocSize);
            }
            #endif // UNITY_EDITOR
            Unsafe.ResetArena(FrameHeap);
        }

        static internal void DestroyBuffer() {
            if (Unsafe.TryFreeArena(ref FrameHeap)) {
                Log.Msg("[Frame] Destroyed per-frame heap");
                s_HeapInitialized = false;
            }
        }

        /// <summary>
        /// Allocates an array. This will be automatically freed at the end of the frame.
        /// </summary>
        static public T* AllocArray<T>(int length) where T : unmanaged {
            T* addr = Unsafe.AllocArray<T>(FrameHeap, length);
            Assert.True(addr != null, "Per-frame heap out of space");
            return addr;
        }

        /// <summary>
        /// Allocates a struct instance on the heap. This will be automatically freed at the end of the frame.
        /// </summary>
        static public T* Alloc<T>() where T : unmanaged {
            T* addr = (T*) Unsafe.Alloc(FrameHeap, sizeof(T));
            Assert.True(addr != null, "Per-frame heap out of space");
            return addr;
        }

        /// <summary>
        /// Allocates an arbitrary buffer on the heap. This will be automatically freed at the end of the frame.
        /// </summary>
        static public void* Alloc(int size) {
            void* addr = Unsafe.Alloc(FrameHeap, size);
            Assert.True(addr != null, "Per-frame heap out of space");
            return addr;
        }

        #endregion // Buffer

        static public bool IsActive(UnityEngine.Object obj) {
            if (!s_HeapInitialized) {
                return false;
            }

            #if UNITY_EDITOR
            if (!Application.IsPlaying(obj) && EditorApplication.isPlayingOrWillChangePlaymode) {
                return false;
            }
            #endif // UNITY_EDITOR

            return true;
        }

        static public bool IsActive(Behaviour obj) {
            if (!s_HeapInitialized || !obj.isActiveAndEnabled) {
                return false;
            }

            #if UNITY_EDITOR
            if (!Application.IsPlaying(obj) && EditorApplication.isPlayingOrWillChangePlaymode) {
                return false;
            }
            #endif // UNITY_EDITOR

            return true;
        }

        static public bool IsActive(GameObject obj) {
            if (!s_HeapInitialized || !obj.activeInHierarchy) {
                return false;
            }

            #if UNITY_EDITOR
            if (!Application.IsPlaying(obj) && EditorApplication.isPlayingOrWillChangePlaymode) {
                return false;
            }
            #endif // UNITY_EDITOR

            return true;
        }

        #if UNITY_EDITOR

        [UnityEditor.InitializeOnLoadMethod]
        static private void EditorInitialize() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            EditorApplication.playModeStateChanged += (s) => {
                if (s == PlayModeStateChange.ExitingEditMode) {
                    DestroyBuffer();
                }
            };
            EditorApplication.quitting += () => DestroyBuffer();
            AppDomain.CurrentDomain.DomainUnload += (_, __) => DestroyBuffer();

            EditorApplication.update += ResetBuffer;

            CreateBuffer();
        }

        #endif // UNITY_EDITOR
    }
}