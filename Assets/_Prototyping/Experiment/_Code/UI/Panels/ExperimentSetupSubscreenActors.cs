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
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private Sprite m_EmptyIcon = null;
        [SerializeField] private ToggleGroup m_ToggleGroup = null;

        #endregion // Inspector

        [NonSerialized] private ActorToggleButton[] m_CachedButtons;
        [NonSerialized] private ExperimentSetupData m_CachedData;

        public Action OnSelectContinue;

        protected override void Awake()
        {
            m_CachedButtons = m_ButtonRoot.GetComponentsInChildren<ActorToggleButton>();
            for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                ActorToggleButton button = m_CachedButtons[i];
                button.Toggle.onValueChanged.AddListener((b) => UpdateFromButton(button.Id.AsStringHash(), b));
                button.Toggle.group = m_ToggleGroup;
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
            var tankType = Services.Tweaks.Get<ExperimentSettings>().GetTank(m_CachedData.Tank);
            var allActorTypes = Services.Data.Profile.Bestiary.GetEntities(BestiaryDescCategory.Critter);

            m_ToggleGroup.enabled = tankType.SingleCritter;

            int buttonIdx = 0;
            foreach(var actorType in allActorTypes)
            {
                if (buttonIdx >= m_CachedButtons.Length)
                    break;

                var button = m_CachedButtons[buttonIdx];

                if (actorType.Size() > tankType.MaxSize || (actorType.Flags() & BestiaryDescFlags.DoNotUseInExperimentation) != 0)
                    continue;
                
                button.Load(actorType.Id(), actorType.Icon(), true);
                ++buttonIdx;
            }

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].Load(StringHash32.Null, m_EmptyIcon, false);
            }

            m_NextButton.interactable = false;
        }

        private void UpdateFromButton(StringHash32 inActorId, bool inbActive)
        {
            var actorData = Services.Assets.Bestiary.Get(inActorId);

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
            Services.Data.SetVariable("experiment:setup." + actorData.name, inbActive);
        }

        private void UpdateDisplay(StringHash32 inActorId)
        {
            if (inActorId.IsEmpty)
            {
                m_Label.SetText(StringHash32.Null);
            }
            else
            {
                var def = Services.Assets.Bestiary.Get(inActorId);
                m_Label.SetText(def.CommonName());
            }

            Services.Data.SetVariable(ExperimentVars.SetupPanelLastActorType, inActorId);
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            UpdateDisplay(StringHash32.Null);
        }
    }
}