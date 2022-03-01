/*
 * Copyright (C) 2022. Autumn Beauchesne
 * Author:  Autumn Beauchesne
 * Date:    17 Feb 2022
 * 
 * File:    StreamingAssetId.cs
 * Purpose: Identifier for a Streaming asset.
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using UnityEngine;

namespace EasyAssetStreaming {

    /// <summary>
    /// Streaming asset id.
    /// </summary>
    public struct StreamingAssetId : IEquatable<StreamingAssetId> {
        private readonly string m_Path;
        private readonly int m_Hash;
        private readonly Streaming.AssetType m_Type;

        internal StreamingAssetId(string path, Streaming.AssetType type) {
            m_Path = path;
            m_Hash = path == null ? 0 : Animator.StringToHash(path);
            m_Type = type;
        }

        public bool IsEmpty {
            get { return m_Hash == 0; }
        }

        internal Streaming.AssetType Type {
            get { return m_Type; }
        }

        public bool Equals(StreamingAssetId other) {
            return other.m_Hash == m_Hash;
        }

        #region Overrides

        public override string ToString() {
            return m_Path ?? string.Empty;
        }

        public override int GetHashCode() {
            return m_Hash;
        }

        public override bool Equals(object obj) {
            if (obj is StreamingAssetId) {
                return Equals((StreamingAssetId) obj);
            }
            return false;
        }

        static public bool operator==(StreamingAssetId a, StreamingAssetId b) {
            return a.m_Hash == b.m_Hash;
        }

        static public bool operator!=(StreamingAssetId a, StreamingAssetId b) {
            return a.m_Hash != b.m_Hash;
        }

        static public implicit operator bool(StreamingAssetId id) {
            return id.m_Hash != 0;
        }

        #endregion // Overrides
    }
}