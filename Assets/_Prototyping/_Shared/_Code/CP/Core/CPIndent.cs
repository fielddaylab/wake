using UnityEngine;

namespace ProtoCP
{
    [RequireComponent(typeof(RectTransform))]
    public class CPIndent : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private RectTransform m_RectTransform;

        #endregion // Inspector

        public void SetIndent(float inIndentLevel)
        {
            Vector2 offset = m_RectTransform.offsetMin;
            offset.x = inIndentLevel;
            m_RectTransform.offsetMin = offset;
        }

        #region Unity Events

        #if UNITY_EDITOR

        private void Reset()
        {
            m_RectTransform = GetComponent<RectTransform>();
        }

        private void OnValidate()
        {
            m_RectTransform = GetComponent<RectTransform>();
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events
    }
}