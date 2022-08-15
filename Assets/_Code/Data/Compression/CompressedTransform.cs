using UnityEngine;

namespace Aqua.Compression {
    public struct CompressedTransform {
        public ushort PosX;
        public ushort PosY;
        public ushort PosZ;
        public ushort ScaleX;
        public ushort ScaleY;
        public ushort ScaleZ;
        public ushort RotationX;
        public ushort RotationY;
        public ushort RotationZ;

        static public void Compress(in CompressedTransformBounds bounds, Transform transform, out CompressedTransform data) {
            Vector3 localPos = transform.localPosition;
            data.PosX = CompressionRange.Encode16(bounds.Pos, localPos.x);
            data.PosY = CompressionRange.Encode16(bounds.Pos, localPos.y);
            data.PosZ = CompressionRange.Encode16(bounds.Pos, localPos.z);
            
            Vector3 localScale = transform.localScale;
            data.ScaleX = CompressionRange.Encode16(bounds.Scale, localScale.x);
            data.ScaleY = CompressionRange.Encode16(bounds.Scale, localScale.y);
            data.ScaleZ = CompressionRange.Encode16(bounds.Scale, localScale.z);
            
            Vector3 localRot = transform.localEulerAngles;
            data.RotationX = CompressionRange.Encode16(CompressedTransformBounds.Rotation, localRot.x);
            data.RotationY = CompressionRange.Encode16(CompressedTransformBounds.Rotation, localRot.y);
            data.RotationZ = CompressionRange.Encode16(CompressedTransformBounds.Rotation, localRot.z);
        }

        static public void Decompress(in CompressedTransformBounds bounds, in CompressedTransform data, Transform transform) {
            transform.localPosition = new Vector3(CompressionRange.Decode16(bounds.Pos, data.PosX), CompressionRange.Decode16(bounds.Pos, data.PosY), CompressionRange.Decode16(bounds.Pos, data.PosZ));
            transform.localScale = new Vector3(CompressionRange.Decode16(bounds.Scale, data.ScaleX), CompressionRange.Decode16(bounds.Scale, data.ScaleY), CompressionRange.Decode16(bounds.Scale, data.ScaleZ));
            transform.localEulerAngles = new Vector3(CompressionRange.Decode16(CompressedTransformBounds.Rotation, data.RotationX), CompressionRange.Decode16(CompressedTransformBounds.Rotation, data.RotationY), CompressionRange.Decode16(CompressedTransformBounds.Rotation, data.RotationZ));
        }
    }

    public struct CompressedTransformBounds {
        public CompressionRange Pos;
        public CompressionRange Scale;
        // rotation range fixed from 0-360 

        static public readonly CompressionRange Rotation = new CompressionRange(0, 360);

        static private readonly CompressedTransformBounds s_Default = new CompressedTransformBounds() {
            Pos = new CompressionRange(-2048, 2048),
            Scale = new CompressionRange(-128, 128)
        };

        static public CompressedTransformBounds Default { get { return s_Default; } }
    }
}