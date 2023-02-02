#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyAssetStreaming {

#if UNITY_EDITOR
    public partial class Streaming : UnityEditor.AssetPostprocessor {
    #else
    static public partial class Streaming {
    #endif // UNITY_EDITOR

        #region Types

        /// <summary>
        /// State change callback.
        /// </summary>
        public delegate void AssetCallback(StreamingAssetHandle id, AssetStatus status, object asset);

        /// <summary>
        /// Asset status.
        /// </summary>
        public enum AssetStatus : byte {
            Unloaded = 0,
            Invalid = 0x01,

            PendingUnload = 0x02,
            PendingLoad = 0x04,
            Loading = 0x08,
            Loaded = 0x10,
            Error = 0x20,
        }

        // invariant information
        internal struct AssetMetaInfo {
            public StreamingAssetType Type;
            public string Address;
            public string ResolvedAddress;
            public uint AddressHash;
        }

        // current state
        internal struct AssetStateInfo {
            public long LastAccessedTS;
            public ushort RefCount;
            public AssetStatus Status;
            public long Size;
            public int InstanceId;
        }

        // loading info
        internal struct AssetLoadInfo {
            public UnityWebRequest Loader;
            public ushort RetryCount;
        }

        // callbacks
        internal struct AssetCallbackInfo {
            public AssetCallback First;
            public List<AssetCallback> List;
        }

        #if UNITY_EDITOR

        internal struct AssetEditorInfo {
            public string Path;
            public long EditTime;
        }

        #endif // UNITY_EDITOR

        [StructLayout(LayoutKind.Sequential)]
        internal struct AssetMetaSlot {
            public short Next;
            public byte Generation;
            public bool Alive;

            static public readonly AssetMetaSlot Default = new AssetMetaSlot() {
                Next = -1,
                Generation = 0,
                Alive = false
            };
        }

        internal class AssetInfoCache {
            public const int DefaultLookupSize = 32;

            public Dictionary<uint, StreamingAssetHandle> ByAddressHash;
            public Dictionary<int, StreamingAssetHandle> ByInstanceId;

            public AssetMetaInfo[] MetaInfo;
            public AssetStateInfo[] StateInfo;
            public AssetLoadInfo[] LoadInfo;
            public AssetCallbackInfo[] CallbackInfo;

            #if UNITY_EDITOR
            public AssetEditorInfo[] EditorInfo;
            #endif // UNITY_EDITOR

            private Stack<List<AssetCallback>> m_CallbackListPool;

            private AssetMetaSlot[] m_Slots;
            private uint m_SlotCount;
            private short m_NextFreeSlot;

            public void Init() {
                MetaInfo = new AssetMetaInfo[DefaultLookupSize];
                StateInfo = new AssetStateInfo[DefaultLookupSize];
                LoadInfo = new AssetLoadInfo[DefaultLookupSize];
                CallbackInfo = new AssetCallbackInfo[DefaultLookupSize];

                #if UNITY_EDITOR
                EditorInfo = new AssetEditorInfo[DefaultLookupSize];
                #endif // UNITY_EDITOR

                m_Slots = new AssetMetaSlot[DefaultLookupSize];
                m_NextFreeSlot = -1;
                m_SlotCount = 0;

                for(int i = 0; i < DefaultLookupSize; i++) {
                    m_Slots[i] = AssetMetaSlot.Default;
                }

                ByAddressHash = new Dictionary<uint, StreamingAssetHandle>(DefaultLookupSize);
                ByInstanceId = new Dictionary<int, StreamingAssetHandle>(DefaultLookupSize);

                m_CallbackListPool = new Stack<List<AssetCallback>>();
            }

            public void Clear() {
                m_SlotCount = 0;
                m_NextFreeSlot = -1;

                for(int i = 0; i < m_Slots.Length; i++) {
                    ref AssetMetaInfo metaInfo = ref MetaInfo[i];
                    ref AssetLoadInfo loadInfo = ref LoadInfo[i];
                    ref AssetStateInfo stateInfo = ref StateInfo[i];
                    ref AssetCallbackInfo callbackInfo = ref CallbackInfo[i];

                    #if UNITY_EDITOR
                    EditorInfo[i] = default(AssetEditorInfo);
                    #endif // UNITY_EDITOR

                    if (loadInfo.Loader != null) {
                        loadInfo.Loader.Dispose();
                        if (OnLoadResult != null) {
                            InvokeLoadResult(new StreamingAssetHandle((uint) i, m_Slots[i].Generation), loadInfo.Loader, LoadResult.Cancelled);
                        }
                    }

                    if (callbackInfo.List != null) {
                        FreeCallbackList(callbackInfo.List);
                    }

                    metaInfo = default(AssetMetaInfo);
                    loadInfo = default(AssetLoadInfo);
                    stateInfo = default(AssetStateInfo);
                    callbackInfo = default(AssetCallbackInfo);

                    m_Slots[i] = AssetMetaSlot.Default;
                }

                ByAddressHash.Clear();
                ByInstanceId.Clear();
            }

            [MethodImpl(256)]
            public bool IsValid(StreamingAssetHandle handle) {
                if (handle.Index >= m_SlotCount) {
                    return false;
                }

                ref AssetMetaSlot slot = ref m_Slots[handle.Index];
                return slot.Alive && slot.Generation == handle.Generation;
            }

            #region Associations

            public void BindAsset(StreamingAssetHandle handle, UnityEngine.Object asset) {
                if (!IsValid(handle)) {
                    return;
                }

                int instanceId = !asset ? 0 : asset.GetInstanceID();
                StateInfo[handle.Index].InstanceId = instanceId;
                ByInstanceId[instanceId] = handle;
            }

            #endregion // Associations

            #region Slot Allocation

            /// <summary>
            /// Allocates a slot.
            /// </summary>
            public StreamingAssetHandle AllocSlot(string address, StreamingAssetType type) {
                int slotIdx;
                if (m_NextFreeSlot >= 0) {
                    slotIdx = (int) m_NextFreeSlot;
                } else {
                    slotIdx = (int) m_SlotCount++;
                    if (m_SlotCount >= m_Slots.Length) {
                        ExpandSlots(m_Slots.Length + 32);
                    }
                }

                ref AssetMetaSlot slot = ref m_Slots[slotIdx];
                m_NextFreeSlot = slot.Next;
                slot.Next = -1;
                slot.Generation = (byte) (slot.Generation < 255 ? slot.Generation + (byte) 1 : (byte) 1);
                slot.Alive = true;

                var handle = new StreamingAssetHandle((uint) slotIdx, slot.Generation);
                
                // meta info
                ref AssetMetaInfo info = ref MetaInfo[slotIdx];
                info.Address = address;
                info.AddressHash = AddressKey(address);
                info.ResolvedAddress = ResolveAddressToURL(address);
                info.Type = type;

                // associate hash
                ByAddressHash[info.AddressHash] = handle;
                return handle;
            }

            /// <summary>
            /// Frees the given slot.
            /// NOTE: This will not unload referenced data. Take care of that prior to calling this method.
            /// </summary>
            public bool FreeSlot(StreamingAssetHandle handle) {
                if (handle.Index >= m_SlotCount) {
                    return false;
                }

                ref AssetMetaSlot slot = ref m_Slots[handle.Index];
                if (!slot.Alive || slot.Generation != handle.Generation) {
                    return false;
                }
                
                int idx = (int) handle.Index;
                
                slot.Alive = false;
                slot.Next = m_NextFreeSlot;
                m_NextFreeSlot = (short) idx;

                ref AssetMetaInfo meta = ref MetaInfo[idx];
                ref AssetStateInfo state = ref StateInfo[idx];
                ref AssetLoadInfo load = ref LoadInfo[idx];
                ref AssetCallbackInfo callbacks = ref CallbackInfo[idx];

                if (callbacks.List != null) {
                    FreeCallbackList(callbacks.List);
                    callbacks.List = null;
                }

                callbacks.First = null;

                if (meta.AddressHash != 0) {
                    ByAddressHash.Remove(meta.AddressHash);
                }
                if (state.InstanceId != 0) {
                    ByInstanceId.Remove(state.InstanceId);
                }

                if (load.Loader != null) {
                    load.Loader.Dispose();
                }

                meta = default(AssetMetaInfo);
                state = default(AssetStateInfo);
                load = default(AssetLoadInfo);
                callbacks = default(AssetCallbackInfo);
                #if UNITY_EDITOR
                EditorInfo[idx] = default(AssetEditorInfo);
                #endif // UNITY_EDITOR

                return true;
            }

            private void ExpandSlots(int newSize) {
                int startIdx = m_Slots.Length;
                Array.Resize(ref m_Slots, newSize);
                for(int i = startIdx; i < newSize; i++) {
                    m_Slots[i] = AssetMetaSlot.Default;
                }

                Array.Resize(ref MetaInfo, newSize);
                Array.Resize(ref StateInfo, newSize);
                Array.Resize(ref LoadInfo, newSize);
                Array.Resize(ref CallbackInfo, newSize);
                
                #if UNITY_EDITOR
                Array.Resize(ref EditorInfo, newSize);
                #endif // UNITY_EDITOR
            }

            #endregion // Slot Allocation

            #region Callback List

            public List<AssetCallback> AllocCallbackList() {
                if (m_CallbackListPool.Count == 0) {
                    return new List<AssetCallback>(4);
                } else {
                    return m_CallbackListPool.Pop();
                }
            }

            private void FreeCallbackList(List<AssetCallback> callbackList) {
                callbackList.Clear();
                m_CallbackListPool.Push(callbackList);
            }
        
            #endregion // Callback List
        }

        #endregion // Types
    
        #region Storage

        static private AssetInfoCache s_Cache;

        static private void EnsureCache() {
            if (s_Cache != null) {
                return;
            }

            s_Cache = new AssetInfoCache();
            s_Cache.Init();
        }

        static internal AssetInfoCache Cache {
            get { 
                EnsureCache();
                return s_Cache;
            }
        }

        /// <summary>
        /// Returns the handle for the given loaded asset.
        /// </summary>
        static public StreamingAssetHandle Handle(UnityEngine.Object instance) {
            if (!instance || s_Cache == null) {
                return default(StreamingAssetHandle);
            }

            StreamingAssetHandle handle;
            int id = instance.GetInstanceID();
            if (!s_Cache.ByInstanceId.TryGetValue(id, out handle)) {
                UnityEngine.Debug.LogWarningFormat("[Streaming] No asset handle found associated with object '{0}'", instance.ToString());
            }
            return handle;
        }

        /// <summary>
        /// Returns the handle for the given path.
        /// </summary>
        static public StreamingAssetHandle Handle(string path) {
            if (string.IsNullOrEmpty(path) || s_Cache == null) {
                return default(StreamingAssetHandle);
            }

            StreamingAssetHandle handle;
            uint hash = AddressKey(path);
            s_Cache.ByAddressHash.TryGetValue(hash, out handle);
            return handle;
        }

        #endregion // Storage

        #region Callbacks

        /// <summary>
        /// Adds a callback to the given asset.
        /// </summary>
        static internal void AddCallback(StreamingAssetHandle handle, object asset, AssetCallback callback) {
            if (callback == null) {
                return;
            }

            AssetStatus status = s_Cache.StateInfo[handle.Index].Status;
            ref AssetCallbackInfo callbackInfo = ref s_Cache.CallbackInfo[handle.Index];
            if (callbackInfo.First == null) {
                callbackInfo.First = callback;
            } else {
                if (callbackInfo.List == null) {
                    callbackInfo.List = s_Cache.AllocCallbackList();
                }
                callbackInfo.List.Add(callback);
            }

            if (status != AssetStatus.PendingLoad) {
                callback(handle, status, asset);
            }
        }

        /// <summary>
        /// Removes a callback from the given asset.
        /// </summary>
        static internal void RemoveCallback(StreamingAssetHandle handle, AssetCallback callback) {
            if (callback == null) {
                return;
            }

            ref AssetCallbackInfo callbackInfo = ref s_Cache.CallbackInfo[handle.Index];
            if (callbackInfo.First == callback) {
                callbackInfo.First = null;
            } else if (callbackInfo.List != null) {
                callbackInfo.List.FastRemove(callback);
            }
        }

        /// <summary>
        /// Invokes callbacks for the given asset.
        /// </summary>
        static internal void InvokeCallbacks(StreamingAssetHandle handle, object asset) {
            AssetStatus status = s_Cache.StateInfo[handle.Index].Status;
            AssetCallbackInfo callbackInfo = s_Cache.CallbackInfo[handle.Index];
            if (callbackInfo.First != null) {
                callbackInfo.First(handle, status, asset);
            }
            if (callbackInfo.List != null) {
                for(int i = 0, len = callbackInfo.List.Count; i < len; i++) {
                    callbackInfo.List[i](handle, status, asset);
                }
            }
        }

        #endregion // Callbacks
    }
}