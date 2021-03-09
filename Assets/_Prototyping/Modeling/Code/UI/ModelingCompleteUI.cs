using System;
using Aqua;
using BeauRoutine.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class ModelingCompleteUI : BasePanel
    {
        #region Inspector

        [Header("Modeling UI")]
        [SerializeField] private LocText m_Title = null;
        [SerializeField] private LocText m_Desc = null;

        #endregion // Inspector

        protected override void Awake()
        {
            base.Awake();
        }

        public void Load(ModelingScenarioData inData)
        {
            m_Title.SetText(inData.TitleId());
            m_Desc.SetText(inData.CompleteId());
            Show();
        }
    }
}