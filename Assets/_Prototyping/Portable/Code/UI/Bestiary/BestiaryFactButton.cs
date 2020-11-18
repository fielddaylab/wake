using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;

namespace ProtoAqua.Portable
{
    public class BestiaryFactButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Button m_Button = null;
        [SerializeField, Required] private RectTransform m_ButtonTail = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private TMP_Text m_Label = null;

        #endregion // Inspector

        public void Initialize(BestiaryFactBase inFact, bool inbButtonMode)
        {
            m_Icon.sprite = inFact.Icon;
            m_Icon.gameObject.SetActive(inFact.Icon);
            m_Label.SetText(inFact.Label);

            m_Button.targetGraphic.raycastTarget = inbButtonMode;
            m_ButtonTail.gameObject.SetActive(inbButtonMode);
        }
    }
}