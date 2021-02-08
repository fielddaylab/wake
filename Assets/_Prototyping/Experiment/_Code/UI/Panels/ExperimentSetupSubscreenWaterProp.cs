using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenWaterProp : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private Button m_NextButton = null;
        [SerializeField] private Button m_BackButton = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private Sprite m_EmptyIcon = null;

        #endregion // Inspector

        [NonSerialized] private SetupToggleButton[] m_CachedButtons;
        [NonSerialized] private ExperimentSetupData m_CachedData;
        [NonSerialized] private ExperimentSettings m_CachedSettings;


        public Action OnSelectContinue;
        public Action OnSelectBack;

        protected override void Awake()
        {
            m_CachedSettings = Services.Tweaks.Get<ExperimentSettings>();
            m_CachedButtons = m_ToggleGroup.GetComponentsInChildren<SetupToggleButton>();
            for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                m_CachedButtons[i].Toggle.group = m_ToggleGroup;
                m_CachedButtons[i].Toggle.onValueChanged.AddListener((b) => UpdateFromSelection());
            }

            m_NextButton.onClick.AddListener(() => OnSelectContinue?.Invoke());
            m_BackButton.onClick.AddListener(() => OnSelectBack?.Invoke());

            UpdateButtons();
        }

        public override void SetData(ExperimentSetupData inData)
        {
            base.SetData(inData);
            m_CachedData = inData;
        }

        public override void Refresh()
        {
            base.Refresh();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            var allEnvs = Services.Data.Profile.Bestiary.GetEntities(BestiaryDescCategory.Environment);

            List<BFWaterProperty> EnvPropIds = new List<BFWaterProperty>();

            foreach(BestiaryDesc env in allEnvs) {
                foreach(BFBase waterFact in env.Facts) {
                    if(waterFact is BFWaterProperty) {
                        EnvPropIds.Add((BFWaterProperty)waterFact);
                    }
                }
            }

            var properties = m_CachedSettings.AllNonEmptyProperties();

            int buttonIdx = 0;

            var noneProperty = WaterPropertyId.None;

            foreach(var waterType in properties)
            {
                if (buttonIdx >= m_CachedButtons.Length)
                    break;

                var button = m_CachedButtons[buttonIdx];
                button.Load((int) waterType.Id, waterType.Icon, true);

                ++buttonIdx;
            }

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].Load((int) noneProperty, m_CachedSettings.GetProperty(noneProperty).Icon, false);
            }
        }

        private void UpdateDisplay(WaterPropertyId inWaterId)
        {
            var def = m_CachedSettings.GetProperty(inWaterId);
            m_Label.SetText(def.LabelId);
            m_NextButton.interactable = inWaterId != WaterPropertyId.None;
        }
    
        private void UpdateFromSelection()
        {
            Toggle active = m_ToggleGroup.ActiveToggle();
            if (active != null)
            {
                m_CachedData.PropertyId = (WaterPropertyId) active.GetComponent<SetupToggleButton>().Id.AsInt();
                Services.Events.Dispatch(ExperimentEvents.StressorText, m_CachedData.PropertyId);
            }
            else
            {
                m_CachedData.PropertyId = WaterPropertyId.None;
            }

            UpdateDisplay(m_CachedData.PropertyId);
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            UpdateDisplay(m_CachedData.PropertyId);
        }
    }
}