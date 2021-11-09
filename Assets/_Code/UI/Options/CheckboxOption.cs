using UnityEngine;
using System;
using UnityEngine.UI;

namespace Aqua.Option
{
    public class CheckboxOption : MonoBehaviour 
    {
        #region Inspector

        [SerializeField] private LocText m_Label = null;
        
        [Header("Mute`")]
        [SerializeField] private Toggle m_Checkbox = null;
        [SerializeField] private CursorInteractionHint m_Hint = null;
        
        #endregion // Inspector

        public Action<bool> OnChanged;

        private void Awake()
        {
            m_Checkbox.onValueChanged.AddListener(OnCheckboxUpdate);
        }

        public void Initialize(TextId inLabel, TextId inDescription, Action<bool> inSetter)
        {
            m_Label.SetText(inLabel);
            m_Hint.TooltipId = inDescription;
            OnChanged = inSetter;
        }

        public void Sync(bool inbValue)
        {
            m_Checkbox.SetIsOnWithoutNotify(inbValue);
            m_Checkbox.targetGraphic.color = inbValue ? AQColors.ContentBlue : AQColors.Teal;
        }

        private void OnCheckboxUpdate(bool inbSetting) 
        {
            OnChanged?.Invoke(inbSetting);

            OptionsData options = Save.Options;
            options.SetDirty();
            m_Checkbox.targetGraphic.color = inbSetting ? AQColors.ContentBlue : AQColors.Teal;
            
            Services.Events.QueueForDispatch(GameEvents.OptionsUpdated, options);
        }
    }
}