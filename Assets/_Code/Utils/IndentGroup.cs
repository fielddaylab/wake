using UnityEngine;

namespace Aqua
{
    [RequireComponent(typeof(RectTransform))]
    public class IndentGroup : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private RectTransform m_RectTransform;
        [SerializeField] private float m_IndentSize = 16f;

        #endregion // Inspector

        public void SetIndent(int inIndentLevel)
        {
            Vector2 offset = m_RectTransform.offsetMin;
            offset.x = inIndentLevel * m_IndentSize;
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