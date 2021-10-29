using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class SummaryPanel : MonoBehaviour
    {
        #region Inspector

        [Required] public Button ContinueButton;
        public GameObject HasFacts;
        public GameObject NoFacts;
        public Transform FactListRoot;
        public LayoutGroup FactListLayout;

        [InstanceOnly] public FactPools FactPools;

        #endregion // Inspector
    }
}