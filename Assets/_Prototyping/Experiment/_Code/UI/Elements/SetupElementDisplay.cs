using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class SetupElementDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private CursorInteractionHint m_CursorHint = null;
        [SerializeField] private LocText m_Label = null;

        #endregion // Inspector

        public void Load(Sprite inIcon, StringHash32 inLabel)
        {
            m_Icon.sprite = inIcon;
            m_CursorHint.TooltipId = inLabel;
            if (m_Label)
                m_Label.SetText(inLabel);
        }
    }
}