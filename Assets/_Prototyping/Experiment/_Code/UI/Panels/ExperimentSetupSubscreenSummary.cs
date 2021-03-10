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
namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenSummary : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private TMP_Text m_TankText = null;
        [SerializeField] private TMP_Text m_SummaryText = null;

        [SerializeField] private Transform m_RangeFactButton = null;
        [SerializeField] private FactSentenceDisplay.Pool m_BehaviorDisplayPool = null;

        #endregion // Inspector

        [NonSerialized] private BestiaryRangeFactButton m_Button = null;

        protected override void OnDisable()
        {
            m_BehaviorDisplayPool.Reset();

            base.OnDisable();
        }

        public void Populate(ExperimentResultData inResultData)
        {
            if(inResultData.Setup.Tank == TankType.Stressor) {
                PopulateStressor(inResultData);

            }
            if(inResultData.Setup.Tank == TankType.Foundational) {
                PopulateFoundational();
            }
            if(inResultData.Setup.Tank == TankType.Measurement) {
                PopulateMeasurement(inResultData);
            }

            m_BehaviorDisplayPool.Reset();

            foreach(var behaviorId in inResultData.ObservedBehaviorIds)
            {
                var behavior = Services.Assets.Bestiary.Fact(behaviorId);
                m_BehaviorDisplayPool.Alloc().Populate(behavior, null);
            }

            // HACKS
            Routine.Start(this, ForceLayoutCorrectHACK());
        }

        private void PopulateMeasurement(ExperimentResultData resData) {
            m_RangeFactButton.gameObject.SetActive(false);
            var form_text = "";


            if(resData.Setup.CritterX != StringHash32.Null) {
                var actor = Services.Assets.Bestiary.Get(resData.Setup.CritterX);
                var actorState = GetActorState(actor, resData.Setup.Values);
                form_text = form_text + actor.PluralName() + GetState(actorState) + "\n";
            }

            if(resData.Setup.CritterY != StringHash32.Null) {
                var target = ((BFEat)resData.Setup.GetResult().Fact).Target();
                var state = target.GetStateForEnvironment(in resData.Setup.Values);
                var eatFact = BestiaryUtils.FindEatingRule(resData.Setup.CritterX, target.Id(), state);
                if(eatFact != null)  {
                    Services.Data.Profile.Bestiary.RegisterFact(eatFact?.Id() ?? StringHash32.Null, out PlayerFactParams factParams);
                    factParams.Add(PlayerFactFlags.KnowValue);
                    form_text = form_text + target.PluralName() + GetState(state) + "\n" + factParams.Fact.GenerateSentence(factParams);
                }
                else {
                    form_text = form_text + target.PluralName() + GetState(state);
                }
                
                Debug.Log(form_text);
            }
            m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankMeasureSummary"));
            m_SummaryText.SetText(form_text);

        }


        private ActorStateId GetActorState(BestiaryDesc critter, WaterPropertyBlockF32 waterBlock) {
            var allProperties = Services.Assets.WaterProp.Sorted();
            var state = ActorStateId.Alive;
            foreach(var prop in allProperties) {
                if(!prop.HasFlags(WaterPropertyFlags.IsMeasureable)) continue;
                var propState = BestiaryUtils.FindStateTransitions(
                    critter, prop.Index()).Evaluate(waterBlock[prop.Index()]);
                if(propState != ActorStateId.Alive) return propState;
            }
            return state;
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

        private void PopulateFoundational() {
            m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankVarSummary"));
            m_SummaryText.SetText(Services.Loc.Localize("experiment.summary.countableSummary"));
            m_RangeFactButton.gameObject.SetActive(false);
        }

        private void PopulateStressor(ExperimentResultData resData) {

            BestiaryDesc actor = GetSingleCritter(resData);
            BFStateRange state = null;
            if(actor == null) throw new NullReferenceException("No actors");
            foreach(var fact in actor.Facts) {
                if(fact.GetType().Equals(typeof(BFStateRange))) {
                    state = (BFStateRange)fact;
                    if(state.PropertyId() == resData.Setup.PropertyId){
                        m_Button = m_RangeFactButton.GetComponent<BestiaryRangeFactButton>();
                        m_Button.Initialize(state, null, false, false, null);
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
                form_text = actor.CommonName() + " Has No Fact for " + selectProp;
            }
            else
            {
                form_text = actor.CommonName() + state.GenerateSentence();
            }
            // BFStateRange state = null;
            // var allEnvs = Services.Assets.Bestiary.AllEntriesForCategory(BestiaryDescCategory.Environment);
            // List<BFStateRange> rangefacts = new List<BFStateRange>();
            // foreach(var env in allEnvs) {
            //     foreach(BFBase fact in env.Facts) {
            //         if(fact is BFStateRange) {
            //             rangefacts.Add((BFStateRange)fact);
            //             state = (BFStateRange) fact;
            //         }
            //     }
            // }

            //     if(rangefacts.Count > 0) {
            //         foreach(var fact in rangefacts) {
            //             m_Button = m_RangeFactButton.GetComponent<BestiaryRangeFactButton>();
            //             m_Button.Initialize(fact, null, false, false, null);
            //             m_Button.gameObject.SetActive(true);
            //         }
            //     }

            //     var form_text = "Sea otter " + state.GenerateSentence(null);                

            m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankStressorSummary"));
                m_SummaryText.SetText(form_text);
        }

        private BestiaryDesc GetSingleCritter(ExperimentResultData resData) {
            var actors = resData.Setup.ActorIds;
            foreach(var actor in actors) {
                if(actor == StringHash32.Null) throw new NullReferenceException("No actors");
                return Services.Assets.Bestiary.Get(actor);
            }
            return null;
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