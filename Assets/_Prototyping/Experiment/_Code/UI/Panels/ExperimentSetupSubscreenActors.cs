using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using BeauUtil.Variants;
using Aqua;
using System.Collections.Generic;
using System.Linq;

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
        [NonSerialized] private bool m_Visited = false;

        [NonSerialized] private ExperimentSetupData m_CachedData;

        public Action OnSelectContinue;

        protected override void Awake()
        {
            Services.Events.Register<ExpSubscreen>(ExperimentEvents.SubscreenBack, PresetButtons, this)
            .Register<TankType>(ExperimentEvents.SetupTank, ConfigureMeasurement, this)
            .Register(ExperimentEvents.MeasurementCritX, SetupMeasurementY, this);


            m_CachedButtons = m_ButtonRoot.GetComponentsInChildren<ActorToggleButton>();
            for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                ActorToggleButton button = m_CachedButtons[i];
                button.Toggle.onValueChanged.AddListener((b) => UpdateFromButton(button.Id.AsStringHash(), b));
            }


            m_NextButton.onClick.AddListener(() => OnSelectContinue?.Invoke());

            UpdateButtons();

        }

        public void SetupMeasurementY() {
            


        }

        public void ConfigureMeasurement(TankType tank) {
            if(tank == TankType.Measurement) {
                for (int i = 0; i < m_CachedButtons.Length; ++i) {
                ActorToggleButton button = m_CachedButtons[i];
                    button.Toggle.onValueChanged.RemoveAllListeners();
                    // button.Toggle.onValueChanged.AddListener((b) => UpdateFromSelection());
                }
            }
            else {
                for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                ActorToggleButton button = m_CachedButtons[i];
                button.Toggle.onValueChanged.RemoveAllListeners();
                button.Toggle.onValueChanged.AddListener((b) => UpdateFromButton(button.Id.AsStringHash(), b));
            }
            }
            
        }
        public override void SetData(ExperimentSetupData inData)
        {
            base.SetData(inData);
            m_CachedData = inData;
        }

        public override void Refresh()
        {
            base.Refresh();
            m_Visited = false;
            UpdateButtons();
        }
        private void PresetButtons(ExpSubscreen sc) {

            if(!sc.Equals(ExpSubscreen.Actor)) return;
            if(m_CachedData == null) {
                throw new NullReferenceException("No cached data in actor.");
            }
            var hasOn = false;
            if(m_CachedData.ActorIds.Count > 0) {
                foreach(var actor in m_CachedData.ActorIds) {
                    foreach(var button in m_CachedButtons) {
                        if(button.Id.AsStringHash().Equals(actor)) {
                            button.Toggle.SetIsOnWithoutNotify(true);
                            hasOn = true;
                            break;
                        }
                    }
                }
            }
            

            m_NextButton.interactable = hasOn;

        }

        private void UpdateButtons()
        {
            var tankType = Services.Tweaks.Get<ExperimentSettings>().GetTank(m_CachedData.Tank);
            if (tankType == null)
                return;
            
            var allActorTypes = Services.Data.Profile.Bestiary.GetEntities(BestiaryDescCategory.Critter);
            
            // if(tankType.Tank.Equals(TankType.Measurement)) {
            //     if(!m_CachedData.CritterX.Equals(StringHash32.Null)) {
            //         allActorTypes = MeasurementFilterY(allActorTypes);
            //     }
            // }

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
                button.Toggle.group = tankType.SingleCritter ? m_ToggleGroup : null;

                ++buttonIdx;
            }

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].Load(StringHash32.Null, m_EmptyIcon, false);
            }

            m_NextButton.interactable = false;
        }

        private List<BestiaryDesc> MeasurementFilterY(IEnumerable<BestiaryDesc> All_Actors) {
            List<BestiaryDesc> allActors = All_Actors.ToList();
            List<BestiaryDesc> targets = new List<BestiaryDesc>();
            foreach(var fact in m_CachedData.GetEat()) {
                BFEat efact = (BFEat)fact.Fact;
                // targets.Add(efact.GetTarget());
            }
            allActors.RemoveAll(b => allActors.Except(targets).Contains(b));
            return allActors;

        }

        private void UpdateFromButton(StringHash32 inActorId, bool inbActive)
        {
            var actorData = Services.Assets.Bestiary.Get(inActorId);
            Toggle active = m_ToggleGroup.ActiveToggle();

            if(m_CachedData.Tank == TankType.Measurement) {
                if(m_CachedData.CritterY.Equals(StringHash32.Null)) {
                    if(active != null) {

                    }
                }
            }


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