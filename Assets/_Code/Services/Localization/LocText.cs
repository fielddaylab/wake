using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Tags;
using TMPro;
using UnityEngine;

namespace Aqua
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocText : MonoBehaviour
    {
        #region Inspector

        [SerializeField, HideInEditor] private TMP_Text m_Text = null;
        [SerializeField] private TextId m_DefaultText = default(TextId);

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_LastId;
        [NonSerialized] private string m_CurrentText;
        private TagString m_TagString;
        [NonSerialized] private bool m_Initialized;

        #region Text

        public TMP_Text Graphic { get { return m_Text; } }
        public TagString CurrentText { get { return m_TagString ?? (m_TagString = new TagString()); } }

        public void SetText(StringHash32 inId, object inContext = null)
        {
            m_LastId = inId;

            if (inId.IsEmpty)
            {
                m_TagString?.Clear();
                InternalSetText(string.Empty);
                return;
            }

            if (Services.Loc.LocalizeTagged(ref m_TagString, inId, inContext))
            {
                InternalSetText(m_TagString.RichText);
            }
            else
            {
                InternalSetText(string.Format("'{0}'", inId.ToDebugString()));
            }
        }

        public void SetText(StringSlice inString, object inContext = null)
        {
            if (inString.IsEmpty)
            {
                m_TagString?.Clear();
                m_LastId = StringHash32.Null;
                InternalSetText(string.Empty);
                return;
            }

            if (inString.StartsWith('\''))
            {
                m_LastId = inString.Substring(1).Hash32();

                Services.Loc.LocalizeTagged(ref m_TagString, m_LastId, inContext);
                InternalSetText(m_TagString.RichText);
                return;
            }
            else
            {
                m_LastId = StringHash32.Null;
                Services.Script.ParseToTag(ref m_TagString, inString, inContext);
                InternalSetText(m_TagString.RichText);
            }
        }

        private void InternalSetText(string inText)
        {
            m_Text.SetText(inText);
            m_CurrentText = inText;
            m_Initialized = true;
        }

        internal void OnLocalizationRefresh()
        {
            if (string.IsNullOrEmpty(m_CurrentText) && !m_DefaultText.IsEmpty)
                SetText(m_DefaultText, null);
            
            m_Initialized = true;
        }

        #endregion // Text

        #region Unity Events

        private void OnEnable()
        {
            Services.Loc.RegisterText(this);

            if (!m_Initialized && !Services.Loc.IsLoading())
            {
                OnLocalizationRefresh();
            }
        }

        private void OnDisable()
        {
            Services.Loc?.DeregisterText(this);
        }

        #if UNITY_EDITOR

        private void Reset()
        {
            this.CacheComponent(ref m_Text);
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events
    }
}