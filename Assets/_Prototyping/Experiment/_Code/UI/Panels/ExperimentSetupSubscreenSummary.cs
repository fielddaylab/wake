using System;
using Aqua;
using Aqua.Portable;
using BeauPools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using System.Collections.Generic;

// TODO : Need some major cleanup
namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenSummary : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Transform m_Group = null;

        [Header("Text")]
        [SerializeField] private TMP_Text m_TankText = null;
        [SerializeField] private TMP_Text m_SummaryText = null;


        [SerializeField] private VerticalLayoutGroup m_BehaviourGroup = null;
        [SerializeField] private Transform m_RangeFactButton = null;
        [SerializeField] private FactSentenceDisplay.Pool m_BehaviorDisplayPool = null;

        #endregion // Inspector

        [NonSerialized] private StateFactDisplay m_Button = null;

        protected override void OnDisable()
        {
            m_BehaviorDisplayPool.Reset();

            base.OnDisable();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Routine.Start(this, PoolForceLayoutCorrectHACK());
        }

        public void Populate(ExperimentResultData inResultData)
        {
            m_BehaviorDisplayPool.Reset();

            if(inResultData.Setup.Tank == TankType.Stressor) {
                PopulateStressor(inResultData);

            }
            if(inResultData.Setup.Tank == TankType.Foundational) {
                PopulateFoundational(inResultData);
            }
            if(inResultData.Setup.Tank == TankType.Measurement) {
                PopulateMeasurement(inResultData);
            }

            // HACKS
            Routine.Start(this, ForceLayoutCorrectHACK());
        }

        private void PopulateMeasurement(ExperimentResultData resData) {
            m_RangeFactButton.gameObject.SetActive(false);
            var form_text = "";
            BestiaryDesc critter = Services.Assets.Bestiary.Get(resData.Setup.Critter);
            var state = critter.GetStateForEnvironment(resData.Setup.Values);
            var consume = BestiaryUtils.FindConsumeRule(critter, resData.Setup.PropertyId, state);
            var produce = BestiaryUtils.FindProduceRule(critter, resData.Setup.PropertyId, state);
            if(produce != null)
            {
                if(Services.Data.Profile.Bestiary.RegisterFact(produce.Id())) 
                {
                    m_BehaviorDisplayPool.Alloc().Populate(produce);
                }
            }
            if(consume != null){
                if(Services.Data.Profile.Bestiary.RegisterFact(consume.Id())) 
                {
                    m_BehaviorDisplayPool.Alloc().Populate(consume);
                }
            }

            if(consume == null && produce == null) {
                form_text = "No facts found for " + Services.Loc.Localize(critter.CommonName()) + " with " 
                    + Services.Assets.WaterProp.Property(resData.Setup.PropertyId).name;
            }

            // if(resData.Setup.CritterX != StringHash32.Null) {
            //     var actor = Services.Assets.Bestiary.Get(resData.Setup.CritterX);
            //     var actorState = GetActorState(actor, resData.Setup.Values);
            //     form_text = form_text + actor.PluralName() + GetState(actorState) + "\n";
            // }

            // if(resData.Setup.CritterY != StringHash32.Null) {
            //     var target = ((BFEat)resData.Setup.GetResult().Fact).Target();
            //     var state = target.GetStateForEnvironment(in resData.Setup.Values);
            //     var eatFact = BestiaryUtils.FindEatingRule(resData.Setup.CritterX, target.Id(), state);
            //     if(eatFact != null)  {
            //         Services.Data.Profile.Bestiary.RegisterFact(eatFact?.Id() ?? StringHash32.Null, out PlayerFactParams factParams);
            //         factParams.Add(PlayerFactFlags.KnowValue);
            //         form_text = form_text + target.PluralName() + GetState(state) + "\n" + factParams.Fact.GenerateSentence(factParams);
            //     }
            //     else {
            //         form_text = form_text + target.PluralName() + GetState(state);
            //     }
                
            //     Debug.Log(form_text);
            // }
            m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankMeasureSummary"));
            m_SummaryText.SetText(form_text);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) m_Group);

        }


        private ActorStateId GetActorState(BestiaryDesc critter, WaterPropertyBlockF32 waterBlock) {
            return critter.GetStateForEnvironment(waterBlock);
        }

        private string GetState(ActorStateId id) {
            switch(id) {
                case ActorStateId.Alive:
                    return " Are Alive";
                case ActorStateId.Dead:
                    return " Are Dead";
                case ActorStateId.Stressed:
                    return " Are Stressed";
                default:
                    return "";
            }
        }

        private void PopulateFoundational(ExperimentResultData inResultData) {
            foreach(var behaviorId in inResultData.ObservedBehaviorIds)
            {
                var behavior = Services.Assets.Bestiary.Fact<BFBehavior>(behaviorId);
                m_BehaviorDisplayPool.Alloc().Populate(behavior);
            }
            m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankVarSummary"));
            m_SummaryText.SetText(Services.Loc.Localize("experiment.summary.countableSummary"));
            m_RangeFactButton.gameObject.SetActive(false);
        }

        private void PopulateStressor(ExperimentResultData resData) {

            BestiaryDesc actor = GetSingleCritter(resData);
            BFState state = null;
            if(actor == null) throw new NullReferenceException("No actors");
            foreach(var fact in actor.Facts) {
                if(fact.GetType().Equals(typeof(BFState))) {
                    state = (BFState)fact;
                    if(state.PropertyId() == resData.Setup.PropertyId){
                        m_Button = m_RangeFactButton.GetComponent<StateFactDisplay>();
                        m_Button.Populate(state);
                        m_Button.gameObject.SetActive(true);
                    }
                    else { state = null; }
                }
            }

            var form_text = "";

            if (state == null)
            {
                var selectProp = "";
                foreach(var prop in Services.Assets.WaterProp.Objects) {
                    if(resData.Setup.PropertyId == prop.Index())  selectProp = prop.name;
                }
                form_text = Services.Loc.Localize(actor.CommonName()) + " Has No Fact for " + selectProp;
                m_SummaryText.SetText(form_text);
            }
            else
            {
                m_SummaryText.gameObject.SetActive(false);
            }
            // else
            // {
            //     form_text = Services.Loc.Localize(actor.CommonName()) + state.GenerateSentence();
            // }  

            m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankStressorSummary"));
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) m_Group);
        }

        private BestiaryDesc GetSingleCritter(ExperimentResultData resData) {
            var actors = resData.Setup.ActorIds;
            foreach(var actor in actors) {
                if(actor == StringHash32.Null) throw new NullReferenceException("No actors");
                return Services.Assets.Bestiary.Get(actor);
            }
            return null;
        }

        private IEnumerator PoolForceLayoutCorrectHACK()
        {
            m_BehaviourGroup.enabled = false;
            yield return null;
            m_BehaviourGroup.enabled = true;
        }

        private IEnumerator ForceLayoutCorrectHACK()
        {
            var layout = m_BehaviorDisplayPool.DefaultSpawnTransform.GetComponent<LayoutGroup>();
            layout.enabled = false;
            yield return null;
            layout.enabled = true;
        }
    }
}