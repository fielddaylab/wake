using System;
using BeauPools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenBegin : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Button m_StartButton = null;
        [SerializeField] private TMP_Text m_TankText = null;
        [SerializeField] private TMP_Text m_ActorText = null;
        [SerializeField] private Button m_BackButton = null;

        #endregion // Inspector

        private ExperimentSetupData m_CachedData;

        public Action OnSelectStart;
        public Action OnSelectBack;

        protected override void Awake()
        {
            m_StartButton.onClick.AddListener(() => OnSelectStart?.Invoke());
            m_BackButton.onClick.AddListener(() => OnSelectBack?.Invoke());
        }

        private void UpdateDisplay()
        {
            var experimentSettings = Services.Tweaks.Get<ExperimentSettings>();

            m_TankText.SetText(Services.Loc.Localize("experiment.summary.tankVarSummary"));

            using(PooledStringBuilder psb = PooledStringBuilder.Create())
            {
                foreach(var actor in m_CachedData.ActorIds)
                {
                    if (psb.Builder.Length > 0)
                        psb.Builder.Append('\n');
                    
                    string label = Services.Loc.Localize(experimentSettings.GetActor(actor).ShortLabelId);
                    psb.Builder.Append(label);
                }

                m_ActorText.SetText(psb.Builder.ToString());
            }
        }

        public override void SetData(ExperimentSetupData inData)
        {
            base.SetData(inData);
            m_CachedData = inData;
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            UpdateDisplay();
        }
    }
}