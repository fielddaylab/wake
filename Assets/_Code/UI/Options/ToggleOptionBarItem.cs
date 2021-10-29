using UnityEngine;
using System;
using Aqua;
using UnityEngine.UI;
using AquaAudio;
using TMPro;
using BeauRoutine;
using BeauUtil;

namespace Aqua.Option
{
    public class ToggleOptionBarItem : MonoBehaviour, IKeyValuePair<object, ToggleOptionBarItem>
    {
        #region Inspector

        [SerializeField] private LocText m_Label = null;
        [SerializeField] private CursorInteractionHint m_Hint = null;
        [SerializeField] private Toggle m_Toggle = null;

        #endregion // Inspector

        public object UserData;

        public Toggle Toggle { get { return m_Toggle; } }

        private void Awake() {
            m_Toggle.onValueChanged.AddListener(OnUpdated);
        }

        public void Initialize(TextId inLabel, TextId inTooltip, object inValue)
        {
            m_Label.SetText(inLabel);
            m_Hint.TooltipId = inTooltip;
            UserData = inValue;
        }

        public void Sync(bool inbSelected) {
            m_Toggle.SetIsOnWithoutNotify(inbSelected);
            m_Toggle.targetGraphic.color = inbSelected ? AQColors.ContentBlue : AQColors.Teal;
        }

        private void OnUpdated(bool inbSelected) {
            m_Toggle.targetGraphic.color = inbSelected ? AQColors.ContentBlue : AQColors.Teal;
        }

        object IKeyValuePair<object, ToggleOptionBarItem>.Key { get { return UserData; } }
        ToggleOptionBarItem IKeyValuePair<object, ToggleOptionBarItem>.Value { get { return this; } }
    }
}