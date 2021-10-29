using System;
using Aqua;
using BeauRoutine.Extensions;
using BeauUtil;
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
            // m_Desc.SetText(inData.CompleteId());
            Show();
        }

        public void LoadAlreadyComplete(StringHash32 inTitleId, StringHash32 inDescId)
        {
            m_Title.SetText(inTitleId);
            m_Desc.SetText(inDescId);
            Show();
        }
    }
}