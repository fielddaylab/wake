using System;
using System.Runtime.CompilerServices;
using BeauPools;
using BeauUtil;
using BeauUtil.Tags;
using TMPro;
using UnityEngine;
using System.Text;
using BeauData;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua {
    [RequireComponent(typeof(TMP_Text)), DisallowMultipleComponent]
    public class LocFont : MonoBehaviour {

        #region Inspector

        [SerializeField, HideInEditor] private TMP_Text m_Text = null;
        [SerializeField] internal float m_DefaultSize = 16;
        [SerializeField] private float m_ESOverrideSize = 14.5f; // size if text is in Spanish

        #endregion // Inspector

        [NonSerialized] private bool m_Initialized;

        #region Text

        public void SetSize(float inSize, object inContext = null) {
            InternalSetSize(inSize);
        }


        internal void InternalSetSize(float inSize) {
            m_Text.fontSize = inSize;
            m_Initialized = true;
        }

        internal void OnLocalizationRefresh() {
            if (!m_Initialized) {
                if (Services.Loc.CurrentLanguageId.Equals(FourCC.Parse("ES"))) {
                    SetSize(m_ESOverrideSize, null);
                }
                else {
                    SetSize(m_DefaultSize, null);
                }

                m_Initialized = true;
            }
        }

        #endregion // Text

        #region Unity Events

        private void OnEnable() {
            Services.Loc.RegisterFont(this);

            if (!m_Initialized && !Services.Loc.IsLoading()) {
                OnLocalizationRefresh();
            }
        }

        private void OnDisable() {
            Services.Loc?.DeregisterFont(this);
        }

#if UNITY_EDITOR

        private void Reset() {
            this.CacheComponent(ref m_Text);
        }

#endif // UNITY_EDITOR

        #endregion // Unity Events

        
    }
}