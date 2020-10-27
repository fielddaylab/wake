using System;
using BeauPools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenSummary : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private TMP_Text m_TankText = null;
        [SerializeField] private TMP_Text m_SummaryText = null;
        [SerializeField] private TMP_Text m_BehaviorText = null;

        #endregion // Inspector

        public void Populate(ExperimentResultData inResultData)
        {
            var experimentSettings = Services.Tweaks.Get<ExperimentSettings>();

            m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankVarSummary"));
            m_SummaryText.SetText(Services.Loc.Localize("experiment.summary.countableSummary"));

            using(PooledStringBuilder psb = PooledStringBuilder.Create())
            {
                foreach(var behavior in inResultData.ObservedBehaviorIds)
                {
                    if (psb.Builder.Length > 0)
                        psb.Builder.Append('\n');
                    
                    string label = Services.Loc.Localize(experimentSettings.GetBehavior(behavior).ShortLabelId);
                    psb.Builder.Append(label);
                }

                m_BehaviorText.SetText(psb.Builder.ToString());
            }
        }
    }
}