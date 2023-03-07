using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

namespace Aqua.Option
{
    public class ButtonOption : MonoBehaviour 
    {
        #region Inspector

        [SerializeField] private LocText m_Label = null;
        [SerializeField] private Button m_Button = null;
        [SerializeField] private CursorInteractionHint m_Hint = null;
        
        #endregion // Inspector

        public Action OnClicked;
        
        private void Awake()
        {
            m_Button.onClick.AddListener(HandleButtonClick);
        }

        public void Initialize(TextId inLabel, TextId inDescription, Action inSetter)
        {
            m_Label.SetText(inLabel);
            m_Hint.TooltipId = inDescription;
            OnClicked = inSetter;
        }

        private void HandleButtonClick() 
        {
            OnClicked?.Invoke();
            Services.Events.Queue(GameEvents.OptionsUpdated, Save.Options);
        }
    }
}