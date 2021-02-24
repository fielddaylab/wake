using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenTank : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private Button m_NextButton = null;
        [SerializeField] private LocText m_Label = null;

        [SerializeField] private Button m_ConstructButton = null;

        #endregion // Inspector

        [NonSerialized] private ExperimentSettings m_CachedSettings;
        [NonSerialized] private SetupToggleButton[] m_CachedButtons;

        [NonSerialized] private ExperimentSetupData m_CachedData;

        [NonSerialized] private TankType m_CurrentTank;

        [NonSerialized] public Action OnChange;

        [NonSerialized] public Action OnSelectContinue;

        [NonSerialized] public Action OnSelectConstruct;

        protected override void Awake()
        {
            Services.Events.Register<ExpSubscreen>(ExperimentEvents.SubscreenBack, PresetButtons, this);
            m_CachedSettings = Services.Tweaks.Get<ExperimentSettings>();
            m_CachedButtons = m_ToggleGroup.GetComponentsInChildren<SetupToggleButton>();
            for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                m_CachedButtons[i].Toggle.group = m_ToggleGroup;
                m_CachedButtons[i].Toggle.onValueChanged.AddListener((b) => UpdateFromSelection());
            }

            m_NextButton.onClick.AddListener(() => OnSelectContinue?.Invoke());

            m_ConstructButton.onClick.AddListener(() => OnSelectConstruct?.Invoke());

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

        public TankType SelectedTank()
        {
            return m_CurrentTank;
        }



        private void UpdateButtons()
        {
            var allTankTypes = m_CachedSettings.AllNonEmptyTanks();
            var noneTankType = m_CachedSettings.GetTank(TankType.None);

            int buttonIdx = 0;
            foreach(var tankType in allTankTypes)
            {
                if (buttonIdx >= m_CachedButtons.Length)
                    break;

                var button = m_CachedButtons[buttonIdx];
                
                if (Services.Data.CheckConditions(tankType.Condition))
                {
                    button.Load((int) tankType.Tank, tankType.Icon, true);
                }
                else
                {
                    button.Load((int) noneTankType.Tank, noneTankType.Icon, false);
                }

                ++buttonIdx;
            }

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].Load((int) noneTankType.Tank, noneTankType.Icon, false);
            }
        }

        private void PresetButtons(ExpSubscreen sc) {
            if(!sc.Equals(ExpSubscreen.Tank)) return;
            if(m_CachedData == null) {
                throw new NullReferenceException("No cached data in actor.");
            }
            if(m_CachedData.Tank.Equals(TankType.None)) return;
            foreach(var button in m_CachedButtons) {
                if(button.Id.Equals((int)m_CachedData.Tank)) {
                    button.Toggle.SetIsOnWithoutNotify(true);
                    break;
                }
            }
        }

        private void UpdateDisplay(TankType inTankType)
        {
            OnChange?.Invoke();
            var def = m_CachedSettings.GetTank(inTankType);
            m_Label.SetText(def.LabelId);
            m_NextButton.interactable = inTankType != TankType.None;


            if(m_CurrentTank.Equals(TankType.Stressor)) {
                // SetTransforms();
                m_NextButton.gameObject.SetActive(false);
                m_ConstructButton.gameObject.SetActive(true);
            }
            else {
                m_NextButton.gameObject.SetActive(true);
                m_ConstructButton.gameObject.SetActive(false);

            }

            Services.Data.SetVariable(ExperimentVars.SetupPanelTankType, inTankType.ToString());
        }

        // private void SetTransforms() {
        //     Transform toggleGroup = m_ToggleGroup.transform;
        //     Transform text = m_Label.transform;

        //     m_ToggleGroup.position = new Vector3(0f, 93f, toggleGroup.position.z);
        //     text.position = new Vector3(text.position.x, 45f, text.position.z);
        // }
    
        private void UpdateFromSelection()
        {
            Toggle active = m_ToggleGroup.ActiveToggle();
            if (active != null)
            {
                m_CurrentTank = (TankType)active.GetComponent<SetupToggleButton>().Id.AsInt();
                m_CachedData.Tank = m_CurrentTank; 
                
                
            }
            else
            {
                m_CurrentTank = TankType.None;
                m_CachedData.Tank = m_CurrentTank;

            }

            UpdateDisplay(m_CachedData.Tank);
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            UpdateDisplay(m_CachedData.Tank);
        }
    }
}