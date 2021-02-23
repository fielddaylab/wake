using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using Aqua;
using System.Collections.Generic;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenEco : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private Button m_NextButton = null;
        [SerializeField] private Button m_BackButton = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private Sprite m_EmptyIcon = null;

        [SerializeField] private Button m_ConstructButton = null;

        #endregion // Inspector

        [NonSerialized] private SetupToggleButton[] m_CachedButtons;
        [NonSerialized] private ExperimentSetupData m_CachedData;

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

        public List<Action> GetAction() {
            var res = new List<Action>();
            res.Add(OnSelectBack);
            res.Add(OnSelectContinue);

            return res;
        }

        private void UpdateButtons()
        {
            var allWaterTypes = Services.Data.Profile.Bestiary.GetEntities(BestiaryDescCategory.Environment);

            int buttonIdx = 0;
            foreach(var waterType in allWaterTypes)
            {
                if (buttonIdx >= m_CachedButtons.Length)
                    break;

                var button = m_CachedButtons[buttonIdx];
                button.Load(waterType.Id(), waterType.Icon(), true);

                ++buttonIdx;
            }

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].Load(StringHash32.Null, m_EmptyIcon, false);
            }
        }

        private void UpdateDisplay(StringHash32 inWaterId)
        {
            if (inWaterId.IsEmpty)
            {
                m_NextButton.interactable = false;
                m_ConstructButton.interactable = false;
                m_ConstructButton.gameObject.SetActive(false);
                m_Label.SetText(StringHash32.Null);
            }
            else
            {
                var def = Services.Assets.Bestiary.Get(inWaterId);
                m_Label.SetText(def.CommonName());
                

                if (m_CachedData.Tank == TankType.Foundational) {
                    m_NextButton.gameObject.SetActive(false);
                    m_ConstructButton.gameObject.SetActive(true);
                    m_ConstructButton.interactable = true;
                }
                else {
                    m_NextButton.gameObject.SetActive(true);
                    m_ConstructButton.gameObject.SetActive(false);
                    m_NextButton.interactable = true;
                }
            }

            Services.Data.SetVariable(ExperimentVars.SetupPanelEcoType, inWaterId);
        }

        // private void SetTransforms() {
        //     Transform toggleGroup = m_ToggleGroup.transform;
        //     Transform text = m_Label.transform;

        //     toggleGroup.position = new Vector3(toggleGroup.position.x, 93f, toggleGroup.position.z);
        //     text.position = new Vector3(text.position.x, 45f, text.position.z);
        // }
    
        private void UpdateFromSelection()
        {
            Toggle active = m_ToggleGroup.ActiveToggle();
            if (active != null)
            {
                m_CachedData.EcosystemId = active.GetComponent<SetupToggleButton>().Id.AsStringHash();
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