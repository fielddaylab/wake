#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;

namespace EasyAssetStreaming {
    internal struct StreamingAssetType : IEquatable<StreamingAssetType> {
        public readonly StreamingAssetTypeId Id;
        public readonly StreamingAssetSubTypeId Sub;

        public StreamingAssetType(StreamingAssetTypeId id, StreamingAssetSubTypeId sub = 0) {
            Id = id;
            Sub = sub;
        }

        public bool Equals(StreamingAssetType other) {
            return Id == other.Id && Sub == other.Sub;
        }

        public override bool Equals(object obj) {
            if (obj is StreamingAssetType) {
                return Equals((StreamingAssetType)obj);
            }

            return false;
        }

        public override string ToString() {
            return string.Format("{0} ({1})", Id.ToString(), Sub.ToString());
        }

        public override int GetHashCode() {
            return (int)Id << 8 | (int)Sub;
        }

        static public implicit operator StreamingAssetType(StreamingAssetTypeId main) {
            return new StreamingAssetType(main);
        }

        static public bool operator ==(StreamingAssetType x, StreamingAssetType y) {
            return x.Equals(y);
        }

        static public bool operator !=(StreamingAssetType x, StreamingAssetType y) {
            return !x.Equals(y);
        }
    }

    internal enum StreamingAssetTypeId : byte {
        Unknown = 0,

        Texture,
        Audio
    }

    internal enum StreamingAssetSubTypeId : byte {
        Default = 0,
        VideoTexture = 1,
    }
}