using System;
using UnityEngine;
using System.IO;
using System.Text;

namespace EasyBugReporter {
    internal class TextDumpWriter : IDumpWriter {
        private TextWriter m_TextWriter;

        public void Begin(Stream stream) {
            Begin(new StreamWriter(stream, Encoding.UTF8));
        }

        public void Begin(TextWriter writer) {
            m_TextWriter = writer;
        }

        public void End() {
            m_TextWriter.Flush();
            m_TextWriter.Dispose();
            m_TextWriter = null;
        }

        public void Prelude(string title, DateTime now) {
            if (string.IsNullOrEmpty(title)) {
                title = Application.productName;
            }
            if (now == default) {
                now = DateTime.Now;
            }

            string nowStr = now.ToString();
            int blockWidth = 6 + Math.Max(title.Length, nowStr.Length);
            title = title.PadRight(blockWidth - 6, ' ');
            nowStr = nowStr.PadRight(blockWidth - 6, ' ');
            string divider = new String('-', blockWidth);
            m_TextWriter.WriteLine(divider);
            m_TextWriter.Write("-- ");
            m_TextWriter.Write(title);
            m_TextWriter.WriteLine(" --");
            m_TextWriter.Write("-- ");
            m_TextWriter.Write(nowStr);
            m_TextWriter.WriteLine(" --");
            m_TextWriter.WriteLine(divider);
        }

        public void Header(string text, DumpStyle style) {
            m_TextWriter.Write("-- ");
            m_TextWriter.Write(text);
            m_TextWriter.WriteLine(" --");
        }

        public void Image(Texture2D texture, string imageName) {
            if (!string.IsNullOrEmpty(imageName)) {
                m_TextWriter.WriteLine(imageName);
            }
            m_TextWriter.WriteLine(DumpUtils.TextureToBase64Html(texture));
        }

        public void NextLine() {
            m_TextWriter.WriteLine();
        }

        public void Space() {
            m_TextWriter.WriteLine();
        }

        public void Text(string text, DumpStyle style) {
            m_TextWriter.WriteLine(text);
        }

        public void InlineText(string text, DumpStyle style) {
            m_TextWriter.Write(text);
        }

        #region Section

        public void BeginSection(string sectionName, bool defaultOpen = true, DumpStyle style = default) {
            m_TextWriter.WriteLine();
            m_TextWriter.WriteLine("----------------------------");
            m_TextWriter.WriteLine(sectionName.ToUpperInvariant());
            m_TextWriter.WriteLine();
        }

        public void EndSection() {
        }

        #endregion // Section

        #region Unsupported

        public bool SupportsTables {
            get { return false; }
        }

        public void BeginTable() {
        }

        public void EndTable() {
        }

        public void BeginTableCell(DumpStyle style = default) {
        }

        public void EndTableCell() {
            m_TextWriter.WriteLine();
        }

        public void BeginTableRow(DumpStyle style = default) {
        }

        public void EndTableRow() {
            m_TextWriter.WriteLine();
        }

        #endregion // Unsupported
    }
}