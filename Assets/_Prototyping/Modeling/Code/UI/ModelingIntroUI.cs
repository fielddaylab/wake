using System;
using Aqua;
using BeauRoutine.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class ModelingIntroUI : BasePanel
    {
        #region Inspector

        [Header("Modeling UI")]
        [SerializeField] private LocText m_Title = null;
        [SerializeField] private LocText m_Desc = null;
        [SerializeField] private Button m_ContinueButton = null;

        #endregion // Inspector

        protected override void Awake()
        {
            base.Awake();

            m_ContinueButton.onClick.AddListener(OnClickContinue);
        }

        public void Load(ModelingScenarioData inData)
        {
            m_Title.SetText(inData.TitleId());
            m_Desc.SetText(inData.DescId());
            Show();
        }

        private void OnClickContinue()
        {
            Hide();
            
            Services.Script.TriggerResponse(SimulationConsts.Trigger_Started);
        }
    }
}