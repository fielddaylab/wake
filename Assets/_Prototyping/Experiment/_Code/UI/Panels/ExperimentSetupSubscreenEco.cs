using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenEco : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private Button m_NextButton = null;
        [SerializeField] private Button m_BackButton = null;
        [SerializeField] private TMP_Text m_Label = null;

        #endregion // Inspector

        [NonSerialized] private ExperimentSettings m_CachedSettings;
        [NonSerialized] private EcoToggleButton[] m_CachedButtons;

        [NonSerialized] private TankSelectionData m_CachedData;

        public Action OnSelectContinue;
        public Action OnSelectBack;

        protected override void Awake()
        {
            m_CachedSettings = Services.Tweaks.Get<ExperimentSettings>();
            m_CachedButtons = m_ToggleGroup.GetComponentsInChildren<EcoToggleButton>();
            for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                m_CachedButtons[i].Toggle.group = m_ToggleGroup;
                m_CachedButtons[i].Toggle.onValueChanged.AddListener((b) => UpdateFromSelection());
            }

            m_NextButton.onClick.AddListener(() => OnSelectContinue?.Invoke());
            m_BackButton.onClick.AddListener(() => OnSelectBack?.Invoke());

            UpdateButtons();
        }

        public void SetData(TankSelectionData inData)
        {
            m_CachedData = inData;
        }

        private void UpdateButtons()
        {
            var allWaterTypes = m_CachedSettings.AllNonEmptyEcos();
            var noneWaterType = m_CachedSettings.GetEco(StringSlice.Empty);

            int buttonIdx = 0;
            foreach(var waterType in allWaterTypes)
            {
                if (buttonIdx >= m_CachedButtons.Length)
                    break;

                var button = m_CachedButtons[buttonIdx];
                button.gameObject.SetActive(true);
                
                if (Services.Data.CheckConditions(waterType.Condition))
                {
                    button.Load(waterType, true);
                }
                else
                {
                    button.Load(noneWaterType, false);
                }

                ++buttonIdx;
            }

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].Load(noneWaterType, false);
            }
        }

        private void UpdateDisplay(StringHash32 inWaterId)
        {
            var def = m_CachedSettings.GetEco(inWaterId);
            m_Label.text = Services.Loc.Localize(def.LabelId);
            m_NextButton.interactable = !inWaterId.IsEmpty;

            Services.Data.SetVariable(ExperimentVars.SetupPanelEcoType, inWaterId);
        }
    
        private void UpdateFromSelection()
        {
            Toggle active = m_ToggleGroup.ActiveToggle();
            if (active != null)
            {
                m_CachedData.EcosystemId = active.GetComponent<EcoToggleButton>().EcoId;
            }
            else
            {
                m_CachedData.EcosystemId = StringHash32.Null;
            }

            UpdateDisplay(m_CachedData.EcosystemId);
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            UpdateDisplay(m_CachedData.EcosystemId);
        }
    }
}