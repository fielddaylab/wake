using System.Runtime.InteropServices;
using BeauUtil;
using EasyAssetStreaming;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Compression {
    [StructLayout(LayoutKind.Sequential)]
    public struct CompressedGraphic {
        public uint Color;

        static public void Compress(Graphic graphic, out CompressedGraphic data) {
            data.Color = Unsafe.Reinterpret<Color32, uint>(graphic.color);
        }

        static public void Compress(SpriteRenderer graphic, out CompressedGraphic data) {
            data.Color = Unsafe.Reinterpret<Color32, uint>(graphic.color);
        }

        static public void Compress(IStreamingTextureComponent graphic, out CompressedGraphic data) {
            data.Color = Unsafe.Reinterpret<Color32, uint>(graphic.Color);
        }

        static public void Decompress(in CompressedGraphic data, Graphic graphic) {
            graphic.color = Unsafe.Reinterpret<uint, Color32>(data.Color);
        }

        static public void Decompress(in CompressedGraphic data, SpriteRenderer graphic) {
            graphic.color = Unsafe.Reinterpret<uint, Color32>(data.Color);
        }

        static public void Decompress(in CompressedGraphic data, IStreamingTextureComponent graphic) {
            graphic.Color = Unsafe.Reinterpret<uint, Color32>(data.Color);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct CompressedStreamingUGUITexture {
        [FieldOffset(0)] public CompressedGraphic Graphic;
        [FieldOffset(4)] public ushort PathIdx;
        [FieldOffset(6)] public byte UV0;
        [FieldOffset(7)] public byte UV1;
        [FieldOffset(8)] public byte UV2;
        [FieldOffset(9)] public byte UV3;
        [FieldOffset(10)] public byte AutoSize;

        static public void Compress(PackageBuilder compressor, StreamingUGUITexture texture, out CompressedStreamingUGUITexture data) {
            CompressedGraphic.Compress(texture, out data.Graphic);

            Rect uv = texture.UVRect;
            data.UV0 = CompressionRange.Encode8(CompressionRange.ZeroToOne, uv.xMin);
            data.UV1 = CompressionRange.Encode8(CompressionRange.ZeroToOne, uv.yMin);
            data.UV2 = CompressionRange.Encode8(CompressionRange.ZeroToOne, uv.xMax);
            data.UV3 = CompressionRange.Encode8(CompressionRange.ZeroToOne, uv.yMax);

            data.AutoSize = (byte) texture.SizeMode;
            data.PathIdx = compressor.AddString(texture.Path);
        }

        static public void Decompress(PackageBank bank, in CompressedStreamingUGUITexture data, StreamingUGUITexture texture) {
            CompressedGraphic.Decompress(data.Graphic, texture);
            
            texture.UVRect = Rect.MinMaxRect(CompressionRange.Decode8(CompressionRange.ZeroToOne, data.UV0), CompressionRange.Decode8(CompressionRange.ZeroToOne, data.UV1),
                CompressionRange.Decode8(CompressionRange.ZeroToOne, data.UV2), CompressionRange.Decode8(CompressionRange.ZeroToOne, data.UV3));

            texture.SizeMode = (AutoSizeMode) data.AutoSize;
            texture.Path = bank.GetString(data.PathIdx);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct CompressedImage {
        [FieldOffset(0)] public CompressedGraphic Graphic;
        [FieldOffset(4)] public ushort SpriteIdx;

        static public void Compress(PackageBuilder compressor, Image image, out CompressedImage data) {
            CompressedGraphic.Compress(image, out data.Graphic);
            data.SpriteIdx = compressor.AddAsset(image.sprite);
        }

        static public void Decompress(PackageBank bank, in CompressedImage data, Image image) {
            CompressedGraphic.Decompress(data.Graphic, image);
            image.sprite = (Sprite) bank.GetAsset(data.SpriteIdx);
        }
    }

    public struct CompressedRectGraphic {
        public CompressedGraphic Graphic;

        static public void Compress(RectGraphic image, out CompressedRectGraphic data) {
            CompressedGraphic.Compress(image, out data.Graphic);
        }

        static public void Decompress(in CompressedRectGraphic data, RectGraphic image) {
            CompressedGraphic.Decompress(data.Graphic, image);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct CompressedTextMeshPro {
        [FieldOffset(0)] public CompressedGraphic Graphic;
        [FieldOffset(4)] public ushort FontIdx;
        [FieldOffset(6)] public ushort FontSize;
        [FieldOffset(8)] public ushort FontStyle;
        [FieldOffset(10)] public ushort Alignment;
        [FieldOffset(12)] public ushort TextIdx;
        [FieldOffset(14)] public byte FontWeight;
        [FieldOffset(15)] public byte CharacterSpacing;
        [FieldOffset(16)] public byte WordSpacing;
        [FieldOffset(17)] public byte LineSpacing;
        [FieldOffset(18)] public byte Wrapping;
        [FieldOffset(19)] public byte Overflow;
        [FieldOffset(20)] public byte Margin0;
        [FieldOffset(21)] public byte Margin1;
        [FieldOffset(22)] public byte Margin2;
        [FieldOffset(23)] public byte Margin3;

        static public void Compress(PackageBuilder compressor, TMP_Text text, out CompressedTextMeshPro data) {
            CompressedGraphic.Compress(text, out data.Graphic);

            data.FontIdx = compressor.AddAsset(text.font);
            data.FontSize = CompressionRange.Encode16(FontSizeRange, text.fontSize);
            data.FontStyle = (ushort) text.fontStyle;
            data.Alignment = (ushort) text.alignment;
            data.TextIdx = compressor.AddString(text.text);

            data.FontWeight = (byte) ((int) text.fontWeight / 100);
            data.CharacterSpacing = CompressionRange.Encode8(SpacingRange, text.characterSpacing);
            data.WordSpacing = CompressionRange.Encode8(SpacingRange, text.wordSpacing);
            data.LineSpacing = CompressionRange.Encode8(SpacingRange, text.lineSpacing);
            data.Wrapping = (byte) (text.enableWordWrapping ? 1 : 0);
            data.Overflow = (byte) text.overflowMode;
            data.Margin0 = CompressionRange.Encode8(MarginRange, text.margin.x);
            data.Margin1 = CompressionRange.Encode8(MarginRange, text.margin.y);
            data.Margin2 = CompressionRange.Encode8(MarginRange, text.margin.z);
            data.Margin3 = CompressionRange.Encode8(MarginRange, text.margin.w);
        }

        static public void Decompress(PackageBank bank, in CompressedTextMeshPro data, TMP_Text text) {
            CompressedGraphic.Decompress(data.Graphic, text);
            
            text.font = (TMP_FontAsset) bank.GetAsset(data.FontIdx);
            text.fontSize = CompressionRange.Decode16(FontSizeRange, data.FontSize, 0.5f);
            text.fontStyle = (FontStyles) data.FontStyle;
            text.alignment = (TextAlignmentOptions) data.Alignment;
            text.text = bank.GetString(data.TextIdx);

            text.fontWeight = (FontWeight) (data.FontWeight * 100);
            text.characterSpacing = CompressionRange.Decode8(SpacingRange, data.CharacterSpacing, 1);
            text.wordSpacing = CompressionRange.Decode8(SpacingRange, data.WordSpacing, 1);
            text.lineSpacing = CompressionRange.Decode8(SpacingRange, data.LineSpacing, 1);
            text.enableWordWrapping = data.Wrapping > 0;
            text.overflowMode = (TextOverflowModes) data.Overflow;
            text.margin = new Vector4(CompressionRange.Decode8(MarginRange, data.Margin0), CompressionRange.Decode8(MarginRange, data.Margin1),
                CompressionRange.Decode8(MarginRange, data.Margin2), CompressionRange.Decode8(MarginRange, data.Margin3));
        }

        static private readonly CompressionRange FontSizeRange = new CompressionRange(0, 512);
        static private readonly CompressionRange SpacingRange = new CompressionRange(0, 100);
        static private readonly CompressionRange MarginRange = new CompressionRange(0, 100);
    }

    public struct CompressedLocText {
        public StringHash32 Id;

        static public void Compress(LocText text, out CompressedLocText data) {
            data.Id = text.m_DefaultText;
        }

        static public void Decompress(in CompressedLocText data, LocText text) {
            text.InternalSetText(Loc.Find(data.Id));
        }
    }
}