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

        [HideInInspector] public FactPools FactPools;

        #endregion // Inspector

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            FactPools = FindObjectOfType<FactPools>();
        }

        #endif // UNITY_EDITOR
    }
}