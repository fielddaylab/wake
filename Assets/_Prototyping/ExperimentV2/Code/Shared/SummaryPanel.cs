using System;
using Aqua;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class SummaryPanel : MonoBehaviour, ISceneOptimizable
    {
        #region Inspector

        [Required] public Button ContinueButton;
        public GameObject HasFacts;
        public GameObject NoFacts;
        public Transform FactListRoot;
        public LayoutGroup FactListLayout;

        [HideInInspector] public FactPools FactPools;

        #endregion // Inspector

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            FactPools = this.GetComponentInParent<FactPools>(true);
        }

        #endif // UNITY_EDITOR
    }
}