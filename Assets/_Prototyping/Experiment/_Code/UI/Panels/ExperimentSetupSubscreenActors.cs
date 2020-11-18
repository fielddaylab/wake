using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using BeauUtil.Variants;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenActors : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private RectTransform m_ButtonRoot = null;
        [SerializeField] private Button m_NextButton = null;
        [SerializeField] private TMP_Text m_Label = null;

        #endregion // Inspector

        [NonSerialized] private ExperimentSettings m_CachedSettings;
        [NonSerialized] private ActorToggleButton[] m_CachedButtons;

        [NonSerialized] private ExperimentSetupData m_CachedData;

        public Action OnSelectContinue;

        protected override void Awake()
        {
            m_CachedSettings = Services.Tweaks.Get<ExperimentSettings>();
            m_CachedButtons = m_ButtonRoot.GetComponentsInChildren<ActorToggleButton>();
            for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                ActorToggleButton button = m_CachedButtons[i];
                button.Toggle.onValueChanged.AddListener((b) => UpdateFromButton(button.Id.AsStringHash(), b));
            }

            m_NextButton.onClick.AddListener(() => OnSelectContinue?.Invoke());

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
            var allActorTypes = m_CachedSettings.AllNonEmptyActors();
            var noneActorType = m_CachedSettings.GetActor(StringHash32.Null);

            int buttonIdx = 0;
            foreach(var actorType in allActorTypes)
            {
                if (buttonIdx >= m_CachedButtons.Length)
                    break;

                var button = m_CachedButtons[buttonIdx];
                
                if (Services.Data.CheckConditions(actorType.Condition))
                {
                    button.Load(actorType.Id, actorType.Icon, true);
                }
                else
                {
                    button.Load(noneActorType.Id, noneActorType.Icon, false);
                }

                ++buttonIdx;
            }

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].Load(noneActorType.Id, noneActorType.Icon, false);
            }

            m_NextButton.interactable = false;
        }

        private void UpdateFromButton(StringHash32 inActorId, bool inbActive)
        {
            var actorData = m_CachedSettings.GetActor(inActorId);

            if (inbActive)
            {
                m_CachedData.ActorIds.Add(inActorId);
                Services.Events.Dispatch(ExperimentEvents.SetupAddActor, inActorId);
            }
            else
            {
                m_CachedData.ActorIds.Remove(inActorId);
                Services.Events.Dispatch(ExperimentEvents.SetupRemoveActor, inActorId);
            }

            m_NextButton.interactable = m_CachedData.ActorIds.Count > 0;

            UpdateDisplay(inActorId);
            Services.Data.SetVariable("experiment:setup." + actorData.Id, inbActive);
        }

        private void UpdateDisplay(StringHash32 inActorId)
        {
            var def = m_CachedSettings.GetActor(inActorId);
            m_Label.text = Services.Loc.Localize(def.LabelId);

            Services.Data.SetVariable(ExperimentVars.SetupPanelLastActorType, inActorId);
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            UpdateDisplay(StringHash32.Null);
        }
    }
}