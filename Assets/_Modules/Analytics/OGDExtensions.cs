#define USING_OGDLOG

using System;
using System.Text;
#if USING_OGDLOG
using OGD;
#endif // USING_OGDLOG

namespace FieldDay.Data {
#if USING_OGDLOG
    /// <summary>
    /// OpenGameData logger extensions.
    /// </summary>
    static public class OGDExtensions {
        internal enum JsonScopeType {
            None,
            Event,
            GameState,
            UserData
        }

        public struct JsonScope : IDisposable, IJsonBuilder<JsonScope> {
            private OGDLog m_Logger;
            private JsonBuilder m_Json;
            private string m_EventName;
            private JsonScopeType m_Type;

            internal JsonScope(OGDLog logger, JsonScopeType scopeType, string eventName, JsonBuilder json) {
                m_Logger = logger;
                m_Type = scopeType;
                m_EventName = eventName;
                m_Json = json;

                json.Begin();
            }

            public void Dispose() {
                switch (m_Type) {
                    case JsonScopeType.Event: {
                        m_Logger.Log(m_EventName, m_Json.End());
                        break;
                    }
                    case JsonScopeType.UserData: {
                        m_Logger.UserData(m_Json.End());
                        break;
                    }
                    case JsonScopeType.GameState: {
                        m_Logger.GameState(m_Json.End());
                        break;
                    }
                }

                m_Json.Clear();
                this = default;
            }

            #region IJsonBuilder

            public JsonScope BeginArray() {
                m_Json.BeginArray();
                return this;
            }

            public JsonScope BeginArray(string name) {
                m_Json.BeginArray(name);
                return this;
            }

            public JsonScope BeginObject() {
                m_Json.BeginObject();
                return this;
            }

            public JsonScope BeginObject(string name) {
                m_Json.BeginObject(name);
                return this;
            }

            public JsonScope EndArray() {
                m_Json.EndArray();
                return this;
            }

            public JsonScope EndObject() {
                m_Json.EndObject();
                return this;
            }

            public JsonScope Field(string name, bool item) {
                m_Json.Field(name, item);
                return this;
            }

            public JsonScope Field(string name, double item) {
                m_Json.Field(name, item);
                return this;
            }

            public JsonScope Field(string name, double item, int precision) {
                m_Json.Field(name, item, precision);
                return this;
            }

            public JsonScope Field(string name, float item) {
                m_Json.Field(name, item);
                return this;
            }

            public JsonScope Field(string name, float item, int precision) {
                m_Json.Field(name, item, precision);
                return this;
            }

            public JsonScope Field(string name, long item) {
                m_Json.Field(name, item);
                return this;
            }

            public JsonScope Field(string name, string item) {
                m_Json.Field(name, item);
                return this;
            }

            public JsonScope Field(string name, StringBuilder item) {
                m_Json.Field(name, item);
                return this;
            }

            public JsonScope Field(string name, ulong item) {
                m_Json.Field(name, item);
                return this;
            }

            public JsonScope Null(string name) {
                m_Json.Null(name);
                return this;
            }

            public JsonScope Item(bool item) {
                m_Json.Item(item);
                return this;
            }

            public JsonScope Item(double item) {
                m_Json.Item(item);
                return this;
            }

            public JsonScope Item(double item, int precision) {
                m_Json.Item(item, precision);
                return this;
            }

            public JsonScope Item(float item) {
                m_Json.Item(item);
                return this;
            }

            public JsonScope Item(float item, int precision) {
                m_Json.Item(item, precision);
                return this;
            }

            public JsonScope Item(long item) {
                m_Json.Item(item);
                return this;
            }

            public JsonScope Item(string item) {
                m_Json.Item(item);
                return this;
            }

            public JsonScope Item(StringBuilder item) {
                m_Json.Item(item);
                return this;
            }

            public JsonScope Item(ulong item) {
                m_Json.Item(item);
                return this;
            }

            public JsonScope Null() {
                m_Json.Null();
                return this;
            }

            #endregion // IJsonBuilder

            static public implicit operator JsonBuilder(JsonScope scope) {
                return scope.m_Json;
            }
        }
    
        static public JsonScope NewEvent(this OGDLog logger, string eventName, JsonBuilder jsonBuilder) {
            return new JsonScope(logger, JsonScopeType.Event, eventName, jsonBuilder);
        }

        static public JsonScope OpenGameState(this OGDLog logger, JsonBuilder jsonBuilder) {
            return new JsonScope(logger, JsonScopeType.GameState, null, jsonBuilder);
        }

        static public JsonScope OpenUserData(this OGDLog logger, JsonBuilder jsonBuilder) {
            return new JsonScope(logger, JsonScopeType.UserData, null, jsonBuilder);
        }
    }
#endif // USING_OGDLOG
}