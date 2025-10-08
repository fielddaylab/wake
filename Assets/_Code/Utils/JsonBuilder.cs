using System.Text;
using BeauUtil;

namespace FieldDay {
    /// <summary>
    /// JSON writing interface.
    /// </summary>
    public interface IJsonBuilder<T> {
        T BeginArray();
        T BeginArray(string name);
        T BeginObject();
        T BeginObject(string name);
        T EndArray();
        T EndObject();
        T Field(string name, bool item);
        T Field(string name, double item);
        T Field(string name, double item, int precision);
        T Field(string name, float item);
        T Field(string name, float item, int precision);
        T Field(string name, long item);
        T Field(string name, string item);
        T Field(string name, StringBuilder item);
        T Field(string name, ulong item);
        T Null(string name);

        T Item(bool item);
        T Item(double item);
        T Item(double item, int precision);
        T Item(float item);
        T Item(float item, int precision);
        T Item(long item);
        T Item(string item);
        T Item(StringBuilder item);
        T Item(ulong item);
        T Null();
    }

    /// <summary>
    /// JSON writing
    /// </summary>
    public class JsonBuilder : IJsonBuilder<JsonBuilder> {
        private readonly StringBuilder m_Builder;

        public JsonBuilder(int capacity) {
            m_Builder = new StringBuilder(capacity);
        }

        #region Object

        public JsonBuilder BeginObject(string name) {
            m_Builder.Append('"').Append(name).Append("\":{");
            return this;
        }

        public JsonBuilder BeginArray(string name) {
            m_Builder.Append('"').Append(name).Append("\":[");
            return this;
        }

        public JsonBuilder BeginObject() {
            m_Builder.Append('{');
            return this;
        }

        public JsonBuilder EndObject() {
            TrimComma(m_Builder);
            m_Builder.Append("},");
            return this;
        }

        #endregion // Object

        #region Array

        public JsonBuilder BeginArray() {
            m_Builder.Append('[');
            return this;
        }

        public JsonBuilder EndArray() {
            TrimComma(m_Builder);
            m_Builder.Append("],");
            return this;
        }

        public JsonBuilder Item(long item) {
            m_Builder.AppendNoAlloc(item).Append(',');
            return this;
        }

        public JsonBuilder Item(ulong item) {
            m_Builder.AppendNoAlloc(item).Append(',');
            return this;
        }

        public JsonBuilder Item(bool item) {
            m_Builder.Append(item ? "true" : "false").Append(',');
            return this;
        }

        public JsonBuilder Item(float item) {
            m_Builder.AppendNoAlloc(item, 2).Append(',');
            return this;
        }

        public JsonBuilder Item(float item, int precision) {
            m_Builder.AppendNoAlloc(item, precision).Append(',');
            return this;
        }

        public JsonBuilder Item(double item) {
            m_Builder.AppendNoAlloc(item, 2).Append(',');
            return this;
        }

        public JsonBuilder Item(double item, int precision) {
            m_Builder.AppendNoAlloc(item, precision).Append(',');
            return this;
        }

        public JsonBuilder Item(string item) {
            if (item == null) {
                m_Builder.Append("null,");
            } else {
                m_Builder.Append('"');
                for (int i = 0, len = item.Length; i < len; i++) {
                    char c = item[i];
                    switch (c) {
                        case '\\': {
                            m_Builder.Append("\\\\");
                            break;
                        }
                        case '\"': {
                            m_Builder.Append("\\\"");
                            break;
                        }
                        case '\n': {
                            m_Builder.Append("\\n");
                            break;
                        }
                        case '\r': {
                            m_Builder.Append("\\r");
                            break;
                        }
                        case '\t': {
                            m_Builder.Append("\\t");
                            break;
                        }
                        case '\b': {
                            m_Builder.Append("\\b");
                            break;
                        }
                        case '\f': {
                            m_Builder.Append("\\f");
                            break;
                        }
                        default: {
                            m_Builder.Append(c);
                            break;
                        }
                    }
                }
                m_Builder.Append("\",");
            }
            return this;
        }

        public JsonBuilder Item(StringBuilder item) {
            if (item == null) {
                m_Builder.Append("null,");
            } else {
                m_Builder.Append('"');
                for (int i = 0, len = item.Length; i < len; i++) {
                    char c = item[i];
                    switch (c) {
                        case '\\': {
                            m_Builder.Append("\\\\");
                            break;
                        }
                        case '\"': {
                            m_Builder.Append("\\\"");
                            break;
                        }
                        case '\n': {
                            m_Builder.Append("\\n");
                            break;
                        }
                        case '\r': {
                            m_Builder.Append("\\r");
                            break;
                        }
                        case '\t': {
                            m_Builder.Append("\\t");
                            break;
                        }
                        case '\b': {
                            m_Builder.Append("\\b");
                            break;
                        }
                        case '\f': {
                            m_Builder.Append("\\f");
                            break;
                        }
                        default: {
                            m_Builder.Append(c);
                            break;
                        }
                    }
                }
                m_Builder.Append("\",");
            }
            return this;
        }

        public JsonBuilder Null() {
            m_Builder.Append("null,");
            return this;
        }

        #endregion // Array

        #region Field

        public JsonBuilder Field(string name, long item) {
            m_Builder.Append('"').Append(name).Append("\":");
            return Item(item);
        }

        public JsonBuilder Field(string name, ulong item) {
            m_Builder.Append('"').Append(name).Append("\":");
            return Item(item);
        }

        public JsonBuilder Field(string name, bool item) {
            m_Builder.Append('"').Append(name).Append("\":");
            return Item(item);
        }

        public JsonBuilder Field(string name, float item) {
            m_Builder.Append('"').Append(name).Append("\":");
            return Item(item);
        }

        public JsonBuilder Field(string name, float item, int precision) {
            m_Builder.Append('"').Append(name).Append("\":");
            return Item(item, precision);
        }

        public JsonBuilder Field(string name, double item) {
            m_Builder.Append('"').Append(name).Append("\":");
            return Item(item);
        }

        public JsonBuilder Field(string name, double item, int precision) {
            m_Builder.Append('"').Append(name).Append("\":");
            return Item(item, precision);
        }

        public JsonBuilder Field(string name, string item) {
            m_Builder.Append('"').Append(name).Append("\":");
            return Item(item);
        }

        public JsonBuilder Field(string name, StringBuilder item) {
            m_Builder.Append('"').Append(name).Append("\":");
            return Item(item);
        }

        public JsonBuilder Null(string name) {
            m_Builder.Append('"').Append(name).Append("\":");
            return Null();
        }

        #endregion // Field

        public void Clear() {
            m_Builder.Length = 0;
        }

        public JsonBuilder Begin() {
            Clear();
            BeginObject();
            return this;
        }

        public StringBuilder End() {
            EndObject();
            TrimComma(m_Builder);
            return m_Builder;
        }

        static private void TrimComma(StringBuilder builder) {
            int len = builder.Length;
            while (len > 0 && builder[len - 1] == ',') {
                len--;
            }
            builder.Length = len;
        }
    }
}