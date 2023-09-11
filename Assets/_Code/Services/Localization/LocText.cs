using System;
using System.Runtime.CompilerServices;
using BeauPools;
using BeauUtil;
using BeauUtil.Tags;
using TMPro;
using UnityEngine;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua {
    [RequireComponent(typeof(TMP_Text)), DisallowMultipleComponent]
    public class LocText : MonoBehaviour {
        [Serializable] public class Pool : SerializablePool<LocText> { }

        public struct TextMetrics {
            public int VisibleCharCount;
            public int RichCharCount;
        }

        #region Inspector

        [SerializeField, HideInEditor] private TMP_Text m_Text = null;
        [SerializeField] internal TextId m_DefaultText = default(TextId);
        [SerializeField] private bool m_TintSprites = false;

        [Header("Modifications")]
        [SerializeField] private string m_Prefix = null;
        [SerializeField] private string m_Postfix = null;

        #endregion // Inspector

        [NonSerialized] private TextId m_LastId;
        [NonSerialized] private TextMetrics m_LastMetrics;
        [NonSerialized] private bool m_Initialized;

        #region Text

        public TMP_Text Graphic { get { return m_Text; } }
        public TextMetrics Metrics { get { return m_LastMetrics; } }

        public void SetText(TextId inId, object inContext = null) {
            m_LastId = inId;

            if (inId.IsEmpty) {
                m_LastMetrics = default;
                InternalSetText(string.Empty);
                return;
            }

            TagString shared = SharedTagString();
            if (Services.Loc.LocalizeTagged(ref shared, inId, inContext)) {
                InternalSetText(shared.RichText);
                m_LastMetrics.VisibleCharCount = shared.VisibleText.Length;
                m_LastMetrics.RichCharCount = shared.RichText.Length;
            } else {
                InternalSetText(string.Format("<color=red>ERROR:</color> {0}", inId.Source()));
                m_LastMetrics.VisibleCharCount = m_Text.text.Length;
                m_LastMetrics.RichCharCount = m_Text.textInfo?.characterCount ?? 0;
            }

            shared.Clear();
        }

        public void SetTextFromString(StringSlice inString, object inContext = null) {
            m_LastId = StringHash32.Null;

            if (inString.IsEmpty) {
                m_LastMetrics = default;
                InternalSetText(string.Empty);
                return;
            }

            TagString shared = SharedTagString();

            Services.Script.ParseToTag(ref shared, inString, inContext);
            InternalSetText(shared.RichText);
            m_LastMetrics.VisibleCharCount = shared.VisibleText.Length;
            m_LastMetrics.RichCharCount = shared.RichText.Length;

            shared.Clear();
        }

        public void SetTextNoParse(StringSlice inString) {
            m_LastId = StringHash32.Null;

            if (inString.IsEmpty) {
                m_LastId = StringHash32.Null;
                m_LastMetrics = default;
                InternalSetText(string.Empty);
                return;
            }

            var shared = SharedStringBuilder();

            shared.AppendSlice(inString);
            InternalSetText(shared);
            m_LastMetrics.VisibleCharCount = shared.Length;
            m_LastMetrics.RichCharCount = shared.Length;

            shared.Clear();
        }

        public void SetTextNoParse(StringBuilder inString) {
            m_LastId = StringHash32.Null;

            if (inString.Length == 0) {
                m_LastMetrics = default;
                InternalSetText(string.Empty);
                return;
            }

            var shared = SharedStringBuilder();

            for(int i = 0; i < inString.Length; i++) {
                shared.Append(inString[i]);
            }
            
            InternalSetText(shared);
            m_LastMetrics.VisibleCharCount = shared.Length;
            m_LastMetrics.RichCharCount = shared.Length;

            shared.Clear();
        }

        internal void InternalSetText(string inText) {
            m_Text.SetText(AssemblePrePostString(inText, m_Prefix, m_Postfix));
            m_Initialized = true;
        }

        internal void InternalSetText(StringBuilder inText) {
            AssemblePrePostString(inText, m_Prefix, m_Postfix);
            m_Text.SetText(inText);
            m_Initialized = true;
        }

        internal void OnLocalizationRefresh() {
            if (!m_Initialized) {
                if (m_LastMetrics.RichCharCount == 0 && !m_DefaultText.IsEmpty) {
                    SetText(m_DefaultText, null);
                }

                m_Initialized = true;
            }
            else {
                if (!m_LastId.IsEmpty) {
                    SetText(m_LastId, null);
                }
            }
        }

        #endregion // Text

        #region Unity Events

        private void OnEnable() {
            m_Text.tintAllSprites = m_TintSprites;
            Services.Loc.RegisterText(this);

            if (!m_Initialized && !Services.Loc.IsLoading()) {
                OnLocalizationRefresh();
            }
        }

        private void OnDisable() {
            Services.Loc?.DeregisterText(this);
        }

#if UNITY_EDITOR

        private void Reset() {
            this.CacheComponent(ref m_Text);
        }

#endif // UNITY_EDITOR

        #endregion // Unity Events

        #region Utils

        // if this is getting accessed by multiple threads
        // then we already have problems, as text can't be updated outside
        // of the main thread
        static private TagString s_SharedTagString;
        static private StringBuilder s_CopyBuffer;

        static private TagString SharedTagString() {
            return s_SharedTagString ?? (s_SharedTagString = new TagString());
        }

        static private StringBuilder SharedStringBuilder() {
            var sb = s_CopyBuffer ?? (s_CopyBuffer = new StringBuilder(512));
            sb.Clear();
            return sb;
        }

        static private string ErrorString(TextId id) {
            return string.Format("<color=red>ERROR:</color> {0}", id.ToDebugString());
        }

        static private unsafe string AssemblePrePostString(string inText, string inPrefix, string inPostfix) {
            if (string.IsNullOrEmpty(inText)) {
                return string.Empty;
            }

            return UnsafeExt.PrePostString(inText, inPrefix, inPostfix);
        }

        static private unsafe void AssemblePrePostString(StringBuilder inText, string inPrefix, string inPostfix) {
            if (inText.Length > 0) {
                if (!string.IsNullOrEmpty(inPrefix)) {
                    inText.Insert(0, inPrefix);
                }
                if (!string.IsNullOrEmpty(inPostfix)) {
                    inText.Append(inPostfix);
                }
            }
        }

        #endregion // Utils
    }
}