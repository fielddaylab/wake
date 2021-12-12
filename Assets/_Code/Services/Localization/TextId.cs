using System;
using System.Text;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    [Serializable]
    public struct TextId : IDebugString
#if UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif // UNITY_EDITOR
    {
        #region Inspector

        [SerializeField] private string m_Source;
        [SerializeField] private uint m_Hash;

        #endregion // Inspector

        private TextId(string inSource)
        {
            m_Source = inSource;
            m_Hash = new StringHash32(inSource).HashValue;
        }

        private TextId(StringHash32 inHash)
        {
            m_Source = null;
            m_Hash = inHash.HashValue;
        }

        public string Source()
        {
            return m_Source;
        }

        public bool IsEmpty
        {
            get { return m_Hash == 0; }
        }

        public StringHash32 Hash()
        {
            // #if UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
            // if (!string.IsNullOrEmpty(m_Source))
            //     return new StringHash32(m_Source);
            // return new StringHash32(m_Hash);
            // #else
            return new StringHash32(m_Hash);
            // #endif // UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
        }

        public string ToDebugString()
        {
            return Hash().ToDebugString();
        }

        public override string ToString()
        {
            return Hash().ToString();
        }

        static public implicit operator TextId(string inString)
        {
            return new TextId(inString);
        }

        static public implicit operator TextId(StringHash32 inHash)
        {
            return new TextId(inHash);
        }

#if EXPANDED_REFS
        static public implicit operator StringHash32(in TextId inId)
#else
        static public implicit operator StringHash32(TextId inId)
#endif // EXPANDED_REFS
        {
            return inId.Hash();
        }

        #if UNITY_EDITOR

        public void OnBeforeSerialize()
		{
            uint hash = new StringHash32(m_Source).HashValue;
            if (m_Hash != hash)
            {
                Log.Warn("[TextId] Hash of {0} was different across multiple machines (old {1} vs new {2})", m_Source, m_Hash, hash);
                m_Hash = hash;
            }
		}

        public void OnAfterDeserialize()
        {
            uint hash = new StringHash32(m_Source).HashValue;
            if (m_Hash != hash)
            {
                Log.Warn("[TextId] Hash of {0} was different across multiple machines (old {1} vs new {2})", m_Source, m_Hash, hash);
                m_Hash = hash;
            }
        }

        #endif // UNITY_EDITOR
    }

    static public class TextIdExtensions
    {
        static public StringBuilder AppendLoc(this StringBuilder inBuilder, TextId inTextId)
        {
            inBuilder.Append(Loc.Find(inTextId));
            return inBuilder;
        }

        static public StringBuilder AppendLocLC(this StringBuilder inBuilder, TextId inTextId)
        {
            inBuilder.Append(Loc.Find(inTextId).ToLowerInvariant());
            return inBuilder;
        }

        static public StringBuilder AppendLocUC(this StringBuilder inBuilder, TextId inTextId)
        {
            inBuilder.Append(Loc.Find(inTextId).ToUpperInvariant());
            return inBuilder;
        }
    }
}