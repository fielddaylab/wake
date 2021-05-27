using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using Aqua;
using BeauUtil.Debugger;

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

        [NonSerialized] private SetupToggleButton[] m_CachedButtons;
        [NonSerialized] private int m_ButtonCount;
        [NonSerialized] private TankType m_CurrentTank;

        public Action OnChange;
        public Action OnSelectContinue;
        public Action OnSelectConstruct;

        protected override void Awake()
        {
            base.Awake();

            m_CachedButtons = m_ToggleGroup.GetComponentsInChildren<SetupToggleButton>();
            for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                m_CachedButtons[i].Toggle.group = m_ToggleGroup;
                m_CachedButtons[i].Toggle.onValueChanged.AddListener((b) => UpdateFromSelection());
            }

            m_NextButton.onClick.AddListener(() => OnSelectContinue?.Invoke());
            m_ConstructButton.onClick.AddListener(() => OnSelectConstruct?.Invoke());
        }

        protected override void RestoreState()
        {
            base.RestoreState();

            int buttonIdx = 0;
            foreach(var tankType in Config.TankTypes())
            {
                Assert.True(buttonIdx < m_CachedButtons.Length);
                if (!tankType.TankOn || !Services.Data.CheckConditions(tankType.Condition))
                    continue;
                
                var button = m_CachedButtons[buttonIdx];
                button.gameObject.SetActive(true);
                button.Load((int) tankType.Tank, tankType.Icon, tankType.ShortLabelId, true);
                button.Toggle.SetIsOnWithoutNotify(tankType.Tank == Setup.Tank);
                buttonIdx++;
            }

            m_ButtonCount = buttonIdx;

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].gameObject.SetActive(false);
            }

            UpdateDisplay(Setup.Tank);
        }

        private void UpdateDisplay(TankType inTankType)
        {
            OnChange?.Invoke();

            var def = Config.GetTank(inTankType);
            m_Label.SetText(def?.LabelId ?? StringHash32.Null);

            switch(m_CurrentTank)
            {
                case TankType.Stressor:
                case TankType.Measurement:
                    {
                        m_NextButton.gameObject.SetActive(false);
                        m_ConstructButton.gameObject.SetActive(true);
                        break;
                    }

                case TankType.None:
                    {
                        m_NextButton.gameObject.SetActive(false);
                        m_ConstructButton.gameObject.SetActive(false);
                        break;
                    }

                default:
                    {
                        m_NextButton.gameObject.SetActive(true);
                        m_ConstructButton.gameObject.SetActive(false);
                        break;
                    }
            }
        }

        private void UpdateFromSelection()
        {
            Toggle active = m_ToggleGroup.ActiveToggle();
            if (active != null)
            {
                m_CurrentTank = (TankType) active.GetComponent<SetupToggleButton>().Id.AsInt();
                Setup.Tank = m_CurrentTank;
            }
            else
            {
                m_CurrentTank = TankType.None;
                Setup.Tank = m_CurrentTank;

                Services.Data.SetVariable(ExperimentVars.SetupPanelTankType, "None");
            }

            UpdateDisplay(Setup.Tank);
        }
    }
}