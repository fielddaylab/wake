using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using Aqua;
using BeauUtil.Debugger;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenEco : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private Button m_NextButton = null;
        [SerializeField] private Button m_BackButton = null;
        [SerializeField] private Button m_ConstructButton = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private ScrollRect m_OptionsScroll = null;
        [SerializeField] private RectTransform m_HasOptions = null;
        [SerializeField] private RectTransform m_NoOptions = null;

        #endregion // Inspector

        [NonSerialized] private SetupToggleButton[] m_CachedButtons;
        [NonSerialized] private int m_ButtonCount;

        public Action OnSelectContinue;
        public Action OnSelectBack;

        public Action OnSelectConstruct;

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
            m_ConstructButton.onClick.AddListener(() => OnSelectConstruct?.Invoke());
        }

        protected override void RestoreState()
        {
            base.RestoreState();

            int buttonIdx = 0;
            foreach(var environmentType in Services.Data.Profile.Bestiary.GetEntities(BestiaryDescCategory.Environment))
            {
                Assert.True(buttonIdx < m_CachedButtons.Length);

                if (environmentType.HasFlags(BestiaryDescFlags.DoNotUseInExperimentation))
                    continue;
                
                var button = m_CachedButtons[buttonIdx];
                button.gameObject.SetActive(true);
                button.Load(environmentType.Id(), environmentType.Icon(), environmentType.CommonName(), true);
                button.Toggle.SetIsOnWithoutNotify(environmentType.Id() == Setup.EnvironmentId);
                buttonIdx++;
            }

            m_ButtonCount = buttonIdx;

            m_HasOptions.gameObject.SetActive(m_ButtonCount > 0);
            m_NoOptions.gameObject.SetActive(m_ButtonCount == 0);

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].gameObject.SetActive(false);
            }

            m_OptionsScroll.ForceRebuild();

            switch(Setup.Tank)
            {
                case TankType.Foundational:
                    {
                        m_NextButton.gameObject.SetActive(false);
                        m_ConstructButton.gameObject.SetActive(true);
                        break;
                    }

                default:
                    {
                        m_NextButton.gameObject.SetActive(true);
                        m_ConstructButton.gameObject.SetActive(false);
                        break;
                    }
            }

            UpdateDisplay(Setup.EnvironmentId);
        }

        private void UpdateDisplay(StringHash32 inWaterId)
        {
            if (inWaterId.IsEmpty)
            {
                m_NextButton.interactable = false;
                m_ConstructButton.interactable = false;
                m_Label.SetText(StringHash32.Null);
            }
            else
            {
                var def = Services.Assets.Bestiary.Get(inWaterId);
                m_Label.SetText(def.CommonName());
                m_NextButton.interactable = true;
                m_ConstructButton.interactable = true;
            }
        }

        private void UpdateFromSelection()
        {
            Toggle active = m_ToggleGroup.ActiveToggle();
            if (active != null)
            {
                Setup.EnvironmentId = active.GetComponent<SetupToggleButton>().Id.AsStringHash();
                var def = Services.Assets.Bestiary.Get(Setup.EnvironmentId);
                Setup.EnvironmentProperties = BestiaryUtils.GenerateInitialState(def);

                Services.Data.SetVariable(ExperimentVars.SetupPanelEcoType, Setup.EnvironmentId);
            }
            else
            {
                Setup.EnvironmentId = StringHash32.Null;
                Setup.EnvironmentProperties = default(WaterPropertyBlockF32);

                Services.Data.SetVariable(ExperimentVars.SetupPanelEcoType, StringHash32.Null);
            }

            UpdateDisplay(Setup.EnvironmentId);
        }
    }
}