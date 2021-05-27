using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using Aqua;
using System.Collections.Generic;
using BeauUtil.Debugger;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenActors : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private RectTransform m_ButtonRoot = null;
        [SerializeField] private Button m_NextButton = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private ScrollRect m_OptionsScroll = null;
        [SerializeField] private RectTransform m_HasOptions = null;
        [SerializeField] private RectTransform m_NoOptions = null;

        #endregion // Inspector

        [NonSerialized] private ActorToggleButton[] m_CachedButtons;
        [NonSerialized] private int m_ButtonCount;
        [NonSerialized] private ExperimentSettings.TankDefinition m_CachedTank;

        // private bool OnMeasurementCritterY;

        public Action OnSelectContinue;

        protected override void Awake()
        {
            m_CachedButtons = m_ButtonRoot.GetComponentsInChildren<ActorToggleButton>();
            for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                ActorToggleButton button = m_CachedButtons[i];
                button.Toggle.onValueChanged.AddListener((b) => UpdateFromButton(button.Id.AsStringHash(), b));
            }

            m_NextButton.onClick.AddListener(() => OnSelectContinue?.Invoke());
        }

        protected override void RestoreState()
        {
            base.RestoreState();

            m_CachedTank = Config.GetTank(Setup.Tank);

            int buttonIdx = 0;
            foreach(var actorType in Services.Data.Profile.Bestiary.GetEntities(BestiaryDescCategory.Critter))
            {
                Assert.True(buttonIdx < m_CachedButtons.Length);

                if (actorType.Size() > m_CachedTank.MaxSize || actorType.HasFlags(BestiaryDescFlags.DoNotUseInExperimentation))
                    continue;
                
                var button = m_CachedButtons[buttonIdx];
                button.gameObject.SetActive(true);
                button.Load(actorType.Id(), actorType.Icon(), actorType.CommonName(), true);
                button.Toggle.SetIsOnWithoutNotify(Setup.ActorIds.Contains(actorType.Id()));
                button.Toggle.group = m_CachedTank.SingleCritter ? m_ToggleGroup : null;
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

            if (m_CachedTank.SingleCritter)
            {
                UpdateDisplay(Setup.CritterId);
            }
            else
            {
                UpdateDisplay(StringHash32.Null);
            }
        }

        private void UpdateFromButton(StringHash32 inActorId, bool inbActive)
        {
            var actorData = Services.Assets.Bestiary.Get(inActorId);
            GeneralUpdate(inActorId, inbActive);
            UpdateDisplay(inActorId);
            Services.Data.SetVariable("experiment:setup." + actorData.name, inbActive);
        }

        private void GeneralUpdate(StringHash32 inActorId, bool inbActive)
        {
            if (inbActive)
            {
                if (m_CachedTank.SingleCritter)
                    Setup.CritterId = inActorId;
                Setup.ActorIds.Add(inActorId);
                Services.Events.Dispatch(ExperimentEvents.SetupAddActor, inActorId);
            }
            else
            {
                if (m_CachedTank.SingleCritter)
                    Ref.CompareExchange(ref Setup.CritterId, inActorId, StringHash32.Null);

                Setup.ActorIds.Remove(inActorId);
                Services.Events.Dispatch(ExperimentEvents.SetupRemoveActor, inActorId);
            }
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

            UpdateButtonInteractable();
            m_NextButton.interactable = Setup.ActorIds.Count > 0 && Setup.ActorIds.Count <= ExperimentConsts.MaxCritters;

            Services.Data.SetVariable(ExperimentVars.SetupPanelLastActorType, inActorId);
        }
    
        private void UpdateButtonInteractable()
        {
            if (m_CachedTank.SingleCritter)
                return;

            bool bPreventMore = Setup.ActorIds.Count >= ExperimentConsts.MaxCritters;

            for(int i = 0; i < m_ButtonCount; i++)
            {
                var button = m_CachedButtons[i];
                button.Toggle.interactable = !bPreventMore ? true : button.Toggle.isOn;
            }
        }
    }
}