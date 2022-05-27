using System;
using UnityEngine;
using System.IO;
using System.Text;

namespace EasyBugReporter {
    internal class HtmlDumpWriter : IDumpWriter {
        private const string DefaultCSS = @"
body{font-family:sans-serif;width:100%;height:100%}b{font-weight:bold}i{font-style:italic}h1{font-weight:bold;font-size:x-large}h2{font-weight:bold;font-size:larger}h3{font-weight:bold;font-size:large}h4{font-weight:bold}table{border:1px black solid;width:95%;border-collapse:collapse}td{padding:2px}details summary{cursor:pointer}details summary>*{display:inline;border:0px !important}details{border:1px black solid;padding:2px}
        ";
        
        private TextWriter m_TextWriter;
        private string m_CustomCSS;

        public HtmlDumpWriter(string css = null) {
            m_CustomCSS = css;
        }

        public void Begin(Stream stream) {
            Begin(new StreamWriter(stream, Encoding.UTF8));
        }

        public void Begin(TextWriter writer) {
            m_TextWriter = writer;
            m_TextWriter.Write("<html>");
            m_TextWriter.Write("<head>");
            m_TextWriter.Write("<style>");
            m_TextWriter.Write(DefaultCSS);
            m_TextWriter.WriteLine("</style>");
            if (!string.IsNullOrEmpty(m_CustomCSS)) {
                m_TextWriter.Write("<style>");
                m_TextWriter.Write(m_CustomCSS);
                m_TextWriter.WriteLine("</style>");
            }
            m_TextWriter.WriteLine("</head>");
        }

        public void End() {
            m_TextWriter.WriteLine();
            m_TextWriter.Write("</body></html>");
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
            m_TextWriter.Write("<h1>");
            m_TextWriter.Write(title);
            m_TextWriter.WriteLine("</h1>");

            m_TextWriter.Write("<h4>");
            m_TextWriter.Write(now);
            m_TextWriter.WriteLine("</h4>");
        }

        public void Header(string text, DumpStyle style) {
            m_TextWriter.Write("<h3>");
            HtmlText(text);
            m_TextWriter.WriteLine("</h3>");
        }

        public void Image(Texture2D texture, string imageName) {
            if (!string.IsNullOrEmpty(imageName)) {
                HtmlText(string.Format("<h4>{0}</h4>", imageName));
            }
            m_TextWriter.Write("<img src=\"");
            m_TextWriter.Write(DumpUtils.TextureToBase64Html(texture));
            m_TextWriter.Write("\"");
            if (!string.IsNullOrEmpty(imageName)) {
                m_TextWriter.Write(" alt=\"");
                m_TextWriter.Write(imageName);
                m_TextWriter.Write("\"");
            }
            m_TextWriter.WriteLine(">");
        }

        public void NextLine() {
            m_TextWriter.WriteLine("<br/>");
        }

        public void Space() {
            m_TextWriter.WriteLine("<br/><br/>");
        }

        public void Text(string text, DumpStyle style) {
            HtmlTextLine(text);
        }

        public void InlineText(string text, DumpStyle style) {
            HtmlText(text);
        }

        #region Section

        public void BeginSection(string sectionName, bool defaultOpen = true, DumpStyle style = default) {
            if (defaultOpen) {
                m_TextWriter.Write("<br/><details open>\n<summary><h2>");
            } else {
                m_TextWriter.Write("<br/><details>\n<summary><h2>");
            }
            HtmlText(sectionName.ToUpperInvariant());
            m_TextWriter.WriteLine("</h2></summary>");
        }

        public void EndSection() {
            m_TextWriter.WriteLine("</details>");
        }

        #endregion // Section

        #region Unsupported

        public bool SupportsTables {
            get { return true; }
        }

        public void BeginTable() {
            m_TextWriter.WriteLine("<table>");
        }

        public void BeginTableCell(DumpStyle style = default) {
            m_TextWriter.WriteLine("<td {0}>", GetCSSForStyle(style));
        }

        public void BeginTableRow(DumpStyle style = default) {
            m_TextWriter.WriteLine("<tr> {0}", GetCSSForStyle(style));
        }

        public void EndTable() {
            m_TextWriter.WriteLine("</table>");
        }

        public void EndTableCell() {
            m_TextWriter.WriteLine("</td>");
        }

        public void EndTableRow() {
            m_TextWriter.WriteLine();
            m_TextWriter.WriteLine("</tr>");
        }

        private void HtmlText(string text) {
            m_TextWriter.Write(text.Replace("\n", "<br/>"));
        }
        private void HtmlTextLine(string text) {
            m_TextWriter.WriteLine(text.Replace("\n", "<br/>") + "<br/>");
        }

        static private string GetCSSForStyle(DumpStyle style) {
            return string.Empty;
        }

        static private string CSSColor(Color32 color) {
            return ColorUtility.ToHtmlStringRGB(color);
        }

        #endregion // Unsupported
    }
}