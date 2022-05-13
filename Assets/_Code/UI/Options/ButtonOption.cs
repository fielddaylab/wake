using UnityEngine;
using System;
using UnityEngine.UI;

namespace Aqua.Option
{
    public class ButtonOption : MonoBehaviour 
    {
        #region Inspector

        [SerializeField] private LocText m_Label = null;
        
        [Header("Mute`")]
        [SerializeField] private Button m_Button = null;
        [SerializeField] private CursorInteractionHint m_Hint = null;
        
        #endregion // Inspector

        public Action OnClicked;

        private void Awake()
        {
            m_Button.onClick.AddListener(() => OnClicked?.Invoke());
        }

        public void Initialize(TextId inLabel, TextId inDescription, Action inOnClick)
        {
            m_Label.SetText(inLabel);
            m_Hint.TooltipId = inDescription;
            OnClicked = inOnClick;
        }
    }
}