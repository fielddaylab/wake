using System.Runtime.InteropServices;
using UnityEngine;

namespace Aqua.Compression {
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct CompressedRectTransform {
        [FieldOffset(0)] public ushort AnchorPosX;
        [FieldOffset(2)] public ushort AnchorPosY;
        [FieldOffset(4)] public ushort SizeDeltaX;
        [FieldOffset(6)] public ushort SizeDeltaY;
        [FieldOffset(8)] public ushort ScaleX;
        [FieldOffset(10)] public ushort ScaleY;
        [FieldOffset(12)] public ushort RotationZ;
        [FieldOffset(14)] public byte AnchorMinX;
        [FieldOffset(15)] public byte AnchorMinY;
        [FieldOffset(16)] public byte AnchorMaxX;
        [FieldOffset(17)] public byte AnchorMaxY;
        [FieldOffset(18)] public byte PivotX;
        [FieldOffset(19)] public byte PivotY;

        static public void Compress(in CompressedRectTransformBounds bounds, RectTransform rect, out CompressedRectTransform data) {
            data.AnchorPosX = CompressionRange.Encode16(bounds.AnchorPos, rect.anchoredPosition.x);
            data.AnchorPosY = CompressionRange.Encode16(bounds.AnchorPos, rect.anchoredPosition.y);
            data.SizeDeltaX = CompressionRange.Encode16(bounds.SizeDelta, rect.sizeDelta.x);
            data.SizeDeltaY = CompressionRange.Encode16(bounds.SizeDelta, rect.sizeDelta.y);
            data.AnchorMinX = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.anchorMin.x);
            data.AnchorMinY = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.anchorMin.y);
            data.AnchorMaxX = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.anchorMax.x);
            data.AnchorMaxY = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.anchorMax.y);
            data.PivotX = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.pivot.x);
            data.PivotY = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.pivot.y);
            data.ScaleX = CompressionRange.Encode16(bounds.Scale, rect.localScale.x);
            data.ScaleY = CompressionRange.Encode16(bounds.Scale, rect.localScale.y);
            data.RotationZ = CompressionRange.Encode16(CompressedRectTransformBounds.RotationZ, rect.localEulerAngles.z);
        }

        static public void Decompress(in CompressedRectTransformBounds bounds, in CompressedRectTransform data, RectTransform rect) {
            rect.anchoredPosition = new Vector2(CompressionRange.Decode16(bounds.AnchorPos, data.AnchorPosX, 0.25f), CompressionRange.Decode16(bounds.AnchorPos, data.AnchorPosY, 0.25f));
            rect.sizeDelta = new Vector2(CompressionRange.Decode16(bounds.SizeDelta, data.SizeDeltaX, 0.5f), CompressionRange.Decode16(bounds.SizeDelta, data.SizeDeltaY, 0.5f));
            rect.anchorMin = new Vector2(CompressionRange.Decode8(CompressionRange.ZeroToOne, data.AnchorMinX), CompressionRange.Decode8(CompressionRange.ZeroToOne, data.AnchorMinY));
            rect.anchorMax = new Vector2(CompressionRange.Decode8(CompressionRange.ZeroToOne, data.AnchorMaxX), CompressionRange.Decode8(CompressionRange.ZeroToOne, data.AnchorMaxY));
            rect.pivot = new Vector2(CompressionRange.Decode8(CompressionRange.ZeroToOne, data.PivotX), CompressionRange.Decode8(CompressionRange.ZeroToOne, data.PivotY));
            rect.localScale = new Vector3(CompressionRange.Decode16(bounds.Scale, data.ScaleX), CompressionRange.Decode16(bounds.Scale, data.ScaleY), 1);
            rect.localEulerAngles = new Vector3(0, 0, CompressionRange.Decode16(CompressedRectTransformBounds.RotationZ, data.RotationZ));
        }
    }

    public struct CompressedRectTransformBounds {
        public CompressionRange AnchorPos;
        public CompressionRange SizeDelta;
        // anchor and pivot ranges fixed from 0-1
        public CompressionRange Scale;
        // rotation range fixed from 0-360 

        static public readonly CompressionRange RotationZ = new CompressionRange(0, 360);

        static private readonly CompressedRectTransformBounds s_Default = new CompressedRectTransformBounds() {
            AnchorPos = new CompressionRange(-2048, 2048),
            SizeDelta = new CompressionRange(-2048, 2048),
            Scale = new CompressionRange(-128, 128)
        };

        static public CompressedRectTransformBounds Default { get { return s_Default; } }
    }
}