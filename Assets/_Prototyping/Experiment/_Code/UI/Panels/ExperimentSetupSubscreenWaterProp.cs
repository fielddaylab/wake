using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Aqua;
using BeauUtil.Debugger;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenWaterProp : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private Button m_NextButton = null;
        [SerializeField] private Button m_BackButton = null;
        [SerializeField] private LocText m_Label = null;

        #endregion // Inspector

        [NonSerialized] private SetupToggleButton[] m_CachedButtons;
        [NonSerialized] private int m_ButtonCount;

        public Action OnSelectContinue;
        public Action OnSelectBack;

        protected override void Awake()
        {
            m_CachedButtons = m_ToggleGroup.GetComponentsInChildren<SetupToggleButton>();

            for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                m_CachedButtons[i].Toggle.group = m_ToggleGroup;
                m_CachedButtons[i].Toggle.onValueChanged.AddListener((b) => UpdateFromSelection());
            }

            m_NextButton.onClick.AddListener(() => OnSelectContinue?.Invoke());
            m_BackButton.onClick.AddListener(() => OnSelectBack?.Invoke());
        }

        protected override void RestoreState()
        {
            base.RestoreState();

            int buttonIdx = 0;
            foreach(var property in Services.Assets.WaterProp.Measurable())
            {
                Assert.True(buttonIdx < m_CachedButtons.Length);

                if (!Services.Data.Profile.Inventory.IsPropertyUnlocked(property.Index()))
                    continue;

                var button = m_CachedButtons[buttonIdx];
                button.gameObject.SetActive(true);
                button.Load((int) property.Index(), property.Icon(), property.LabelId(), true);
                button.Toggle.SetIsOnWithoutNotify(Setup.PropertyId == property.Index());

                buttonIdx++;
            }

            m_ButtonCount = buttonIdx;

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].gameObject.SetActive(false);
            }

            UpdateDisplay(WaterPropertyId.MAX);
        }

        private void UpdateDisplay(WaterPropertyId inWaterId)
        {
            if (inWaterId == WaterPropertyId.MAX)
            {
                m_Label.SetText(string.Empty);
                m_NextButton.interactable = false;
            }
            else
            {
                var def = Services.Assets.WaterProp.Property(inWaterId);
                m_Label.SetText(def.LabelId());
                m_NextButton.interactable = true;
            }
        }
    
        private void UpdateFromSelection()
        {
            Toggle active = m_ToggleGroup.ActiveToggle();
            if (active != null)
            {
                Setup.PropertyId = (WaterPropertyId) active.GetComponent<SetupToggleButton>().Id.AsInt();
                Services.Events.Dispatch(ExperimentEvents.StressorColor, Setup.PropertyId);
                Services.Events.Dispatch(ExperimentEvents.SetupAddWaterProperty, Setup.PropertyId);
            }
            else
            {
                Setup.PropertyId = WaterPropertyId.MAX;
                Services.Events.Dispatch(ExperimentEvents.SetupRemoveWaterProperty);
            }

            UpdateDisplay(Setup.PropertyId);
        }
    }
}