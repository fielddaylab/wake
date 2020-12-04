using BeauUtil;
using TMPro;
using UnityEngine;

namespace Aqua
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocText : MonoBehaviour
    {
        #region Inspector

        [SerializeField, HideInEditor] private TMP_Text m_Text = null;
        [SerializeField] private string m_DefaultText = null;

        #endregion // Inspector

        private string m_CurrentText;

        #region Text

        public TMP_Text Graphic { get { return m_Text; } }

        public void SetText(StringHash32 inId, object inContext = null)
        {
            if (inId.IsEmpty)
            {
                InternalSetText(string.Empty);
                return;
            }

            string localized = Services.Loc.Localize(inId, "Text Not Found", inContext, false);
            InternalSetText(localized);
        }

        public void SetText(StringSlice inString, object inContext = null)
        {
            if (inString.IsEmpty)
            {
                InternalSetText(string.Empty);
                return;
            }

            if (inString.StartsWith('\''))
            {
                string localized = Services.Loc.Localize(inString.Substring(1).Hash32(), inString.ToString(), inContext);
                InternalSetText(localized);
                return;
            }
            else
            {
                InternalSetText(inString.ToString());
            }
        }

        private void InternalSetText(string inText)
        {
            m_Text.SetText(inText);
            m_CurrentText = inText;
        }

        #endregion // Text

        #region Unity Events

        private void Start()
        {
            if (string.IsNullOrEmpty(m_CurrentText) && !string.IsNullOrEmpty(m_DefaultText))
                SetText(m_DefaultText, null);
        }

        private void OnEnable()
        {
            // TODO: Hook into refresh?
        }

        private void OnDisable()
        {
            // TODO: Unhook from refresh?
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