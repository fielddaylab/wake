using BeauRoutine;
using UnityEngine;

namespace Aqua.Modeling {
    public class DivergencePopup : MonoBehaviour {
        public InlinePopupPanel Panel = null;

        [Header("Divergence Group")]
        [SerializeField] private GameObject m_DivergenceGroup = null;
        [SerializeField] private RectTransform[] m_SignFlip = null;
        [SerializeField] private TextId m_DivergenceHeader = default;
        
        [Header("Less Population")]
        [SerializeField] private ActiveGroup m_LessGroup = new ActiveGroup();
        [SerializeField] private TextId m_LessText = default;
        
        [Header("More Population")]
        [SerializeField] private ActiveGroup m_MoreGroup = new ActiveGroup();
        [SerializeField] private TextId m_MoreText = default;

        private void Awake() {
            m_LessGroup.ForceActive(false);
            m_MoreGroup.ForceActive(false);
        }

        public void DisplayDivergence(int sign) {
            m_DivergenceGroup.gameObject.SetActive(true);
            foreach(var transform in m_SignFlip) {
                transform.SetScale(sign, Axis.Y);
            }

            if (sign > 0) {
                m_LessGroup.Deactivate();
                m_MoreGroup.Activate();
            } else {
                m_MoreGroup.Deactivate();
                m_LessGroup.Activate();
            }

            PopupContent content = default;
            content.Header = Loc.Find(m_DivergenceHeader);
            content.Text = Loc.Find(sign < 0 ? m_LessText : m_MoreText);
            Panel.Present(content, PopupFlags.ShowCloseButton);
        }
    }
}