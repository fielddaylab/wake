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
            if(inResultData.Setup.Tank.Equals(TankType.Stressor)) {
                
                BFStateRange state = null;
                var allEnvs = Services.Assets.Bestiary.AllEntriesForCategory(BestiaryDescCategory.Environment);
                List<BFStateRange> rangefacts = new List<BFStateRange>();
                foreach(var env in allEnvs) {
                    foreach(BFBase fact in env.Facts) {
                        if(fact is BFStateRange) {
                            rangefacts.Add((BFStateRange)fact);
                            state = (BFStateRange) fact;
                        }
                    }
                }

                if(rangefacts.Count > 0) {
                    foreach(var fact in rangefacts) {
                        m_Button = m_RangeFactButton.GetComponent<BestiaryRangeFactButton>();
                        m_Button.Initialize(fact, null, false, false, null);
                        m_Button.gameObject.SetActive(true);
                    }
                }

                var form_text = "Sea otter " + state.GenerateSentence(null);                

                m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankStressorSummary"));
                m_SummaryText.SetText(form_text);
            }
            else {
                m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankVarSummary"));
                m_SummaryText.SetText(Services.Loc.Localize("experiment.summary.countableSummary"));
                m_RangeFactButton.gameObject.SetActive(false);
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

        private IEnumerator ForceLayoutCorrectHACK()
        {
            var layout = m_BehaviorDisplayPool.DefaultSpawnTransform.GetComponent<LayoutGroup>();
            layout.enabled = false;
            yield return null;
            layout.enabled = true;
        }
    }
}