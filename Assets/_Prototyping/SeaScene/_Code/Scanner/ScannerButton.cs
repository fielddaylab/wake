using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using System;
using System.Collections;
using BeauRoutine.Extensions;
using Aqua;

namespace ProtoAqua.Observation
{
    public class ScannerButton : BasePanel
    {
        static private readonly StringHash32 ActiveTooltip = "ui.scanner.active.tooltip";
        static private readonly StringHash32 InactiveTooltip = "ui.scanner.inactive.tooltip";

        #region Inspector
        
        [SerializeField, Required] private Toggle m_Toggle = null;
        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private CursorInteractionHint m_InputHint;

        [Header("Assets")]
        [SerializeField] private Sprite m_ScanIcon = null;
        [SerializeField] private Sprite m_MoveIcon = null;

        #endregion // Inspector

        protected override void Awake()
        {
            Services.Events
                .Register(ObservationEvents.ScannerOn, OnScannerOn, this)
                .Register(ObservationEvents.ScannerOff, OnScannerOff, this);
            
            m_Toggle.onValueChanged.AddListener(OnToggle);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void OnToggle(bool inState)
        {
            Services.Events.Dispatch(ObservationEvents.ScannerSetState, inState);
        }

        private void OnScannerOn()
        {
            m_Toggle.SetIsOnWithoutNotify(true);
            m_Icon.sprite = m_MoveIcon;
            m_InputHint.TooltipId = ActiveTooltip;
        }

        private void OnScannerOff()
        {
            m_Toggle.SetIsOnWithoutNotify(false);
            m_Icon.sprite = m_ScanIcon;
            m_InputHint.TooltipId = InactiveTooltip;
        }
    }
}