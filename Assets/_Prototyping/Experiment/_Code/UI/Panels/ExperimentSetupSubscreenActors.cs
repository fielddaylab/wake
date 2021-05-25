using System;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using BeauUtil.Variants;
using Aqua;
using System.Collections.Generic;

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

        // private bool OnMeasurementCritterY;

        public Action OnSelectContinue;

        private class ActorButton : IKeyValuePair<StringHash32, ActorButton>
        {
            public StringHash32 ActorId;
            public ActorToggleButton Button;

            StringHash32 IKeyValuePair<StringHash32, ActorButton>.Key { get { return ActorId; } }

            ActorButton IKeyValuePair<StringHash32, ActorButton>.Value { get { return this; } }
        }

        private Dictionary<StringHash32, ActorButton> buttonDict;

        protected override void Awake()
        {
            Services.Events.Register<ExpSubscreen>(ExperimentEvents.SubscreenBack, PresetButtons, this);
            buttonDict = new Dictionary<StringHash32, ActorButton>();

            string toggleLog = "";

            m_CachedButtons = m_ButtonRoot.GetComponentsInChildren<ActorToggleButton>();
            for(int i = 0; i < m_CachedButtons.Length; ++i)
            {
                ActorToggleButton button = m_CachedButtons[i];
                button.Toggle.onValueChanged.AddListener((b) => UpdateFromButton(button.Id.AsStringHash(), b));
                toggleLog += button.Toggle.IsActive();
            }

            Debug.Log("actor screen is " + toggleLog);


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
            // OnMeasurementCritterY = OnMeasurementY();
            buttonDict.Clear();
            m_Visited = false;
            UpdateButtons();
        }

        // public bool OnMeasurementY() {
        //     if(m_CachedData.Tank == TankType.Measurement) {
        //         if(m_CachedData.CritterX != StringHash32.Null && m_CachedData.CritterY == StringHash32.Null) {
        //             return true;
        //         }
        //     }
        //     return false;
        // }
        private void PresetButtons(ExpSubscreen sc) {
            if(!(sc == ExpSubscreen.Actor)) return;
            if(m_CachedData == null) {
                throw new NullReferenceException("No cached data in actor.");
            }
            if(m_CachedData.ActorIds.Count == 0) return;
            bool singleCritter = Services.Tweaks.Get<ExperimentSettings>().GetTank(m_CachedData.Tank).SingleCritter;
            var hasOn = false;

            foreach(var actor in m_CachedData.ActorIds) {
                buttonDict.TryGetValue(actor, out ActorButton result);
                if(result != null) {
                    result.Button.Toggle.SetIsOnWithoutNotify(true);
                    hasOn = true;
                    if(singleCritter) break;
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
            // if(tankType.Tank == TankType.Measurement && OnMeasurementCritterY) {
            //     allActorTypes = MeasurementFilterY(allActorTypes);
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
                ActorButton acb = new ActorButton();
                acb.ActorId = actorType.Id();
                acb.Button = m_CachedButtons[buttonIdx];
                if(!buttonDict.ContainsKey(acb.ActorId)) buttonDict.Add(acb.ActorId, acb);

                button.Toggle.group = tankType.SingleCritter ? m_ToggleGroup : null;


                ++buttonIdx;
            }
            

            for(; buttonIdx < m_CachedButtons.Length; ++buttonIdx)
            {
                m_CachedButtons[buttonIdx].Load(StringHash32.Null, m_EmptyIcon, false);
            }

            m_NextButton.interactable = false;
        }

        // private List<BestiaryDesc> MeasurementFilterY(IEnumerable<BestiaryDesc> All_Actors) {
        //     List<BestiaryDesc> Result = new List<BestiaryDesc>(All_Actors);
        //     var targets = m_CachedData.GetTargets();
        //     foreach(var actor in All_Actors) {
        //         if(!targets.Contains(actor.Id())) Result.Remove(actor);
        //     }
        //     return Result;
        // }

        private void UpdateFromButton(StringHash32 inActorId, bool inbActive)
        {
            var actorData = Services.Assets.Bestiary.Get(inActorId);
            // if(m_CachedData.Tank == TankType.Measurement) {
            //     MeasurementUpdate(inActorId, inbActive);
            // }
            // else { GeneralUpdate(inActorId, inbActive); }
            GeneralUpdate(inActorId, inbActive);

            m_NextButton.interactable = m_CachedData.ActorIds.Count > 0;
            UpdateDisplay(inActorId);
            Services.Data.SetVariable("experiment:setup." + actorData.name, inbActive);
        }

        // private void MeasurementUpdate(StringHash32 inActorId, bool inbActive) {
        //     if(inbActive) {
        //         if(OnMeasurementCritterY) {
        //             m_CachedData.CritterY = inActorId;
        //         }
        //         else { 
        //             m_CachedData.CritterX = inActorId;
        //             m_CachedData.Process(inActorId);
        //         }
        //         Services.Events.Dispatch(ExperimentEvents.SetupAddActor, inActorId);
        //     }
        //     else {
        //         if(OnMeasurementCritterY) {
        //             m_CachedData.CritterY = StringHash32.Null;
        //         }
        //         else { m_CachedData.CritterX = StringHash32.Null; }
        //         Services.Events.Dispatch(ExperimentEvents.SetupRemoveActor, inActorId);
        //     }
        // }

        private void GeneralUpdate(StringHash32 inActorId, bool inbActive) {
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
            if(m_CachedData.Tank == TankType.Measurement) {
                if(inbActive)   m_CachedData.Critter = inActorId;
                else {
                    m_CachedData.Critter = StringHash32.Null;
                }
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

            Services.Data.SetVariable(ExperimentVars.SetupPanelLastActorType, inActorId);
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            UpdateDisplay(StringHash32.Null);
        }
    }
}