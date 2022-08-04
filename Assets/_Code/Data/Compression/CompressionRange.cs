using System.Runtime.CompilerServices;
using UnityEngine;

namespace Aqua.Compression {
    public struct CompressionRange {
        public readonly float Min;
        public readonly float Max;

        public CompressionRange(float min, float max) {
            Min = min;
            Max = max;
        }

        static public readonly CompressionRange ZeroToOne = new CompressionRange(0, 1);

        private const byte Limit8 = 1 << 7;
        private const ushort Limit16 = 1 << 15;

        [MethodImpl(256)]
        static public byte Encode8(CompressionRange range, float value) {
            float inv = Mathf.Clamp01((value - range.Min) / (range.Max - range.Min));
            return (byte) (inv * Limit8);
        }

        [MethodImpl(256)]
        static public float Decode8(CompressionRange range, byte value) {
            float lerp = (float) value / Limit8;
            return range.Min + (range.Max - range.Min) * lerp;
        }

        [MethodImpl(256)]
        static public float Decode8(CompressionRange range, byte value, float quantize) {
            float lerp = (float) value / Limit8;
            return Quantize(range.Min + (range.Max - range.Min) * lerp, quantize);
        }

        [MethodImpl(256)]
        static public ushort Encode16(CompressionRange range, float value) {
            float inv = Mathf.Clamp01((value - range.Min) / (range.Max - range.Min));
            return (ushort) (inv * Limit16);
        }

        [MethodImpl(256)]
        static public float Decode16(CompressionRange range, ushort value) {
            float lerp = (float) value / Limit16;
            return range.Min + (range.Max - range.Min) * lerp;
        }

        [MethodImpl(256)]
        static public float Decode16(CompressionRange range, ushort value, float quantize) {
            float lerp = (float) value / Limit16;
            return Quantize(range.Min + (range.Max - range.Min) * lerp, quantize);
        }

        [MethodImpl(256)]
        static public float Quantize(float value, float increment) {
            return increment * Mathf.Round(value / increment);
        }

        [MethodImpl(256)]
        static public float Quantize(float value) {
            return Mathf.Round(value);
        }
    }
}