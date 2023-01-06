/*
 * Copyright (C) 2022. Autumn Beauchesne
 * Author:  Autumn Beauchesne
 * Date:    17 Feb 2022
 * 
 * File:    StreamingAssetHandle.cs
 * Purpose: Handle for a Streaming asset.
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Runtime.CompilerServices;

namespace EasyAssetStreaming {

    /// <summary>
    /// Streaming asset handle.
    /// </summary>
    public struct StreamingAssetHandle : IEquatable<StreamingAssetHandle> {
        
        private const int IndexMask = 0xFFFFFF;
        private const int GenerationMask = 0xFF;
        private const int GenerationShift = 24;

        /// <summary>
        /// Global identifier.
        /// </summary>
        public readonly uint Id;

        internal StreamingAssetHandle(uint id) {
            Id = id;
        }

        internal StreamingAssetHandle(uint index, uint generation) {
            Id = (generation << GenerationShift) | index;
        }

        /// <summary>
        /// Index of identifier.
        /// </summary>
        public uint Index {
            get { return Id & IndexMask; }
        }

        /// <summary>
        /// Generation of index.
        /// </summary>
        public uint Generation {
            get { return (Id >> GenerationShift) & GenerationMask; }
        }

        /// <summary>
        /// If the asset handle is empty.
        /// </summary>
        public bool IsEmpty {
            get { return Id == 0; }
        }

        [MethodImpl(256)]
        public bool Equals(StreamingAssetHandle other) {
            return Id == other.Id;
        }

        #region Cache Access

        internal Streaming.AssetMetaInfo MetaInfo {
            [MethodImpl(256)] get { return Streaming.Cache.MetaInfo[Index]; }
        }

        internal ref Streaming.AssetStateInfo StateInfo {
            [MethodImpl(256)] get { return ref Streaming.Cache.StateInfo[Index]; }
        }

        internal ref Streaming.AssetLoadInfo LoadInfo {
            [MethodImpl(256)] get { return ref Streaming.Cache.LoadInfo[Index]; }
        }

        #if UNITY_EDITOR

        internal ref Streaming.AssetEditorInfo EditorInfo {
            [MethodImpl(256)] get { return ref Streaming.Cache.EditorInfo[Index]; }
        }

        #endif // UNITY_EDITOR

        internal StreamingAssetType AssetType {
            [MethodImpl(256)] get { return Streaming.Cache.MetaInfo[Index].Type; }
        }

        #endregion // Cache Access

        #region Overrides

        public override string ToString() {
            return string.Format("{0}({1})", Index, Generation);
        }

        [MethodImpl(256)]
        public override int GetHashCode() {
            return (int)Id;
        }

        public override bool Equals(object obj) {
            if (obj is StreamingAssetHandle) {
                return Equals((StreamingAssetHandle) obj);
            }
            return false;
        }

        [MethodImpl(256)]
        static public bool operator==(StreamingAssetHandle a, StreamingAssetHandle b) {
            return a.Id == b.Id;
        }

        [MethodImpl(256)]
        static public bool operator!=(StreamingAssetHandle a, StreamingAssetHandle b) {
            return a.Id != b.Id;
        }

        [MethodImpl(256)]
        static public implicit operator bool(StreamingAssetHandle id) {
            return id.Id != 0;
        }

        #endregion // Overrides
    }
}