using System;
using UnityEngine;
using System.IO;

namespace EasyBugReporter {

    /// <summary>
    /// Interface for writing data dumps.
    /// </summary>
    public interface IDumpWriter {
        // beginning
        void Begin(Stream stream);
        void Begin(TextWriter writer);
        void End();

        // prelude
        void Prelude(string title, DateTime now);

        // sections
        void BeginSection(string sectionName, bool defaultOpen = true, DumpStyle style = default);
        void EndSection();
        
        // contents
        void Header(string text, DumpStyle style = default);
        void Text(string text, DumpStyle style = default);
        void InlineText(string text, DumpStyle style = default);
        void Image(Texture2D texture, string imageName = null);
        
        // spacing
        void NextLine();
        void Space();

        // Layout
        bool SupportsTables { get; }
        void BeginTable();
        void EndTable();
        void BeginTableRow(DumpStyle style = default);
        void EndTableRow();
        void BeginTableCell(DumpStyle style = default);
        void EndTableCell();
    }

    /// <summary>
    /// Visual customization for dump information.
    /// </summary>
    public struct DumpStyle {
        public FontStyle FontStyle;
        public Color32 Color;
        public Color32 BackgroundColor;

        public DumpStyle(FontStyle style, Color32 color, Color32 bg) {
            FontStyle = style;
            Color = color;
            BackgroundColor = bg;
        }

        static public implicit operator DumpStyle(Color color) {
            return new DumpStyle(FontStyle.Normal, color, default);
        }

        static public implicit operator DumpStyle(Color32 color) {
            return new DumpStyle(FontStyle.Normal, color, default);
        }

        static public implicit operator DumpStyle(FontStyle style) {
            return new DumpStyle(style, default, default);
        }
    }

    static public class DumpUtils {
        static public readonly DumpStyle DefaultTableHeaderStyle = new DumpStyle(FontStyle.Bold, default, default);

        static public string TextureToBase64(Texture2D texture) {
            return Convert.ToBase64String(texture.EncodeToPNG());
        }

        static public string TextureToBase64Html(Texture2D texture) {
            return "data:image/png;base64," + TextureToBase64(texture);
        }

        static public void TableCellText(this IDumpWriter writer, string text, DumpStyle style = default) {
            writer.BeginTableCell();
            writer.InlineText(text, style);
            writer.EndTableCell();
        }

        static public void TableCellHeader(this IDumpWriter writer, string text) {
            writer.BeginTableCell();
            writer.InlineText(text, DefaultTableHeaderStyle);
            writer.EndTableCell();
        }

        static public void TableCellHeader(this IDumpWriter writer, string text, DumpStyle style) {
            writer.BeginTableCell();
            writer.InlineText(text, style);
            writer.EndTableCell();
        }

        static public void KeyValue(this IDumpWriter writer, string label, object value, DumpStyle style = default) {
            writer.Text(string.Format("{0}: {1}", label, value), style);
        }
    }

    /// <summary>
    /// Format to write data dumps to.
    /// </summary>
    public enum DumpFormat : byte {
        Html = 0,
        Text = 1,

        Custom = 255
    }
}