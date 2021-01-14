using System;
using Aqua;
using BeauPools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using BeauRoutine;
using System.Collections;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenSummary : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private TMP_Text m_TankText = null;
        [SerializeField] private TMP_Text m_SummaryText = null;
        [SerializeField] private FactSentenceDisplay.Pool m_BehaviorDisplayPool = null;

        #endregion // Inspector

        protected override void OnDisable()
        {
            m_BehaviorDisplayPool.Reset();

            base.OnDisable();
        }

        public void Populate(ExperimentResultData inResultData)
        {
            m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankVarSummary"));
            m_SummaryText.SetText(Services.Loc.Localize("experiment.summary.countableSummary"));

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