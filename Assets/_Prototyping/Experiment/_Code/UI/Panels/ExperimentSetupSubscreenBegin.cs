using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Experiment
{
    public class ExperimentSetupSubscreenBegin : ExperimentSetupSubscreen
    {
        #region Inspector

        [SerializeField] private Button m_StartButton = null;
        [SerializeField] private Button m_BackButton = null;

        #endregion // Inspector

        public Action OnSelectStart;
        public Action OnSelectBack;

        protected override void Awake()
        {
            m_StartButton.onClick.AddListener(() => OnSelectStart?.Invoke());
            m_BackButton.onClick.AddListener(() => OnSelectBack?.Invoke());
        }
    }
}